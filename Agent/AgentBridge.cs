using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindBot.Agent.Core;
using WindBot.Agent.Knowledge;
using WindBot.Game;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Agent
{
    /// <summary>
    /// Adapter between GameBehavior (WindBot types) and the neutral agent core.
    /// Converts every engine prompt into a DecisionRequest, asks the policy, and maps the
    /// chosen indices back to the WindBot return types that GameBehavior encodes.
    /// Contains no card-specific logic and never touches GameAI or any Executor.
    /// </summary>
    public sealed class AgentBridge : IGameDecider
    {
        public const string DeckPrefix = "Agent:";

        public static bool IsAgentDeck(string deck)
        {
            return deck != null && deck.StartsWith(DeckPrefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Separator in the deck setting before the opponent's deck: "Agent:AI_Ryzeal@&lt;opponent ydk&gt;".</summary>
        public const char OpponentSeparator = '@';

        public static string DeckNameOf(string deck)
        {
            string name = deck.Substring(DeckPrefix.Length);
            int at = name.IndexOf(OpponentSeparator);
            if (at >= 0) name = name.Substring(0, at);
            name = name.Trim();
            return name.Length == 0 ? "AI_Ryzeal" : name;
        }

        /// <summary>The opponent's deck named in the deck setting ("Agent:AI_Ryzeal@Snake-Eye"), or null.</summary>
        public static string OpponentDeckOf(string deck)
        {
            if (deck == null) return null;
            int at = deck.IndexOf(OpponentSeparator);
            if (at < 0) return null;
            string name = deck.Substring(at + 1).Trim();
            return name.Length == 0 ? null : name;
        }

        /// <summary>
        /// The opponent's deck list, which the engine never sends: from the deck setting or the AgentOpponentDeck argument.
        /// Without it the bot only knows the opponent's cards it has seen.
        /// </summary>
        public string OpponentDeck;

        private readonly Duel _duel;
        private readonly string _deck;
        private readonly string _buildId;
        private IDecisionPolicy _policy;
        private AgentLog _log;
        private bool _duelFinished;
        private string _duelId;
        private Deck _deckList;
        private GameCardDb _db;
        private ValueModel _values;
        private TurnPlanner _planner;
        private BaitModel _bait;
        /// <summary>Plan behind each main-phase option of the current request, and the not yet reached moves of the plan taken.</summary>
        private readonly Dictionary<DecisionOption, PlanResult> _optionPlans = new Dictionary<DecisionOption, PlanResult>();
        private readonly List<PlanMove> _planAhead = new List<PlanMove>();
        /// <summary>Cards we may not Normal Summon, with the last turn the lock lasts (Lightning Crash).</summary>
        private readonly Dictionary<int, int> _noNormalSummonUntil = new Dictionary<int, int>();
        private const double PlanTargetBonus = 1000;

        /// <summary>Crossout Designator: declare a name, banish that card from our Main Deck, negate the opponent's cards of that name.</summary>
        private const int CrossoutDesignator = 65681983;
        /// <summary>Name to declare for the Crossout Designator we are chaining (0 if none).</summary>
        private int _declareName;
        private DuelZoneView _zones;
        private int _pendingRuleSummon;
        /// <summary>Monster whose attack we just declared, for the attack-target pick that follows.</summary>
        private ClientCard _attacker;

        public AgentBridge(Duel duel, string deck)
        {
            _duel = duel;
            _deck = deck;
            _buildId = typeof(AgentBridge).Module.ModuleVersionId.ToString("N");
            BeginDuelLog();
        }

        private void BeginDuelLog()
        {
            int seed = Environment.TickCount ^ Guid.NewGuid().GetHashCode();
            string duelId = Guid.NewGuid().ToString("N");
            _duelId = duelId;
            ValuePolicy vp = new ValuePolicy(seed);
            try { vp.Explore = CardDbFactory.LoadWeights().Explore; } catch (Exception) { } // so the header shows the rate in use
            _policy = vp;
            _log = new AgentLog(System.IO.Path.Combine("logs", "agent"), duelId);
            _log.Header(duelId, _buildId, _policy.Name, seed, _deck);
            _duelFinished = false;
            Logger.WriteLine("Agent log: " + _log.Path);
        }

        // ---------------------------------------------------------------- per-game card DB

        public void OnDeckLoaded(Deck deck)
        {
            _deckList = deck;
            BuildCardDb();
        }

        private void BuildCardDb()
        {
            _noNormalSummonUntil.Clear();
            if (_deckList == null) return;
            try
            {
                _db = CardDbFactory.Build(_deckList);
                if (!String.IsNullOrEmpty(OpponentDeck))
                {
                    Deck theirs = CardDbFactory.LoadDeck(OpponentDeck);
                    if (theirs != null) _db.AddOpponentDeck(theirs.Cards, theirs.ExtraCards);
                    else _log.Error("opponent-deck", "Deck not found: " + OpponentDeck);
                }
                _values = new ValueModel(_db, CardDbFactory.LoadWeights());
                _planner = new TurnPlanner(_db, _values.Weights);
                _bait = new BaitModel(_db, _values.Weights);
                ValueModel vm = _values;
                _planner.NextTurnValue = id => vm.ValueNextTurn(id, _zones).Value;
                ValuePolicy vp = _policy as ValuePolicy;
                if (vp != null) vp.Explore = _values.Weights.Explore;
                _zones = new DuelZoneView(_duel, _db);
                WriteCardDb();
            }
            catch (Exception ex)
            {
                _db = null;
                _log.Error("carddb", ex.ToString());
            }
        }

        private string LogBase { get { return _log.Path.Substring(0, _log.Path.Length - ".jsonl".Length); } }

        /// <summary>Fixed card information and links. Written at start, rewritten at the end to include revealed opponent cards.</summary>
        private void WriteCardDb()
        {
            if (_db == null) return;
            try
            {
                string path = LogBase + "_carddb.md";
                System.IO.File.WriteAllText(path, _db.Describe(null), new UTF8Encoding(false));
                _log.CardDb("carddb", path, _db.Cards.Count);
            }
            catch (Exception ex)
            {
                _log.Error("carddb-write", ex.ToString());
            }
        }

        /// <summary>One compact section per turn: events, effects used, where our cards are.</summary>
        private void AppendState(string title)
        {
            if (_db == null) return;
            try
            {
                string path = LogBase + "_state.md";
                if (!System.IO.File.Exists(path))
                    System.IO.File.WriteAllText(path, "# 듀얼 상태 기록\n\n", new UTF8Encoding(false));
                string text = _db.DescribeState(title, InstancesOf);
                if (_values != null) text += "### 패 카드 가치 (유한 등비 합)\n" + CardDbFactory.DescribeValues(_db, _values, _zones);
                if (_planner != null) text += "### 지금 필드의 방해와 이 상태에서의 최선 계획\n" + CardDbFactory.DescribePlan(_planner, _zones) + "\n";
                System.IO.File.AppendAllText(path, text, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                _log.Error("state-write", ex.ToString());
            }
        }

        /// <summary>Where the copies of a card are right now (own side), with current stats on the field.</summary>
        private string InstancesOf(int cardId)
        {
            if (_duel.Fields == null || _duel.Fields[0] == null || _duel.Fields[0].Hand == null) return null;
            List<string> parts = new List<string>();
            for (int p = 0; p < 2; ++p)
            {
                ClientField f = _duel.Fields[p];
                string side = p == 0 ? "" : "상대 ";
                int hand = Count(f.Hand, cardId), grave = Count(f.Graveyard, cardId), banished = Count(f.Banished, cardId), extra = Count(f.ExtraDeck, cardId);
                if (hand > 0) parts.Add(side + "패 " + hand);
                foreach (ClientCard c in f.MonsterZone)
                    if (c != null && c.Id == cardId)
                        parts.Add(side + "몬스터 존 " + c.Sequence + "(" + (c.IsFaceup() ? "앞면" : "뒷면") + ", 공 " + c.Attack + "/수 " + c.Defense
                                  + (c.Overlays != null && c.Overlays.Count > 0 ? ", 소재 " + c.Overlays.Count : "") + ")");
                foreach (ClientCard c in f.SpellZone)
                    if (c != null && c.Id == cardId) parts.Add(side + "마법·함정 존 " + c.Sequence + "(" + (c.IsFaceup() ? "앞면" : "세트") + ")");
                if (grave > 0) parts.Add(side + "묘지 " + grave);
                if (banished > 0) parts.Add(side + "제외 " + banished);
                if (p == 0 && extra > 0) parts.Add("엑스트라 " + extra);
            }
            return String.Join(", ", parts.ToArray());
        }

        private static int Count(IEnumerable<ClientCard> cards, int id)
        {
            int n = 0;
            if (cards != null) foreach (ClientCard c in cards) if (c != null && c.Id == id) n++;
            return n;
        }

        /// <summary>Best planned outcome when this main-phase option is taken first.</summary>
        private PlanResult PlanFor(DecisionOption o, List<PlanResult> byFirst, PlanResult stop)
        {
            if (o.Action == "ToEndPhase") return stop;
            string kind = null;
            int effectIndex = 0;
            switch (o.Action)
            {
                case "Summon": kind = "ns"; break;
                case "SpSummon": kind = o.Location == (int)CardLocation.Hand ? "rule" : o.Location == (int)CardLocation.Extra ? "xyz" : null; break;
                case "Activate":
                    {
                        CardEntry c = _db.Get(o.CardId);
                        EffectEntry e = c != null ? _db.FindEffect(c, o.Description) : null;
                        effectIndex = e != null ? e.Index : 0;
                        bool monster = c != null && (c.Data.Type & 0x1) != 0;
                        kind = o.Location == (int)CardLocation.Hand ? (monster ? "hand" : "spell")
                             : o.Location == (int)CardLocation.MonsterZone ? "ignition"
                             : o.Location == (int)CardLocation.SpellZone || o.Location == (int)CardLocation.FieldZone
                                 ? (e != null && e.Kind.StartsWith("기동") ? "zone" : "setspell") : null;
                        break;
                    }
            }
            if (kind == null) return null;
            PlanResult best = null;
            foreach (PlanResult p in byFirst)
            {
                PlanMove m = p.Moves.Count > 0 ? p.Moves[0] : null;
                if (m == null || m.Kind != kind || m.CardId != o.CardId) continue;
                if (effectIndex != 0 && m.EffectIndex != 0 && m.EffectIndex != effectIndex) continue;
                if (best == null || p.Value > best.Value) best = p;
            }
            return best;
        }

        /// <summary>Attaches card-DB knowledge to each option so the policy (and the log) sees it.</summary>
        private void Annotate(DecisionRequest r)
        {
            if (_db == null) return;

            // Main phase on our turn: plan the rest of the turn once, then read each option's best plan.
            List<PlanResult> byFirst = null;
            PlanResult stop = null;
            _optionPlans.Clear();
            if (r.Kind == RequestKind.Idle && _planner != null && _duel.Player == 0)
            {
                try { _planner.Plan(PlanSnapshot(), out byFirst, out stop); }
                catch (Exception ex) { _log.Error("planner", ex.ToString()); byFirst = null; }
            }

            foreach (DecisionOption o in r.Options)
            {
                if (byFirst != null)
                {
                    PlanResult p = PlanFor(o, byFirst, stop);
                    if (p != null)
                    {
                        _optionPlans[o] = p;
                        o.Estimate = p.Value;
                        o.Info = (o.CardId != 0 ? (_db.Get(o.CardId) != null ? _db.Get(o.CardId).Data.Name : "") + " | " : "") + "계획 " + p.Describe();
                        continue;
                    }
                }
                if (o.CardId == 0) continue;
                CardEntry card = _db.EnsureCard(o.CardId, o.Controller == 1);
                if (card == null) continue;
                StringBuilder sb = new StringBuilder(card.Data.Name);
                EffectEntry effect = o.Action == "Activate" ? _db.FindEffect(card, o.Description) : null;
                if (effect == null && o.Action == "SpSummon" && o.Location == (int)CardLocation.Hand)
                    effect = card.Effects.Find(e => e.Kind.StartsWith("룰"));
                if (effect != null)
                {
                    sb.Append(" | 효과 ").Append(effect.Index).Append(": ").Append(effect.Kind);
                    if (!String.IsNullOrEmpty(effect.Label)) sb.Append(" — ").Append(effect.Label);
                    string usage = _db.UsageText(o.Controller == 1 ? 1 : 0, effect);
                    if (usage.Length > 0) sb.Append(" | 이번 턴: ").Append(usage);
                    foreach (CardLink l in effect.Links)
                        if (l.Targets.Count > 0)
                            sb.Append(" | ").Append(l.Relation).Append(" → ").Append(String.Join(", ", l.Targets.ConvertAll(id => _db.Get(id).Data.Name).ToArray()));
                }
                else if (card.LinkedFrom.Count > 0 || card.Effects.Count > 0)
                {
                    sb.Append(" | 효과 ").Append(card.Effects.Count).Append("개, 이 카드를 불러오는 카드 ").Append(card.LinkedFrom.Count).Append("개 연결");
                }

                // Chain value of own actions (finite geometric sum): information only; decisions use the estimates below.
                if (_values != null && o.Controller != 1)
                {
                    ChainResult v = null;
                    if (effect != null && (o.Action == "Activate" || o.Action == "SpSummon")) v = _values.ValueOfEffect(o.CardId, effect, _zones);
                    else if (o.Action == "Summon") v = _values.ValueOfNormalSummon(o.CardId, _zones);
                    if (v != null) sb.Append(" | 흐름 가치 ").Append(v.Describe());
                }
                o.Info = sb.ToString();
            }

            try { Estimate(r, stop); }
            catch (Exception ex) { _log.Error("estimate:" + r.Kind, ex.ToString()); }
        }

        private static readonly HashSet<long> GiveUpHints = new HashSet<long>
        {
            500 /* Release */, 501 /* Discard */, 503 /* Remove */, 504 /* ToGrave */, 507 /* ToDeck */,
            513 /* XyzMaterial */, 519 /* RemoveXyz */, 531 /* Tribute */, 532 /* DeattachFrom */
        };

        /// <summary>Hints where the picked card is removed: as a target, an opponent's card is the point.</summary>
        private static readonly HashSet<long> RemovalHints = new HashSet<long>
        {
            502 /* Destroy */, 503 /* Remove */, 504 /* ToGrave */, 505 /* RtoHand */, 507 /* ToDeck */
        };

        /// <summary>Negate / disable picks: an opponent's card is the point; our own only when forced, least worth first.</summary>
        private static readonly HashSet<long> NegateHints = new HashSet<long> { 573 /* Disable */, 575 /* Negate */ };

        private const double OpponentTargetBase = 50;

        /// <summary>
        /// Two or more interruptions offered against the same opponent play: order them by the bait seed
        /// (the one likely to be stopped and worth less first). One offered: only its stop chance is noted.
        /// </summary>
        private void BaitOrder(DecisionRequest r)
        {
            if (_bait == null) return;
            List<DecisionOption> opts = new List<DecisionOption>();
            List<KeyValuePair<CardEntry, EffectEntry>> fx = new List<KeyValuePair<CardEntry, EffectEntry>>();
            foreach (DecisionOption o in r.Options)
            {
                if (o.Action == "Pass" || double.IsNaN(o.Estimate) || o.Estimate <= 0) continue;
                CardEntry c = _db.Get(o.CardId);
                EffectEntry e = c != null ? _db.FindEffect(c, o.Description) : null;
                if (c == null || e == null || !_planner.Interrupts.IsInterrupt(c, e)) continue;
                opts.Add(o);
                fx.Add(new KeyValuePair<CardEntry, EffectEntry>(c, e));
            }
            if (opts.Count == 0) return;
            OpponentPool pool = OpponentPoolNow();
            if (opts.Count == 1)
            {
                opts[0].Info = (opts[0].Info ?? "") + " | 막힐 확률 " + _bait.StopChance(pool, fx[0]).ToString("0.##");
                return;
            }
            List<string> notes = new List<string>();
            List<double> est = _bait.Order(pool, fx, opts.Select(o => o.Estimate).ToList(), notes);
            for (int i = 0; i < opts.Count; ++i)
            {
                opts[i].Estimate = est[i];
                opts[i].Info = (opts[i].Info ?? "") + " | " + notes[i];
            }
        }

        /// <summary>The opponent's cards as we see them now: hidden pool, cards they can use right now, seen copies, face-up board.</summary>
        private OpponentPool OpponentPoolNow()
        {
            ClientField f = _duel.Fields[1];
            OpponentPool p = new OpponentPool();
            int faceDown = 0, setSpells = 0;
            foreach (ClientCard c in f.MonsterZone)
            {
                if (c == null) continue;
                if (c.IsFacedown()) { faceDown++; continue; }
                p.Board.Add(new KeyValuePair<int, int>(c.Id, c.Overlays.Count));
                AddSeen(p, c.Id);
            }
            foreach (ClientCard c in f.SpellZone)
            {
                if (c == null) continue;
                if (c.IsFacedown()) { faceDown++; setSpells++; continue; }
                p.Board.Add(new KeyValuePair<int, int>(c.Id, 0));
                AddSeen(p, c.Id);
            }
            foreach (ClientCard c in f.Graveyard) if (c != null) AddSeen(p, c.Id);
            foreach (ClientCard c in f.Banished) if (c != null && c.IsFaceup()) AddSeen(p, c.Id);
            foreach (ClientCard c in f.Hand) if (c != null && c.Id != 0) AddSeen(p, c.Id); // revealed hand cards are known, not hidden
            int knownHand = f.Hand.Count(c => c != null && c.Id != 0);
            int deck = f.Deck != null ? f.Deck.Count : 0;
            p.Hidden = deck + f.Hand.Count - knownHand + faceDown;
            p.Hand = f.Hand.Count - knownHand;
            p.Set = setSpells;
            p.TheirTurn = _duel.Player == 1;
            p.FieldEmpty = f.GetMonsters().Count == 0 && f.GetSpells().Count == 0;
            p.Exhausted = e =>
            {
                KeyUsage u = _db.PeekUsage(1, e);
                return u != null && e.Limit != null && u.Used >= e.Limit.Count;
            };
            return p;
        }

        private static void AddSeen(OpponentPool p, int id)
        {
            if (id == 0) return;
            int n;
            p.Seen[id] = p.Seen.TryGetValue(id, out n) ? n + 1 : 1;
        }

        /// <summary>Face-up cards on the opponent's field (monster and spell/trap zones, field zone included).</summary>
        private int OpponentFaceUp()
        {
            int n = 0;
            foreach (ClientCard c in _duel.Fields[1].MonsterZone) if (c != null && c.IsFaceup()) n++;
            foreach (ClientCard c in _duel.Fields[1].SpellZone) if (c != null && c.IsFaceup()) n++;
            return n;
        }

        /// <summary>
        /// Estimates the value policy decides by, for requests other than the planned main-phase actions:
        /// main-phase extras (battle, setting traps), battle commands, own optional triggers, chain responses, card picks.
        /// </summary>
        private void Estimate(DecisionRequest r, PlanResult stop)
        {
            if (_planner == null) return;
            switch (r.Kind)
            {
                case RequestKind.Idle:
                    if (stop == null) return;
                    foreach (DecisionOption o in r.Options)
                    {
                        if (!double.IsNaN(o.Estimate)) continue;
                        if (o.Action == "ToBattlePhase") o.Estimate = stop.Value + 1; // attacking is how the game is won
                        else if (o.Action == "SetSpell" && IsSetInterrupt(o.CardId)) o.Estimate = stop.Value + 1; // the plan counts it as set
                        else if (o.Action == "Repos" && ReposWanted(o)) o.Estimate = stop.Value + 2; // free, and the plan does not move it
                    }
                    break;

                case RequestKind.Battle:
                    // Attack options carry their battle outcome (set in OnSelectBattleCmd); stopping is 0.
                    foreach (DecisionOption o in r.Options)
                        if (o.Action != "Attack") o.Estimate = o.Action == "Activate" ? double.NaN : 0;
                    r.NoExplore = true;
                    break;

                case RequestKind.Position:
                    {
                        CardEntry pc = r.SourceCardId != 0 ? _db.EnsureCard(r.SourceCardId, false) : null;
                        bool attack = pc == null || BattleMath.PreferAttack(pc.Data.Atk, pc.Data.Def);
                        foreach (DecisionOption o in r.Options)
                        {
                            CardPosition pos = (CardPosition)o.Value;
                            if (pos == CardPosition.FaceUpAttack) o.Estimate = attack ? 1 : 0;
                            else if (pos == CardPosition.FaceUpDefence) o.Estimate = attack ? 0 : 1;
                            else o.Estimate = -1;
                        }
                        break;
                    }

                case RequestKind.EffectYn:
                    {
                        // Our own optional triggers: the plan assumes we use them,
                        // except an effect that hits a card on the field while the opponent has none face-up (it would hit ours).
                        CardEntry yc = r.SourceCardId != 0 ? _db.Get(r.SourceCardId) : null;
                        EffectEntry ye = yc != null ? _db.FindEffect(yc, r.Description) : null;
                        bool idle = yc != null && ye != null && _planner.HitsFieldCards(yc, ye) && OpponentFaceUp() == 0;
                        r.Options[0].Estimate = idle ? 0 : 1;
                        r.Options[1].Estimate = idle ? 1 : 0;
                        if (idle) { r.NoExplore = true; r.Options[0].Info = (r.Options[0].Info ?? "") + " | 상대 앞면 카드 없음: 자기 카드만 대상"; }
                        break;
                    }

                case RequestKind.YesNo:
                    {
                        // "...then you can ..." inside our own card's resolution (Plugin / Plasma Hole attaching a material):
                        // a free extra the plan already counts, so take it. Prompts from other cards stay unscored.
                        int code = (int)(r.Description >> 20);
                        bool own = code != 0 && _deckList != null && (_deckList.Cards.Contains(code) || _deckList.ExtraCards.Contains(code));
                        if (!own) break;
                        r.Options[0].Estimate = 1;
                        r.Options[1].Estimate = 0;
                        r.Options[0].Info = (r.Options[0].Info ?? "") + " | 처리 중 추가 효과: 사용";
                        r.NoExplore = true;
                        break;
                    }

                case RequestKind.Option:
                    {
                        // "Add to hand or send to GY" (aux.ToHandOrElse, e.g. Infernal Flame Banshee ①): the plan counts the search.
                        bool any = false;
                        foreach (DecisionOption o in r.Options)
                        {
                            if (o.Description == StrAddToHand) { o.Estimate = 1; any = true; }
                            else if (o.Description == StrSendToGrave) { o.Estimate = 0; any = true; }
                        }
                        if (any) r.NoExplore = true;
                        break;
                    }

                case RequestKind.Chain:
                    {
                        bool opponentActed = _duel.LastChainPlayer == 1;
                        if (opponentActed || _duel.Player == 1) r.NoExplore = true; // a random pass or a wasted interruption costs the game
                        foreach (DecisionOption o in r.Options)
                        {
                            if (o.Action == "Pass") { o.Estimate = opponentActed ? 0 : 1; continue; }
                            if (o.CardId == CrossoutDesignator)
                            {
                                // Only in answer to an opponent's card that our Main Deck still holds a copy of: that is the name we declare.
                                int name = opponentActed ? CrossoutName() : 0;
                                CardEntry cc = _db.Get(o.CardId);
                                EffectEntry ce = cc != null ? _db.FindEffect(cc, o.Description) : null;
                                o.Estimate = name != 0 && cc != null && ce != null ? _planner.Interrupts.Value(cc, ce, o.Location == (int)CardLocation.Hand) : -1;
                                _declareName = name;
                                if (name != 0) o.Info = (o.Info ?? "") + " | 선언 대상: " + (_db.Get(name) != null ? _db.Get(name).Data.Name : name.ToString());
                                continue;
                            }
                            CardEntry c = _db.Get(o.CardId);
                            EffectEntry e = c != null ? _db.FindEffect(c, o.Description) : null;
                            if (c == null || e == null) continue;
                            if (e.Kind.StartsWith("유발 효과"))
                            {
                                // Own trigger offered in a chain window; one that hits a field card has nothing to hit but ours
                                // while the opponent shows no face-up card (Mereologic Aggregator ③ negating our own Ryzeal).
                                if (_planner.HitsFieldCards(c, e) && OpponentFaceUp() == 0)
                                {
                                    o.Estimate = 0;
                                    o.Info = (o.Info ?? "") + " | 상대 앞면 카드 없음: 자기 카드만 대상";
                                }
                                else o.Estimate = 2;
                            }
                            else if (opponentActed && _planner.Interrupts.IsInterrupt(c, e))
                                o.Estimate = _planner.Interrupts.Value(c, e, o.Location == (int)CardLocation.Hand);
                        }
                        if (opponentActed) BaitOrder(r);
                        break;
                    }

                case RequestKind.SelectCard:
                case RequestKind.SelectUnselect:
                case RequestKind.Tribute:
                    {
                        if (r.Hint == WindBot.Game.AI.HintMsg.AttackTarget && _attacker != null)
                        {
                            // Attack target: the best battle outcome for the declared attacker.
                            foreach (DecisionOption o in r.Options)
                            {
                                ClientCard t = FieldCard(o);
                                if (t != null) o.Estimate = BattleMath.Score(_attacker, t, Battle());
                            }
                            r.NoExplore = true;
                            r.CancelIfNotPositive = true; // the engine lists only legal targets: if all lose, call the attack off
                            break;
                        }
                        r.PreferLow = GiveUpHints.Contains(r.Hint);
                        ZoneSnapshot z = _zones.Snapshot();
                        bool negate = NegateHints.Contains(r.Hint);
                        bool removal = RemovalHints.Contains(r.Hint) || negate;
                        bool anyOpponent = r.Options.Any(o => o.Controller == 1);
                        if ((removal && anyOpponent) || negate) r.PreferLow = true; // destroy / banish / bounce / negate: the opponent's cards first
                        foreach (DecisionOption o in r.Options)
                        {
                            if (o.Controller == 0 && o.CardId != 0)
                            {
                                o.Estimate = _planner.Worth(z, o.CardId);
                                if (o.Location == (int)CardLocation.Overlay)
                                {
                                    // Detaching: what the material fuels on the Xyz monster holding it (Detonator ① > Duo Drive).
                                    ClientCard holder = o.Sequence >= 0 && o.Sequence < _duel.Fields[0].MonsterZone.Length ? _duel.Fields[0].MonsterZone[o.Sequence] : null;
                                    if (holder != null)
                                    {
                                        o.Estimate = _planner.MaterialWorth(holder.Id, holder.Overlays.Count) + o.Estimate * 0.01;
                                        CardEntry hc = _db.Get(holder.Id);
                                        o.Info = (o.Info ?? "") + " | 소재 위치: " + (hc != null ? hc.Data.Name : holder.Id.ToString());
                                    }
                                }
                                else if (o.Location == (int)CardLocation.Extra && r.PreferLow)
                                    o.Estimate = _planner.FodderCost(z, o.CardId); // same order the plan used (e.g. Ext Ryzeal ① sends Eclipse Twins)
                                else if (o.Location == (int)CardLocation.Grave && r.Hint == 507 /* ToDeck */)
                                    o.Estimate = -_planner.ReturnPriority(z, o.CardId); // GY → deck (Cross ②): cards that refuel a search next turn first
                            }
                            else if (o.Controller == 1 && removal)
                            {
                                // Removing an opponent's card: the more it is worth to them, the earlier we pick it.
                                double threat = 0;
                                if (o.CardId != 0 && _db.EnsureCard(o.CardId, true) != null) threat = _planner.Worth(z, o.CardId);
                                o.Estimate = -(OpponentTargetBase + Math.Max(0, threat));
                            }
                        }
                        if (anyOpponent) r.NoExplore = true;
                        if (!r.PreferLow) FollowPlan(r);
                        else if (!anyOpponent && !negate) FollowPlanCost(r);
                        break;
                    }
            }
        }

        /// <summary>
        /// Search / summon / set picks: the card the plan taken in the main phase expects comes first,
        /// so a pick by current worth does not break the plan (e.g. searching Node instead of the planned Star).
        /// </summary>
        private void FollowPlan(DecisionRequest r)
        {
            for (int i = 0; i < _planAhead.Count; ++i)
            {
                int target = _planAhead[i].TargetId;
                if (target == 0) continue;
                DecisionOption hit = r.Options.FirstOrDefault(o => o.Controller == 0 && o.CardId == target);
                if (hit == null) continue;
                hit.Estimate = (double.IsNaN(hit.Estimate) ? 0 : hit.Estimate) + PlanTargetBonus;
                hit.Info = (hit.Info ?? "") + " | 계획대로: " + _planAhead[i].Text;
                _planAhead.RemoveRange(0, i + 1);
                return;
            }
        }

        /// <summary>
        /// Cost picks (send to GY, discard, ...): the hand card the plan paid for the current move goes first,
        /// so a field card the plan still needs is not given up instead (e.g. Ice Ryzeal's cost taking the Sword
        /// meant as the next Xyz material). Only the move being resolved and its triggers are looked at.
        /// </summary>
        private void FollowPlanCost(DecisionRequest r)
        {
            for (int i = 0; i < _planAhead.Count; ++i)
            {
                PlanMove m = _planAhead[i];
                if (i > 0 && m.Kind != "trigger") return;
                // Every card of this move the request can take (a pick of 2, e.g. Cross ② returning 2 from the GY).
                bool any = false;
                foreach (int id in m.CostIds.ToList())
                {
                    DecisionOption hit = r.Options.FirstOrDefault(o => o.Controller == 0 && o.CardId == id && o.Location == (int)CardLocation.Hand && o.Estimate > -PlanTargetBonus / 2)
                                      ?? r.Options.FirstOrDefault(o => o.Controller == 0 && o.CardId == id && !(o.Estimate <= -PlanTargetBonus / 2));
                    if (hit == null) continue;
                    hit.Estimate = (double.IsNaN(hit.Estimate) ? 0 : hit.Estimate) - PlanTargetBonus;
                    hit.Info = (hit.Info ?? "") + " | 계획대로: " + m.Text;
                    m.CostIds.Remove(id);
                    any = true;
                    if (r.Max <= 1) return;
                }
                if (any) return;
            }
        }

        /// <summary>The opponent's latest chain link, if our Main Deck still has a copy of that card (Crossout Designator's name to declare), else 0.</summary>
        private int CrossoutName()
        {
            if (_deckList == null || _values == null) return 0;
            ChainInfo link = _duel.CurrentChainInfo.LastOrDefault(x => x.ActivatePlayer == 1);
            if (link == null) return 0;
            Dictionary<int, int> left = _values.DeckRemaining(_zones);
            foreach (int id in new[] { link.RelatedCard != null ? link.RelatedCard.Id : 0, link.ActivateId, link.ActivateAlias })
            {
                int n;
                if (id != 0 && _deckList.Cards.Contains(id) && left.TryGetValue(id, out n) && n > 0) return id;
            }
            return 0;
        }

        /// <summary>A trap / quick-play in hand that the end-of-turn evaluation counts as set (not one better kept in hand).</summary>
        private bool IsSetInterrupt(int cardId)
        {
            CardEntry c = _db.Get(cardId);
            if (c == null || _values.Weights.Excluded.Contains(cardId)) return false;
            bool trap = (c.Data.Type & 0x4) != 0, quickPlay = (c.Data.Type & 0x10000) != 0;
            if (!trap && !quickPlay) return false;
            ZoneSnapshot z = _zones.Snapshot();
            bool fieldEmpty = z.Monsters.Count == 0 && z.Spells.Count == 0;
            if (trap && c.TrapActInHand && (!c.TrapActInHandNeedsEmptyField || fieldEmpty)) return false;
            return c.Effects.Any(e => e.Kind.StartsWith("카드의 발동") && _planner.Interrupts.IsInterrupt(c, e) && _planner.Interrupts.TypeValue(c, e) > 0);
        }

        // ---------------------------------------------------------------- core call

        private Decision Ask(DecisionRequest request)
        {
            StateSnapshot state = Snapshot();
            Annotate(request);
            Decision decision;
            try
            {
                decision = _policy.Decide(request, state);
            }
            catch (Exception ex)
            {
                _log.Error("policy:" + request.Kind, ex.ToString());
                throw;
            }
            ValuePolicy vp = _policy as ValuePolicy;
            _log.Decision(request, state, decision, vp != null && vp.LastExplored);
            if (request.Kind == RequestKind.Chain)
            {
                // The declared name is only for a Crossout Designator we actually chain.
                bool crossout = !decision.Canceled && decision.Indices.Count > 0 && request.Options[decision.Indices[0]].CardId == CrossoutDesignator;
                if (!crossout) _declareName = 0;
            }
            if (request.Kind == RequestKind.Idle)
            {
                // Remember the rest of the plan taken, for the picks that its moves ask for.
                _planAhead.Clear();
                PlanResult taken;
                if (!decision.Canceled && decision.Indices.Count > 0 && _optionPlans.TryGetValue(request.Options[decision.Indices[0]], out taken))
                    _planAhead.AddRange(taken.Moves);
            }
            return decision;
        }

        private int AskOne(DecisionRequest request)
        {
            request.Shape = RequestShape.One;
            Decision d = Ask(request);
            return d.Canceled || d.Indices.Count == 0 ? -1 : d.Indices[0];
        }

        private static DecisionRequest Request(RequestKind kind)
        {
            DecisionRequest r = new DecisionRequest();
            r.Kind = kind;
            return r;
        }

        private static DecisionOption Option(string action)
        {
            DecisionOption o = new DecisionOption();
            o.Action = action;
            return o;
        }

        private static DecisionOption CardOption(string action, ClientCard card)
        {
            DecisionOption o = Option(action);
            if (card != null)
            {
                o.CardId = card.Id;
                o.Controller = card.Controller;
                o.Location = (int)card.Location;
                o.Sequence = card.Sequence;
                o.Position = card.Position;
            }
            return o;
        }

        /// <summary>System strings of aux.ToHandOrElse's option prompt.</summary>
        private const long StrAddToHand = 573, StrSendToGrave = 574;

        private static void AddYesNo(DecisionRequest r)
        {
            r.Options.Add(Option("Yes"));
            r.Options.Add(Option("No"));
        }

        // ---------------------------------------------------------------- state

        private StateSnapshot Snapshot()
        {
            StateSnapshot s = new StateSnapshot();
            s.Turn = _duel.Turn;
            s.Phase = _duel.Phase.ToString();
            s.TurnPlayer = _duel.Player;
            s.IsFirst = _duel.IsFirst;
            // Before the duel starts (rock-paper-scissors, going first) the fields may not exist yet.
            if (_duel.Fields != null && _duel.Fields.Length >= 2)
            {
                FillPlayer(s.Self, _duel.Fields[0]);
                FillPlayer(s.Opponent, _duel.Fields[1]);
            }
            if (_db != null)
            {
                // Opponent cards enter the card DB the first time they become visible.
                foreach (List<int> ids in new[] { s.Opponent.Hand, s.Opponent.Monsters, s.Opponent.Spells, s.Opponent.Graveyard, s.Opponent.Banished })
                    foreach (int id in ids) _db.EnsureCard(id, true);
            }
            return s;
        }

        private static void FillPlayer(PlayerView view, ClientField field)
        {
            if (field == null) return;
            view.LifePoints = field.LifePoints;
            view.DeckCount = field.Deck != null ? field.Deck.Count : 0;
            AddIds(view.Hand, field.Hand);
            AddIds(view.Monsters, field.MonsterZone);
            AddIds(view.Spells, field.SpellZone);
            AddIds(view.Graveyard, field.Graveyard);
            AddIds(view.Banished, field.Banished);
            AddIds(view.Extra, field.ExtraDeck);
        }

        private static void AddIds(List<int> target, IEnumerable<ClientCard> cards)
        {
            if (cards == null) return;
            foreach (ClientCard c in cards)
                if (c != null) target.Add(c.Id);
        }

        // ---------------------------------------------------------------- notifications

        public void OnJoinGame() { }
        public void OnDeckError(string card) { _log.Error("deck", card); }

        public void OnStart()
        {
            if (_duelFinished)
            {
                BeginDuelLog();
                BuildCardDb(); // a new game gets a fresh card DB
            }
        }

        public void OnDraw(int player) { }
        public void OnNewTurn() { }
        public void OnNewPhase() { }
        public void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation)
        {
            // A card our own effect added from the Deck under "you cannot Normal Summon/Set monsters with its name"
            // (Lightning Crash): the lock lasts until the end of the next turn, and the planner must keep it after the chain.
            if (_db == null || card == null || card.Id == 0 || currentControler != 0) return;
            if (previousLocation != (int)CardLocation.Deck || currentLocation != (int)CardLocation.Hand) return;
            foreach (ChainInfo link in _duel.CurrentChainInfo)
            {
                if (link.ActivatePlayer != 0) continue;
                int id = link.RelatedCard != null && link.RelatedCard.Id != 0 ? link.RelatedCard.Id : link.ActivateId;
                CardEntry c = _db.Get(id);
                EffectEntry e = c != null ? _db.FindEffect(c, link.ActivateDescription) : null;
                if (e == null || !e.BlocksNormalSummonOfAdded) continue;
                _noNormalSummonUntil[card.Id] = _duel.Turn + 1;
                return;
            }
        }
        public void OnDirectAttack(ClientCard card) { }
        public void OnChaining(ClientCard card, int player) { }

        public void OnChainSolved(int chainIndex)
        {
            if (_db != null) _db.OnChainSolved(chainIndex);
        }

        public void OnChainNegated(int chainIndex)
        {
            if (_db != null) _db.OnChainNegated(chainIndex);
        }

        public void OnChainDisabled(int chainIndex)
        {
            if (_db != null) _db.OnChainDisabled(chainIndex);
        }

        public void OnChainEnd()
        {
            if (_db != null) _db.OnChainEnd();
        }

        public void OnSpSummoned()
        {
            if (_db != null && _pendingRuleSummon != 0) _db.OnRuleSummon(_pendingRuleSummon);
            _pendingRuleSummon = 0;
        }
        public void OnPlayerHint(int player, int hintType, ulong description) { }
        public void OnHintZone(int player, int zone) { }
        public void OnReceivingAnnouce(int player, long data) { }
        public void CleanSelectMaterials() { }

        public void OnGameMessage(GameMessage message)
        {
            _log.Message(message.ToString(), _duel.Turn, _duel.Phase.ToString());
            if (_db == null) return;
            try
            {
                if (message == GameMessage.Chaining && _duel.CurrentChainInfo.Count > 0)
                {
                    // GameBehavior has just appended the new chain link.
                    ChainInfo info = _duel.CurrentChainInfo[_duel.CurrentChainInfo.Count - 1];
                    int id = info.RelatedCard != null && info.RelatedCard.Id != 0 ? info.RelatedCard.Id : info.ActivateId;
                    _db.OnChaining(_duel.CurrentChainInfo.Count, info.ActivatePlayer, id, info.ActivateDescription);
                }
                else if (message == GameMessage.NewTurn)
                {
                    if (_db.Turn > 0) AppendState("턴 종료");
                    _db.OnNewTurn(_duel.Turn);
                }
            }
            catch (Exception ex)
            {
                _log.Error("carddb-event:" + message, ex.ToString());
            }
        }

        public void OnWin(int result)
        {
            _log.Result(result == 2 ? "Draw" : result == 0 ? "Win" : "Lose", _duel.Turn);
            AppendState("듀얼 종료: " + (result == 2 ? "무승부" : result == 0 ? "승리" : "패배"));
            WriteCardDb();
            _duelFinished = true;
        }

        // ---------------------------------------------------------------- decisions

        public int OnRockPaperScissors()
        {
            DecisionRequest r = Request(RequestKind.RockPaperScissors);
            for (int v = 1; v <= 3; ++v)
            {
                DecisionOption o = Option("Hand");
                o.Value = v;
                r.Options.Add(o);
            }
            int i = AskOne(r);
            return i < 0 ? 1 : (int)r.Options[i].Value;
        }

        public bool OnSelectHand()
        {
            DecisionRequest r = Request(RequestKind.SelectFirst);
            r.Options.Add(Option("GoFirst"));
            r.Options.Add(Option("GoSecond"));
            return AskOne(r) != 1;
        }

        public MainPhaseAction OnSelectIdleCmd(MainPhase main)
        {
            DecisionRequest r = Request(RequestKind.Idle);
            List<MainPhaseAction> actions = new List<MainPhaseAction>();

            AddIdle(r, actions, main.SummonableCards, "Summon", MainPhaseAction.MainAction.Summon, 0);
            AddIdle(r, actions, main.SpecialSummonableCards, "SpSummon", MainPhaseAction.MainAction.SpSummon, 1);
            AddIdle(r, actions, main.ReposableCards, "Repos", MainPhaseAction.MainAction.Repos, 2);
            AddIdle(r, actions, main.MonsterSetableCards, "SetMonster", MainPhaseAction.MainAction.SetMonster, 3);
            AddIdle(r, actions, main.SpellSetableCards, "SetSpell", MainPhaseAction.MainAction.SetSpell, 4);
            for (int i = 0; i < main.ActivableCards.Count; ++i)
            {
                ClientCard card = main.ActivableCards[i];
                long desc = main.ActivableDescs[i];
                int index;
                if (!card.ActionActivateIndex.TryGetValue(desc, out index)) index = card.ActionIndex[5];
                DecisionOption o = CardOption("Activate", card);
                o.Description = desc;
                r.Options.Add(o);
                actions.Add(new MainPhaseAction(MainPhaseAction.MainAction.Activate, index));
            }
            if (main.CanBattlePhase)
            {
                r.Options.Add(Option("ToBattlePhase"));
                actions.Add(new MainPhaseAction(MainPhaseAction.MainAction.ToBattlePhase));
            }
            if (main.CanEndPhase)
            {
                r.Options.Add(Option("ToEndPhase"));
                actions.Add(new MainPhaseAction(MainPhaseAction.MainAction.ToEndPhase));
            }

            _pendingRuleSummon = 0;
            int chosen = AskOne(r);
            if (chosen < 0) return new MainPhaseAction(MainPhaseAction.MainAction.ToEndPhase);

            // Bookkeeping the engine does not report: rule summons from hand and the normal summon.
            DecisionOption picked = r.Options[chosen];
            if (_db != null)
            {
                if (picked.Action == "SpSummon" && picked.Location == (int)CardLocation.Hand) _pendingRuleSummon = picked.CardId;
                if (picked.Action == "Summon" || picked.Action == "SetMonster") _db.OnNormalSummon(picked.CardId);
            }
            return actions[chosen];
        }

        private static void AddIdle(DecisionRequest r, List<MainPhaseAction> actions, IList<ClientCard> cards,
            string label, MainPhaseAction.MainAction action, int slot)
        {
            foreach (ClientCard card in cards)
            {
                r.Options.Add(CardOption(label, card));
                actions.Add(new MainPhaseAction(action, card.ActionIndex[slot]));
            }
        }

        public BattlePhaseAction OnSelectBattleCmd(BattlePhase battle)
        {
            DecisionRequest r = Request(RequestKind.Battle);
            List<BattlePhaseAction> actions = new List<BattlePhaseAction>();
            for (int i = 0; i < battle.ActivableCards.Count; ++i)
            {
                ClientCard card = battle.ActivableCards[i];
                DecisionOption o = CardOption("Activate", card);
                o.Description = battle.ActivableDescs[i];
                r.Options.Add(o);
                actions.Add(new BattlePhaseAction(BattlePhaseAction.BattleAction.Activate, card.ActionIndex));
            }
            List<ClientCard> defenders = _duel.Fields[1].GetMonsters();
            BattleContext bc = Battle();
            foreach (ClientCard card in battle.AttackableCards)
            {
                DecisionOption o = CardOption("Attack", card);
                o.Value = card.CanDirectAttack ? 1 : 0;
                ClientCard target;
                o.Estimate = BattleMath.Best(card, defenders, bc, out target);
                o.Info = "전투 " + (target == null ? "직접 공격" : "→ " + target.Name + (target.IsDefense() ? " 수비 " + target.Defense : " 공격 " + target.Attack)) + " (공 " + card.Attack + ")";
                r.Options.Add(o);
                actions.Add(new BattlePhaseAction(BattlePhaseAction.BattleAction.Attack, card.ActionIndex));
            }
            if (battle.CanMainPhaseTwo)
            {
                r.Options.Add(Option("ToMainPhaseTwo"));
                actions.Add(new BattlePhaseAction(BattlePhaseAction.BattleAction.ToMainPhaseTwo));
            }
            if (battle.CanEndPhase)
            {
                r.Options.Add(Option("ToEndPhase"));
                actions.Add(new BattlePhaseAction(BattlePhaseAction.BattleAction.ToEndPhase));
            }

            int chosen = AskOne(r);
            _attacker = chosen >= 0 && r.Options[chosen].Action == "Attack" ? battle.AttackableCards.FirstOrDefault(c => c.Id == r.Options[chosen].CardId && c.Sequence == r.Options[chosen].Sequence) : null;
            return chosen < 0 ? new BattlePhaseAction(BattlePhaseAction.BattleAction.ToEndPhase) : actions[chosen];
        }

        /// <summary>
        /// Our zones plus what the planner's battle term needs: whether our Battle Phase is still ahead, LP, and the
        /// opponent's monsters with their stats, held interruptions and battle protection (same reading as the battle math).
        /// </summary>
        private ZoneSnapshot PlanSnapshot()
        {
            ZoneSnapshot z = _zones.Snapshot();
            foreach (var kv in _noNormalSummonUntil) if (_duel.Turn <= kv.Value) z.NoNormalSummon.Add(kv.Key);
            ClientField opp = _duel.Fields[1];
            if (opp == null || _duel.Fields[0] == null) return z;
            z.CanAttack = _duel.Turn > 1 && _duel.Player == 0 && _duel.Phase == DuelPhase.Main1;
            z.OwnLp = _duel.Fields[0].LifePoints;
            z.OpponentLp = opp.LifePoints;
            foreach (ClientCard c in opp.MonsterZone)
            {
                if (c == null) continue;
                OpponentMonster m = new OpponentMonster { Id = c.Id, Atk = c.Attack, Def = c.Defense, AttackPosition = c.IsAttack(), FaceUp = c.IsFaceup() };
                if (c.IsFaceup() && c.Id != 0)
                {
                    m.Held = HeldInterrupts(c);
                    KeyValuePair<GuardState, double> g = GuardOf(c);
                    m.Indestructible = g.Key == GuardState.Indestructible;
                    if (g.Key == GuardState.Replace) m.ReplaceCost = g.Value;
                }
                z.OpponentMonsters.Add(m);
            }
            foreach (ClientCard c in opp.Graveyard)
                if (c != null && c.HasType(CardType.Xyz)) z.OpponentGraveXyzRanks += c.Rank;
            return z;
        }

        /// <summary>Battle math inputs: LP, monster worth (interruptions held on the field) and battle protection from Lua.</summary>
        private BattleContext Battle()
        {
            BattleContext bc = new BattleContext();
            bc.OwnLp = _duel.Fields[0].LifePoints;
            bc.OpponentLp = _duel.Fields[1].LifePoints;
            if (_values == null || _db == null || _planner == null) return bc;
            bc.LpValue = _values.Weights.BaseOf("battle.lp");
            double destroy = _values.Weights.BaseOf("battle.destroy");
            bc.Worth = c => destroy + HeldInterrupts(c);
            bc.Guard = GuardOf;
            return bc;
        }

        /// <summary>Interruptions a monster still offers from the field (e.g. Detonator ② per material, Rafflesia).</summary>
        private double HeldInterrupts(ClientCard c)
        {
            CardEntry ce = c == null ? null : _db.EnsureCard(c.Id, c.Controller == 1);
            if (ce == null || c.IsFacedown()) return 0;
            double v = 0;
            foreach (EffectEntry e in ce.Effects)
            {
                if (e.Range == null || !e.Range.Contains("MZONE") || !_planner.Interrupts.IsInterrupt(ce, e)) continue;
                if (e.DetachCount > 0 && c.Overlays.Count < e.DetachCount) continue;
                KeyUsage u = _db.PeekUsage(c.Controller, e);
                if (u != null && e.Limit != null && u.Used >= e.Limit.Count) continue;
                v += _planner.Interrupts.Value(ce, e, false);
            }
            return v;
        }

        /// <summary>
        /// Battle protection of a monster from its own side: indestructible (Lua EFFECT_INDESTRUCTABLE_BATTLE) or a destroy
        /// replacement (EFFECT_DESTROY_REPLACE, e.g. Detonator ③ for its side's Xyz), with the replacement's cost (the material it uses).
        /// </summary>
        private KeyValuePair<GuardState, double> GuardOf(ClientCard target)
        {
            KeyValuePair<GuardState, double> best = new KeyValuePair<GuardState, double>(GuardState.None, 0);
            CardEntry te = _db.EnsureCard(target.Id, target.Controller == 1);
            ClientField side = _duel.Fields[target.Controller];
            foreach (ClientCard holder in side.GetMonsters().Concat(side.GetSpells()))
            {
                if (holder == null || holder.IsFacedown()) continue;
                CardEntry he = _db.EnsureCard(holder.Id, holder.Controller == 1);
                if (he == null) continue;
                foreach (EffectEntry e in he.Effects)
                {
                    BattleGuard g = e.Guard;
                    if (g == null || !_db.GuardCovers(g, he, te)) continue;
                    if (g.SelfOnly && holder != target) continue;
                    if (g.NeedsMaterial && holder.Overlays.Count == 0) continue;
                    KeyUsage u = _db.PeekUsage(target.Controller, e);
                    if (u != null && e.Limit != null && u.Used >= e.Limit.Count) continue;
                    if (!g.Replace) return new KeyValuePair<GuardState, double>(GuardState.Indestructible, 0);
                    if (best.Key == GuardState.None)
                        best = new KeyValuePair<GuardState, double>(GuardState.Replace, _planner.MaterialWorth(holder.Id, holder.Overlays.Count));
                }
            }
            return best;
        }

        /// <summary>The field card an option points at (monster zone only).</summary>
        private ClientCard FieldCard(DecisionOption o)
        {
            if (o.Controller < 0 || o.Controller > 1 || o.Location != (int)CardLocation.MonsterZone) return null;
            ClientCard[] zone = _duel.Fields[o.Controller].MonsterZone;
            return o.Sequence >= 0 && o.Sequence < zone.Length ? zone[o.Sequence] : null;
        }

        /// <summary>
        /// Changing a face-up monster's position: to attack when it has a winning attack now,
        /// to defense when it has none and its DEF is the higher stat.
        /// </summary>
        private bool ReposWanted(DecisionOption o)
        {
            ClientCard c = FieldCard(o);
            if (c == null || c.IsFacedown()) return false; // flip summons are left to the plan
            ClientCard target;
            double best = BattleMath.Best(c, _duel.Fields[1].GetMonsters(), Battle(), out target);
            if (c.IsDefense()) return best > 0;
            return best <= 0 && c.Defense > c.Attack;
        }

        public int OnSelectChain(IList<ClientCard> cards, IList<long> descs, bool forced, int timing)
        {
            DecisionRequest r = Request(RequestKind.Chain);
            r.Hint = timing;
            for (int i = 0; i < cards.Count; ++i)
            {
                DecisionOption o = CardOption("Activate", cards[i]);
                o.Description = descs[i];
                r.Options.Add(o);
            }
            if (!forced) r.Options.Add(Option("Pass"));
            int chosen = AskOne(r);
            if (chosen < 0 || chosen >= cards.Count) return forced ? 0 : -1;
            return chosen;
        }

        public bool OnSelectEffectYn(ClientCard card, long desc)
        {
            DecisionRequest r = Request(RequestKind.EffectYn);
            r.Description = desc;
            r.SourceCardId = card != null ? card.Id : 0;
            AddYesNo(r);
            return AskOne(r) == 0;
        }

        public bool OnSelectYesNo(long desc)
        {
            DecisionRequest r = Request(RequestKind.YesNo);
            r.Description = desc;
            AddYesNo(r);
            return AskOne(r) == 0;
        }

        public bool OnSelectBattleReplay()
        {
            DecisionRequest r = Request(RequestKind.BattleReplay);
            AddYesNo(r);
            return AskOne(r) == 0;
        }

        public int OnSelectOption(IList<long> options)
        {
            DecisionRequest r = Request(RequestKind.Option);
            foreach (long desc in options)
            {
                DecisionOption o = Option("Option");
                o.Description = desc;
                r.Options.Add(o);
            }
            int chosen = AskOne(r);
            return chosen < 0 ? 0 : chosen;
        }

        public IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, long hint, bool cancelable)
        {
            return SelectMany(RequestKind.SelectCard, cards, min, max, hint, cancelable);
        }

        public IList<ClientCard> OnSelectUnselectCard(IList<ClientCard> cards, int min, int max, long hint, bool cancelable)
        {
            return SelectMany(RequestKind.SelectUnselect, cards, min, max, hint, cancelable);
        }

        public IList<ClientCard> OnSelectTribute(IList<ClientCard> cards, int min, int max, long hint, bool cancelable)
        {
            return SelectMany(RequestKind.Tribute, cards, min, max, hint, cancelable);
        }

        private IList<ClientCard> SelectMany(RequestKind kind, IList<ClientCard> cards, int min, int max, long hint, bool cancelable)
        {
            DecisionRequest r = Request(kind);
            r.Shape = RequestShape.Many;
            r.Min = min;
            r.Max = max;
            r.Hint = hint;
            r.Cancelable = cancelable;
            foreach (ClientCard card in cards) r.Options.Add(CardOption("Card", card));
            Decision d = Ask(r);
            List<ClientCard> selected = new List<ClientCard>();
            if (!d.Canceled)
                foreach (int i in d.Indices) selected.Add(cards[i]);
            return selected;
        }

        public IList<ClientCard> OnSelectSum(IList<ClientCard> cards, int sum, int min, int max, long hint, bool mode)
        {
            DecisionRequest r = Request(RequestKind.Sum);
            r.Shape = RequestShape.Sum;
            r.Min = min;
            r.Max = max;
            r.Hint = hint;
            r.Target = sum;
            r.ExactSum = mode;
            foreach (ClientCard card in cards)
            {
                DecisionOption o = CardOption("Card", card);
                o.Param1 = card.OpParam1;
                o.Param2 = card.OpParam2;
                r.Options.Add(o);
            }
            Decision d = Ask(r);
            List<ClientCard> selected = new List<ClientCard>();
            if (d.Canceled) _log.Error("sum", "no subset meets target " + sum);
            else foreach (int i in d.Indices) selected.Add(cards[i]);
            return selected;
        }

        public IList<int> OnSelectCounter(int type, int quantity, IList<ClientCard> cards, IList<int> counters)
        {
            DecisionRequest r = Request(RequestKind.Counter);
            r.Shape = RequestShape.Counter;
            r.Quantity = quantity;
            r.Hint = type;
            for (int i = 0; i < cards.Count; ++i)
            {
                DecisionOption o = CardOption("Card", cards[i]);
                o.Param1 = counters[i];
                r.Options.Add(o);
            }
            Decision d = Ask(r);
            List<int> used = new List<int>();
            for (int i = 0; i < cards.Count; ++i) used.Add(i < d.Indices.Count ? d.Indices[i] : 0);
            return used;
        }

        public int OnSelectPlace(long cardId, int player, CardLocation location, int available)
        {
            DecisionRequest r = Request(RequestKind.Place);
            r.Hint = cardId;
            for (int bit = 0; bit < 31; ++bit)
            {
                if ((available & (1 << bit)) == 0) continue;
                DecisionOption o = Option("Zone");
                o.Controller = player;
                o.Location = (int)location;
                o.Sequence = bit;
                o.Value = 1 << bit;
                r.Options.Add(o);
            }
            int chosen = AskOne(r);
            return chosen < 0 ? 0 : (int)r.Options[chosen].Value;
        }

        public CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
        {
            DecisionRequest r = Request(RequestKind.Position);
            r.SourceCardId = cardId;
            foreach (CardPosition p in positions)
            {
                DecisionOption o = Option("Position");
                o.Value = (int)p;
                r.Options.Add(o);
            }
            int chosen = AskOne(r);
            return chosen < 0 ? positions[0] : positions[chosen];
        }

        public IList<ClientCard> OnCardSorting(IList<ClientCard> cards)
        {
            DecisionRequest r = Request(RequestKind.SortCard);
            r.Shape = RequestShape.Order;
            r.Min = r.Max = cards.Count;
            foreach (ClientCard card in cards) r.Options.Add(CardOption("Card", card));
            Decision d = Ask(r);
            List<ClientCard> ordered = new List<ClientCard>();
            foreach (int i in d.Indices) ordered.Add(cards[i]);
            return ordered.Count == cards.Count ? ordered : new List<ClientCard>(cards);
        }

        public int OnAnnounceCard(IList<int> avail)
        {
            DecisionRequest r = Request(RequestKind.AnnounceCard);
            foreach (int id in avail)
            {
                DecisionOption o = Option("Announce");
                o.CardId = id;
                o.Value = id;
                if (_declareName != 0) o.Estimate = id == _declareName ? 1 : 0; // the name picked when chaining Crossout Designator
                r.Options.Add(o);
            }
            _declareName = 0;
            int chosen = AskOne(r);
            return avail[chosen < 0 ? 0 : chosen];
        }

        public int OnAnnounceNumber(IList<int> numbers)
        {
            DecisionRequest r = Request(RequestKind.AnnounceNumber);
            foreach (int n in numbers)
            {
                DecisionOption o = Option("Number");
                o.Value = n;
                r.Options.Add(o);
            }
            int chosen = AskOne(r);
            return chosen < 0 ? 0 : chosen; // GameBehavior expects an index
        }

        public IList<CardAttribute> OnAnnounceAttrib(int count, IList<CardAttribute> attributes)
        {
            DecisionRequest r = Request(RequestKind.AnnounceAttribute);
            r.Shape = RequestShape.Many;
            r.Min = r.Max = count;
            foreach (CardAttribute a in attributes)
            {
                DecisionOption o = Option("Attribute");
                o.Value = (int)a;
                r.Options.Add(o);
            }
            Decision d = Ask(r);
            List<CardAttribute> chosen = new List<CardAttribute>();
            foreach (int i in d.Indices) chosen.Add(attributes[i]);
            foreach (CardAttribute a in attributes) if (chosen.Count < count && !chosen.Contains(a)) chosen.Add(a);
            return chosen;
        }

        public IList<CardRace> OnAnnounceRace(int count, IList<CardRace> races)
        {
            DecisionRequest r = Request(RequestKind.AnnounceRace);
            r.Shape = RequestShape.Many;
            r.Min = r.Max = count;
            foreach (CardRace race in races)
            {
                DecisionOption o = Option("Race");
                o.Value = (long)race;
                r.Options.Add(o);
            }
            Decision d = Ask(r);
            List<CardRace> chosen = new List<CardRace>();
            foreach (int i in d.Indices) chosen.Add(races[i]);
            foreach (CardRace race in races) if (chosen.Count < count && !chosen.Contains(race)) chosen.Add(race);
            return chosen;
        }
    }
}
