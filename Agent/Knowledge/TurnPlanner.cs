using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WindBot.Agent.Knowledge
{
    /// <summary>Our own visible cards, as plain ids, for the planner.</summary>
    public sealed class ZoneSnapshot
    {
        public readonly List<int> Hand = new List<int>();
        /// <summary>Monster id and attached material count.</summary>
        public readonly List<KeyValuePair<int, int>> Monsters = new List<KeyValuePair<int, int>>();
        /// <summary>Attached material ids per monster, index-aligned with Monsters (missing or short = the rest unknown).</summary>
        public readonly List<List<int>> MonsterMaterials = new List<List<int>>();
        public readonly List<int> Spells = new List<int>();
        /// <summary>Face-down spells that do nothing until activated (field / continuous / normal spells). Set traps and quick-plays stay in Spells.</summary>
        public readonly List<int> SetSpells = new List<int>();
        public readonly List<int> Grave = new List<int>();
        public readonly List<int> Banished = new List<int>();
        public bool NormalSummonAvailable = true;
        /// <summary>Opponent's face-up cards on the field, or -1 if not known (offline tests).</summary>
        public int OpponentFaceUp = -1;
        /// <summary>Our Battle Phase is still ahead this turn (not the duel's first turn, not past Main Phase 1).</summary>
        public bool CanAttack;
        public int OwnLp = 8000;
        public int OpponentLp = 8000;
        public readonly List<OpponentMonster> OpponentMonsters = new List<OpponentMonster>();
        /// <summary>Sum of the Ranks of the Xyz monsters in the opponent's GY (F0's ATK).</summary>
        public int OpponentGraveXyzRanks;
        /// <summary>Cards an earlier effect forbids us to Normal Summon (Lightning Crash's searched name).</summary>
        public readonly HashSet<int> NoNormalSummon = new HashSet<int>();
    }

    /// <summary>An opponent's monster as the planner's battle term sees it.</summary>
    public sealed class OpponentMonster
    {
        public int Id;
        public int Atk;
        public int Def;
        public bool AttackPosition;
        public bool FaceUp;
        /// <summary>Interruptions it still offers from the field (lost while its effects are negated).</summary>
        public double Held;
        public bool Indestructible;
        /// <summary>A destroy replacement protects it once, at this cost to its controller (Detonator: a material).</summary>
        public double ReplaceCost = -1;

        public OpponentMonster Clone() { return (OpponentMonster)MemberwiseClone(); }
    }

    // ------------------------------------------------------------------ interruptions

    /// <summary>
    /// Value of an effect as an interruption on the opponent's turn (VALUE_MODEL.md 7장).
    /// Type comes from the Lua categories (or a per-card override), then scope / battle-only / bonuses.
    /// </summary>
    public sealed class InterruptModel
    {
        private readonly ValueWeights _w;
        public InterruptModel(ValueWeights w) { _w = w; }

        /// <summary>Can this effect be used on the opponent's turn at all?</summary>
        public bool IsInterrupt(CardEntry c, EffectEntry e)
        {
            double o;
            if (_w.TryCardInt("ib", c.Data.Id, e.Index, out o)) return true;
            if (e.Kind.StartsWith("유발 즉시")) return true;
            if (e.Kind.Contains("DESTROY_REPLACE")) return true;
            if (e.Kind.StartsWith("카드의 발동"))
            {
                bool trap = (c.Data.Type & 0x4) != 0, quickPlay = (c.Data.Type & 0x10000) != 0;
                return (trap || quickPlay) && TypeValue(c, e) > 0;
            }
            // Triggers that can fire on the opponent's turn (e.g. "when sent to the GY").
            if (e.Kind.StartsWith("유발 효과") && e.Events.Any(ev => ev.Contains("TO_GRAVE") || ev.Contains("DESTROYED") || ev.Contains("LEAVE_FIELD")))
                return TypeValue(c, e) > 0;
            return false;
        }

        public string TypeName(CardEntry c, EffectEntry e)
        {
            double o;
            if (_w.TryCardInt("ib", c.Data.Id, e.Index, out o)) return "설정값";
            if (e.Kind.Contains("DESTROY_REPLACE")) return "방해 보호";
            var cat = e.Categories;
            bool neg = cat.Contains("NEGATE"), dis = cat.Contains("DISABLE"), des = cat.Contains("DESTROY");
            if (cat.Contains("DRAW") && (c.Data.Type & 0x1) != 0) return "드로우형";
            if (cat.Contains("CONTROL")) return "컨트롤 탈취";
            if (neg && des) return "발동 무효 후 파괴";
            if (dis && des) return "효과 무효 후 파괴";
            if (dis) return "효과 무효";
            if (neg) return "발동 무효";
            if (cat.Contains("REMOVE")) return "단순 제외";
            if (cat.Contains("TODECK") && !cat.Contains("SEARCH")) return "덱으로 바운스";
            if (cat.Contains("TOHAND") && !cat.Contains("SEARCH")) return "패로 바운스";
            if (des) return "단순 파괴";
            return "미분류";
        }

        /// <summary>A negation / removal that needs a card on the field to hit (the opponent's, on our own turn).</summary>
        public bool HitsFieldCards(CardEntry c, EffectEntry e)
        {
            string t = TypeName(c, e);
            return e.NegateScope != null || t == "단순 파괴" || t == "단순 제외" || t == "패로 바운스" || t == "덱으로 바운스" || t == "컨트롤 탈취"
                || t == "효과 무효" || t == "발동 무효" || t == "효과 무효 후 파괴" || t == "발동 무효 후 파괴";
        }

        /// <summary>Base value by type (before scope and bonuses).</summary>
        public double TypeValue(CardEntry c, EffectEntry e)
        {
            double o;
            if (_w.TryCardInt("ib", c.Data.Id, e.Index, out o)) return o;
            switch (TypeName(c, e))
            {
                case "방해 보호": return _w.BaseOf("int.protect");
                case "드로우형": return _w.BaseOf("int.draw");
                case "발동 무효 후 파괴": return _w.BaseOf("int.negate_activation_destroy");
                case "효과 무효 후 파괴": return _w.BaseOf("int.negate_effect_destroy");
                case "효과 무효": return _w.BaseOf("int.negate_effect");
                case "발동 무효": return _w.BaseOf("int.negate_activation");
                case "단순 제외": return _w.BaseOf("int.banish");
                case "단순 파괴": return _w.BaseOf("int.destroy");
                case "패로 바운스": return _w.BaseOf("int.bounce_hand");
                case "덱으로 바운스": return _w.BaseOf("int.bounce_deck");
                case "컨트롤 탈취": return _w.BaseOf("int.control");
                default: return 0;
            }
        }

        /// <summary>One use of the effect as an interruption. inHand adds the hand-trap bonus.</summary>
        public double Value(CardEntry c, EffectEntry e, bool inHand)
        {
            double v = TypeValue(c, e), x;
            v *= ScopeFactor(e);
            if (_w.TryCardInt("is", c.Data.Id, e.Index, out x)) v *= x;
            if (Reactive(c, e)) v *= _w.BaseOf("int.reactive_factor");
            if (_w.TryCardInt("ibt", c.Data.Id, e.Index, out x) && x != 0) v *= _w.BaseOf("int.battle_factor");
            if (_w.TryCardInt("ia", c.Data.Id, e.Index, out x)) v += x;
            if (inHand) v += _w.BaseOf("int.handtrap_bonus");
            return v;
        }

        /// <summary>What a negation can hit (from Lua): all ×int.scope.all, monster effects ×int.scope.monster, spell/trap effects ×int.scope.spelltrap. 1 for non-negations.</summary>
        public double ScopeFactor(EffectEntry e)
        {
            return e.NegateScope == null ? 1 : _w.BaseOf("int.scope." + e.NegateScope);
        }

        /// <summary>
        /// Timing "narrow": a removal (destroy, banish, bounce, control) that can only answer an activation (CHAINING), so the activated effect still resolves.
        /// Negations are not narrowed: answering the activation is how they work.
        /// </summary>
        public bool Reactive(CardEntry c, EffectEntry e)
        {
            double o;
            if (_w.TryCardInt("ib", c.Data.Id, e.Index, out o)) return false;
            string t = TypeName(c, e);
            bool removal = t == "단순 파괴" || t == "단순 제외" || t == "패로 바운스" || t == "덱으로 바운스" || t == "컨트롤 탈취";
            return removal && e.Events.Any(ev => ev.Contains("CHAINING"));
        }

        public bool MultipliesByCondition(CardEntry c, EffectEntry e)
        {
            double x;
            return _w.TryCardInt("imc", c.Data.Id, e.Index, out x) && x != 0;
        }
    }

    // ------------------------------------------------------------------ planner

    public sealed class PlanMove
    {
        public string Kind;   // ns, rule, spell, ignition, xyz, trigger
        public int CardId;
        public int EffectIndex;
        /// <summary>The card this move searches, summons or sets (0 if none): picks during the move follow it.</summary>
        public int TargetId;
        /// <summary>Hand cards the plan sends as this move's cost: the engine's cost pick follows them.</summary>
        public readonly List<int> CostIds = new List<int>();
        public string Text;
        public double Gain;
    }

    public sealed class PlanResult
    {
        public double Value;
        public double FlowValue;
        public double EndValue;
        /// <summary>Follow-up worth of the cards still in hand at the end of the turn (next turn's use).</summary>
        public double FollowValue;
        /// <summary>Battle Phase still ahead: destroying the opponent's monsters and damage with the end board (breakthrough).</summary>
        public double BattleValue;
        public readonly List<string> Battle = new List<string>();
        public int HandSpent;
        /// <summary>The search budget ran out somewhere below this result, so it may be an underestimate (not memoised).</summary>
        public bool Truncated;
        public readonly List<PlanMove> Moves = new List<PlanMove>();
        public readonly List<string> EndBoard = new List<string>();

        public string Describe()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Value.ToString("0.#", CultureInfo.InvariantCulture))
              .Append(" (흐름 ").Append(FlowValue.ToString("0.#", CultureInfo.InvariantCulture))
              .Append(" + 턴 종료 방해 ").Append(EndValue.ToString("0.#", CultureInfo.InvariantCulture))
              .Append(" + 후속 ").Append(FollowValue.ToString("0.#", CultureInfo.InvariantCulture));
            if (BattleValue != 0) sb.Append(" + 전투 ").Append(BattleValue.ToString("0.#", CultureInfo.InvariantCulture));
            sb.Append(" − 효율 ").Append(HandSpent).Append("장)");
            if (Moves.Count > 0) sb.Append(" 행동: ").Append(String.Join(" → ", Moves.Select(m => m.Text).ToArray()));
            if (EndBoard.Count > 0) sb.Append(" | 방해: ").Append(String.Join(", ", EndBoard.ToArray()));
            if (Battle.Count > 0) sb.Append(" | 전투: ").Append(String.Join(", ", Battle.ToArray()));
            return sb.ToString();
        }
    }

    /// <summary>
    /// Searches our own turn (going first) for the plan with the highest
    ///   Σ γ^k × (function value of each step)  +  interruptions available at end of turn  −  efficiency × hand cards spent.
    /// Moves: normal summon, rule summon from hand, activate a spell from hand, field ignition effects, Xyz summon,
    /// and the optional triggers each arrival opens. The engine stays the authority; this is an approximation
    /// of resources and constraints (VALUE_MODEL.md 2.4 boundary).
    /// </summary>
    public sealed class TurnPlanner
    {
        private readonly GameCardDb _db;
        private readonly ValueWeights _w;
        private readonly InterruptModel _int;
        private int _budget;
        /// <summary>Finished sub-results by depth and state; one table shared by the workers of a parallel plan.</summary>
        private sealed class MemoTable
        {
            public readonly System.Collections.Concurrent.ConcurrentDictionary<string, PlanResult> D =
                new System.Collections.Concurrent.ConcurrentDictionary<string, PlanResult>(StringComparer.Ordinal);
            public int Count;
        }
        private MemoTable _memo;
        public int BudgetPerFirstMove = 40000;
        public int MaxDepth = 16; // main moves per turn (a Duo Drive + 2 Detonator line already takes about 10)
        public long MillisPerPlan = 4000; // wall-clock cap for one Plan call, shared evenly by the first moves left (results past it count as truncated)
        /// <summary>First moves searched at once, each by its own worker planner (1 = one after another on this thread).</summary>
        public int Threads = Math.Max(1, Math.Min(16, Environment.ProcessorCount - 2));
        /// <summary>Most memo entries one plan keeps (shared by its workers): bounds memory in the 32-bit process.</summary>
        public int MemoCap = 600000; // about 640 MB at the peak of a 30 s parallel plan
        /// <summary>
        /// The plan taken before (its moves, the ones already made included). Searched first wherever the state
        /// still follows it, so a short re-plan always re-reads that line in full instead of dropping it when time runs out.
        /// </summary>
        public List<PlanMove> Hint;
        private List<PlanMove> _hint;
        private readonly System.Diagnostics.Stopwatch _clock = new System.Diagnostics.Stopwatch();
        private long _moveDeadline;

        public TurnPlanner(GameCardDb db, ValueWeights w)
        {
            _db = db;
            _w = w;
            _int = new InterruptModel(w);
            _payoff = new GravePayoffReader(db, w, HitsFieldCards);
        }

        private readonly GravePayoffReader _payoff;

        public InterruptModel Interrupts { get { return _int; } }

        /// <summary>
        /// Undiscounted next-turn value of a card in hand (ValueModel.ValueNextTurn). Null: no follow-up term.
        /// Looked up once per card per Plan call.
        /// </summary>
        public Func<int, double> NextTurnValue;
        private readonly Dictionary<int, double> _next = new Dictionary<int, double>();

        private double NextTurn(int id)
        {
            if (NextTurnValue == null) return 0;
            double v;
            if (!_next.TryGetValue(id, out v)) { v = NextTurnValue(id); _next[id] = v; }
            return v;
        }

        /// <summary>Follow-up worth of the cards in hand: per card name, follow.discount × next-turn value, further copies × follow.duplicate.</summary>
        private double FollowTerm(St s)
        {
            double total = 0;
            foreach (var kv in s.Hand.Where(x => x.Value > 0))
            {
                if (_w.Excluded.Contains(kv.Key)) continue;
                double v = NextTurn(kv.Key);
                if (v <= 0) continue;
                for (int k = 1; k <= kv.Value; ++k) total += ValueModel.FollowOf(_w, v, k);
            }
            return total;
        }

        // ---------------------------------------------------------- state

        private sealed class Mon
        {
            public int Id;
            /// <summary>Attached materials as card ids, oldest first (0 = a material whose identity is not known).</summary>
            public List<int> MatIds = new List<int>();
            public int Mats { get { return MatIds.Count; } }
            public bool Negated;
            public Mon Clone() { return new Mon { Id = Id, MatIds = new List<int>(MatIds), Negated = Negated }; }
            /// <summary>Order-free material signature, for state keys and duplicate checks.</summary>
            public string MatSig() { return String.Join("/", MatIds.OrderBy(x => x).Select(x => x.ToString()).ToArray()); }
            /// <summary>This monster as a material of a new Xyz: its own materials, then itself.</summary>
            public IEnumerable<int> AsMaterial() { return MatIds.Concat(new[] { Id }); }
        }

        private sealed class St
        {
            public Dictionary<int, int> Hand = new Dictionary<int, int>();
            public Dictionary<int, int> Orig = new Dictionary<int, int>();
            public List<Mon> Mons = new List<Mon>();
            public List<int> Spells = new List<int>();
            public List<int> Inactive = new List<int>(); // set spells not yet activated
            public Dictionary<int, int> Grave = new Dictionary<int, int>();
            public Dictionary<int, int> Deck = new Dictionary<int, int>();
            public Dictionary<int, int> Extra = new Dictionary<int, int>();
            public HashSet<string> Used = new HashSet<string>(StringComparer.Ordinal);
            public HashSet<int> NoNormalSummon = new HashSet<int>();
            /// <summary>Turn locks already on us: Extra Deck only Xyz of this Rank / attacks only by Xyz of this Rank (-1 none, -2 nothing).</summary>
            public int ExtraRank = -1;
            public int AttackRank = -1;
            public bool NS;
            public int HandSpent;
            /// <summary>Cards drawn by our effects this turn (unknown cards: valued by the deck's expected draw at the end).</summary>
            public int Draws;
            /// <summary>Cards put on the bottom of the deck: searchable again, not drawn.</summary>
            public List<int> Bottom = new List<int>();
            /// <summary>End-of-turn worth already fixed by a move (e.g. a card returned to the deck refuels a search next turn).</summary>
            public double Bonus;

            public St Clone()
            {
                St s = new St { NS = NS, HandSpent = HandSpent, Draws = Draws, Bonus = Bonus, ExtraRank = ExtraRank, AttackRank = AttackRank };
                s.Bottom.AddRange(Bottom);
                foreach (var kv in Hand) s.Hand[kv.Key] = kv.Value;
                foreach (var kv in Orig) s.Orig[kv.Key] = kv.Value;
                foreach (Mon m in Mons) s.Mons.Add(m.Clone());
                s.Spells.AddRange(Spells);
                s.Inactive.AddRange(Inactive);
                foreach (var kv in Grave) s.Grave[kv.Key] = kv.Value;
                foreach (var kv in Deck) s.Deck[kv.Key] = kv.Value;
                foreach (var kv in Extra) s.Extra[kv.Key] = kv.Value;
                foreach (string u in Used) s.Used.Add(u);
                foreach (int i in NoNormalSummon) s.NoNormalSummon.Add(i);
                return s;
            }

            public string Key()
            {
                StringBuilder sb = new StringBuilder();
                foreach (var kv in Hand.Where(x => x.Value > 0).OrderBy(x => x.Key)) sb.Append(kv.Key).Append('x').Append(kv.Value).Append(',');
                sb.Append('|');
                foreach (Mon m in Mons.OrderBy(x => x.Id).ThenBy(x => x.MatSig(), StringComparer.Ordinal)) sb.Append(m.Id).Append(':').Append(m.MatSig()).Append(m.Negated ? "n" : "").Append(',');
                sb.Append('|');
                foreach (int i in Spells.OrderBy(x => x)) sb.Append(i).Append(',');
                foreach (int i in Inactive.OrderBy(x => x)) sb.Append('s').Append(i).Append(',');
                // Which cards, not how many: GY targets (Detonator ②, Node ②, Plugin, GY payoffs), what searches still find and the Extra Deck left all change the rest of the turn.
                sb.Append('|');
                foreach (var kv in Grave.Where(x => x.Value > 0).OrderBy(x => x.Key)) sb.Append(kv.Key).Append('x').Append(kv.Value).Append(',');
                sb.Append("|d");
                foreach (var kv in Deck.OrderBy(x => x.Key)) sb.Append(kv.Value).Append(',');
                sb.Append("|e");
                foreach (var kv in Extra.OrderBy(x => x.Key)) sb.Append(kv.Value).Append(',');
                sb.Append('|').Append(Bonus.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|');
                foreach (string u in Used.OrderBy(x => x, StringComparer.Ordinal)) sb.Append(u).Append(',');
                sb.Append('|').Append(NS ? 1 : 0).Append('|').Append(HandSpent).Append('|').Append(Draws).Append('|').Append(ExtraRank).Append('/').Append(AttackRank);
                foreach (int i in Bottom.OrderBy(x => x)) sb.Append('b').Append(i);
                foreach (int i in NoNormalSummon.OrderBy(x => x)) sb.Append('!').Append(i);
                return sb.ToString();
            }

            public void Add(Dictionary<int, int> d, int id, int n)
            {
                int v;
                d.TryGetValue(id, out v);
                d[id] = Math.Max(0, v + n);
            }

            public int Count(Dictionary<int, int> d, int id)
            {
                int v;
                return d.TryGetValue(id, out v) ? v : 0;
            }

            public void TakeFromHand(int id)
            {
                Add(Hand, id, -1);
                if (Count(Orig, id) > 0) { Add(Orig, id, -1); HandSpent++; }
            }

            public int GraveMonsters(Func<int, bool> isMonster)
            {
                return Grave.Where(kv => kv.Value > 0 && isMonster(kv.Key)).Sum(kv => kv.Value);
            }

            public int Materials { get { return Mons.Sum(m => m.Mats); } }
            public int Left(int id) { return Count(Deck, id); }
        }

        /// <summary>Most cards an "up to N" removal can hit: the opponent's face-up cards, at least int.expected_targets.</summary>
        private int _targetCap = 2;
        /// <summary>Opponent's face-up cards right now (-1: unknown, e.g. offline tests).</summary>
        private int _oppFaceUp = -1;
        private bool _canAttack;
        private int _ownLp = 8000, _oppLp = 8000, _oppGraveRanks;
        private List<OpponentMonster> _oppMons = new List<OpponentMonster>();

        private St FromSnapshot(ZoneSnapshot z, GameCardDb db)
        {
            _targetCap = Math.Max(z.OpponentFaceUp, (int)_w.BaseOf("int.expected_targets"));
            _oppFaceUp = z.OpponentFaceUp;
            _canAttack = z.CanAttack;
            _ownLp = z.OwnLp;
            _oppLp = z.OpponentLp;
            _oppGraveRanks = z.OpponentGraveXyzRanks;
            _oppMons = z.OpponentMonsters.Select(x => x.Clone()).ToList();
            St s = new St { NS = z.NormalSummonAvailable };
            foreach (int id in z.NoNormalSummon) s.NoNormalSummon.Add(id);
            foreach (int id in z.Hand) { s.Add(s.Hand, id, 1); s.Add(s.Orig, id, 1); }
            for (int i = 0; i < z.Monsters.Count; ++i)
            {
                // Known material ids where the snapshot has them, unknown (0) for the rest of the count.
                List<int> known = i < z.MonsterMaterials.Count && z.MonsterMaterials[i] != null ? z.MonsterMaterials[i] : new List<int>();
                Mon m = new Mon { Id = z.Monsters[i].Key };
                m.MatIds.AddRange(known.Take(z.Monsters[i].Value));
                while (m.MatIds.Count < z.Monsters[i].Value) m.MatIds.Add(0);
                s.Mons.Add(m);
            }
            s.Spells.AddRange(z.Spells);
            s.Inactive.AddRange(z.SetSpells);
            foreach (int id in z.Grave) s.Add(s.Grave, id, 1);
            // Deck and Extra Deck contents are hidden: deck list minus every copy we can see.
            List<int> seen = new List<int>(z.Hand);
            seen.AddRange(z.Monsters.Select(m => m.Key));
            seen.AddRange(z.Spells);
            seen.AddRange(z.SetSpells);
            seen.AddRange(z.Grave);
            seen.AddRange(z.Banished);
            foreach (int id in db.MainDeck.Distinct()) s.Deck[id] = Math.Max(0, db.MainDeck.Count(x => x == id) - seen.Count(x => x == id));
            foreach (int id in db.ExtraDeck.Distinct()) s.Extra[id] = Math.Max(0, db.ExtraDeck.Count(x => x == id) - seen.Count(x => x == id));
            foreach (CardEntry c in db.Cards.Values)
                if (!c.Opponent) // our own usage only
                foreach (EffectEntry e in c.Effects)
                {
                    KeyUsage u = db.PeekUsage(0, e);
                    if (u != null && e.Limit != null && u.Used >= e.Limit.Count) s.Used.Add(e.UsageKey);
                    if (u != null && u.Resolved > 0) ApplyLocks(s, e); // e.g. a Ryzeal was rule summoned earlier this turn
                }
            return s;
        }

        // ---------------------------------------------------------- public API

        /// <summary>Best plan from this state, and the best plan for each possible first move.</summary>
        public PlanResult Plan(ZoneSnapshot z, out List<PlanResult> byFirstMove, out PlanResult stop)
        {
            _memo = new MemoTable();
            _next.Clear();
            _drawWorth.Clear();
            St s = FromSnapshot(z, _db);
            stop = StopResult(s);
            byFirstMove = new List<PlanResult>();
            PlanResult best = stop;
            var firstMoves = Moves(s, 0).ToList();
            int hintAt = HintStart(firstMoves);
            HintStartedAt = hintAt;
            if (Threads > 1 && firstMoves.Count > 0)
            {
                PlanParallel(z, firstMoves, hintAt, byFirstMove);
                foreach (PlanResult r in byFirstMove) if (r.Value > best.Value) best = r;
                return best;
            }
            System.Diagnostics.Stopwatch total = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < firstMoves.Count; ++i)
            {
                var mv = firstMoves[i];
                // Every first move gets its own budget: otherwise the first one searched uses it all
                // and the others are cut after a step or two (and look worse than they are).
                // Fully searched sub-results are shared through the memo.
                _budget = BudgetPerFirstMove;
                _clock.Restart();
                _moveDeadline = Math.Max(50, (MillisPerPlan - total.ElapsedMilliseconds) / (firstMoves.Count - i));
                PlanResult rest = Search(mv.Value, 1, HintNext(hintAt, mv.Key));
                PlanResult r = Combine(mv.Key, rest, 0);
                byFirstMove.Add(r);
                if (r.Value > best.Value) best = r;
            }
            return best;
        }

        /// <summary>
        /// The first two moves are expanded here; each (first, second) pair is one unit, searched by a worker planner
        /// on its own thread, Threads at a time, all sharing one memo table. A unit gets the time left over the
        /// waves of units still to start (as the one-thread loop does per first move). A first move's plan is the
        /// best of stopping after it and its units.
        /// </summary>
        private void PlanParallel(ZoneSnapshot z, List<KeyValuePair<List<PlanMove>, St>> firstMoves, int hintAt, List<PlanResult> byFirstMove)
        {
            var units = new List<Tuple<int, KeyValuePair<List<PlanMove>, St>, int>>(); // first move, second move, hint position after it
            var afterFirst = new List<PlanResult>();
            for (int i = 0; i < firstMoves.Count; ++i)
            {
                St s1 = firstMoves[i].Value;
                afterFirst.Add(StopResult(s1));
                int h1 = HintNext(hintAt, firstMoves[i].Key);
                if (MaxDepth <= 1) continue;
                var second = Moves(s1, 1).ToList();
                if (h1 >= 0)
                {
                    int h = HintIndex(second, h1);
                    if (h > 0) { var f = second[h]; second.RemoveAt(h); second.Insert(0, f); }
                }
                foreach (var mv in second) units.Add(Tuple.Create(i, mv, HintNext(h1, mv.Key)));
            }
            // Hinted units first, so they start with the most time.
            units = units.OrderByDescending(u => u.Item3 >= 0).ToList();

            PlanResult[] got = new PlanResult[units.Count];
            object gate = new object();
            Func<int, double> next = NextTurnValue;
            Func<int, double> nextLocked = null;
            if (next != null) nextLocked = id => { lock (gate) return next(id); };
            MemoTable memo = _memo;
            System.Diagnostics.Stopwatch total = System.Diagnostics.Stopwatch.StartNew();
            int started = 0;
            Exception failure = null;
            var workers = new System.Threading.ThreadLocal<TurnPlanner>(() =>
            {
                TurnPlanner w = new TurnPlanner(_db, _w);
                w.BudgetPerFirstMove = BudgetPerFirstMove;
                w.MaxDepth = MaxDepth;
                w.Threads = 1;
                w.MemoCap = MemoCap;
                w.Hint = Hint;
                w._hint = Hint;
                w.NextTurnValue = nextLocked;
                w._memo = memo;
                w.FromSnapshot(z, _db); // the opponent's board, LP and targets for this worker
                return w;
            });
            System.Threading.Tasks.Parallel.For(0, units.Count, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Threads }, u =>
            {
                try
                {
                    int order = System.Threading.Interlocked.Increment(ref started) - 1;
                    int wavesLeft = (units.Count - order + Threads - 1) / Threads;
                    long millis = Math.Max(50, (MillisPerPlan - total.ElapsedMilliseconds) / Math.Max(1, wavesLeft));
                    TurnPlanner w = workers.Value;
                    w._budget = BudgetPerFirstMove;
                    w._clock.Restart();
                    w._moveDeadline = millis;
                    var unit = units[u];
                    got[u] = w.Combine(unit.Item2.Key, w.Search(unit.Item2.Value, 2, unit.Item3), 1);
                }
                catch (Exception ex) { lock (gate) failure = ex; }
            });
            workers.Dispose();
            if (failure != null) throw new InvalidOperationException("parallel plan failed", failure);

            for (int i = 0; i < firstMoves.Count; ++i)
            {
                PlanResult rest = afterFirst[i];
                bool truncated = false;
                for (int u = 0; u < units.Count; ++u)
                {
                    if (units[u].Item1 != i || got[u] == null) continue;
                    truncated |= got[u].Truncated;
                    if (got[u].Value > rest.Value) rest = got[u];
                }
                PlanResult r = Combine(firstMoves[i].Key, rest, 0);
                r.Truncated = truncated;
                byFirstMove.Add(r);
            }
        }

        // ---------------------------------------------------------- plan hint (principal line first)

        /// <summary>Debug: the value of walking only the hinted line from this state, and how many hint steps it took.</summary>
        public PlanResult WalkHint(ZoneSnapshot z, out int steps)
        {
            _memo = new MemoTable();
            St s = FromSnapshot(z, _db);
            var firsts = Moves(s, 0).ToList();
            int at = HintStart(firsts);
            steps = 0;
            var path = new List<KeyValuePair<List<PlanMove>, int>>();
            var moves = firsts;
            int k = 0;
            while (at >= 0 && at < _hint.Count && k < MaxDepth)
            {
                int h = HintIndex(moves, at);
                if (h < 0) break;
                path.Add(new KeyValuePair<List<PlanMove>, int>(moves[h].Key, k));
                s = moves[h].Value;
                at += moves[h].Key.Count;
                steps = at;
                ++k;
                moves = Moves(s, k).ToList();
            }
            PlanResult r = StopResult(s);
            for (int i = path.Count - 1; i >= 0; --i) r = Combine(path[i].Key, r, path[i].Value);
            return r;
        }

        /// <summary>Step identity for the hint: the label carries materials, costs, detaches and targets, which tell variants of one card's move apart.</summary>
        private static string StepKey(PlanMove m) { return m.Kind + "|" + m.CardId + "|" + m.EffectIndex + "|" + m.TargetId + "|" + m.Text; }

        /// <summary>Where the last Plan picked up the hint (-1: not at all) and how long the hint was.</summary>
        public int HintStartedAt { get; private set; }
        public int HintLength { get { return Hint != null ? Hint.Count : 0; } }

        /// <summary>Hint position the first moves continue from: the earliest step some first move starts with (the steps before it are done).</summary>
        private int HintStart(List<KeyValuePair<List<PlanMove>, St>> firstMoves)
        {
            _hint = Hint;
            if (_hint == null || _hint.Count == 0) return -1;
            for (int p = 0; p < _hint.Count; ++p)
                foreach (var mv in firstMoves)
                    if (HintMatches(p, mv.Key)) return p;
            return -1;
        }

        private bool HintMatches(int at, List<PlanMove> steps)
        {
            if (at < 0 || _hint == null || at + steps.Count > _hint.Count) return false;
            for (int i = 0; i < steps.Count; ++i)
                if (StepKey(_hint[at + i]) != StepKey(steps[i])) return false;
            return true;
        }

        /// <summary>
        /// The move that follows the hint from position <paramref name="at"/>: the longest match, since a summon comes
        /// both alone and with its trigger, and taking it alone leaves the hinted trigger behind. -1: none.
        /// </summary>
        private int HintIndex(List<KeyValuePair<List<PlanMove>, St>> moves, int at)
        {
            int best = -1;
            for (int i = 0; i < moves.Count; ++i)
                if (HintMatches(at, moves[i].Key) && (best < 0 || moves[i].Key.Count > moves[best].Key.Count)) best = i;
            return best;
        }

        /// <summary>Hint position after taking these steps, or -1 when they leave the hinted line.</summary>
        private int HintNext(int at, List<PlanMove> steps)
        {
            return HintMatches(at, steps) ? at + steps.Count : -1;
        }

        public bool Exhausted { get { return _budget <= 0; } }

        /// <summary>Worth of each of our cards right now (unused functions + interruption value), for picking what to take or give up.</summary>
        public double Worth(ZoneSnapshot z, int cardId)
        {
            CardEntry c = _db.Get(cardId);
            if (c == null) return 0;
            if (_w.Excluded.Contains(cardId)) return -1;
            return StaticValue(FromSnapshot(z, _db), c);
        }

        // ---------------------------------------------------------- search

        private PlanResult Search(St s, int k, int hintAt = -1)
        {
            string key = k + "#" + s.Key();
            PlanResult cached;
            if (_memo.D.TryGetValue(key, out cached)) return cached;
            PlanResult best = StopResult(s);
            bool truncated = false;
            if (k < MaxDepth)
            {
                bool inTime = --_budget > 0 && _clock.ElapsedMilliseconds < _moveDeadline;
                // Out of time, the hinted step is still taken (only it): the line found before is always read to its end.
                if (inTime || (hintAt >= 0 && hintAt < (_hint != null ? _hint.Count : 0)))
                {
                    var moves = Moves(s, k).ToList();
                    if (hintAt >= 0)
                    {
                        // The hinted step first, while the budget is fresh.
                        int h = HintIndex(moves, hintAt);
                        if (h > 0) { var first = moves[h]; moves.RemoveAt(h); moves.Insert(0, first); }
                        if (h < 0) hintAt = -1;
                        if (!inTime)
                        {
                            truncated = moves.Count > (h >= 0 ? 1 : 0);
                            moves = h >= 0 ? moves.GetRange(0, 1) : new List<KeyValuePair<List<PlanMove>, St>>();
                        }
                    }
                    foreach (var mv in moves)
                    {
                        PlanResult sub = Search(mv.Value, k + 1, HintNext(hintAt, mv.Key));
                        truncated |= sub.Truncated;
                        PlanResult r = Combine(mv.Key, sub, k);
                        if (r.Value > best.Value) best = r;
                    }
                }
                else truncated = Moves(s, k).Any();
            }
            best.Truncated = truncated;
            if (!truncated && _memo.Count < MemoCap && _memo.D.TryAdd(key, best)) System.Threading.Interlocked.Increment(ref _memo.Count);
            return best;
        }

        private PlanResult Combine(List<PlanMove> moves, PlanResult rest, int k)
        {
            PlanResult r = new PlanResult();
            double gain = moves.Sum(m => m.Gain) * Math.Pow(_w.Gamma, k);
            r.Moves.AddRange(moves);
            r.Moves.AddRange(rest.Moves);
            r.FlowValue = gain + rest.FlowValue;
            r.EndValue = rest.EndValue;
            r.FollowValue = rest.FollowValue;
            r.BattleValue = rest.BattleValue;
            r.HandSpent = rest.HandSpent;
            r.Truncated = rest.Truncated;
            r.EndBoard.AddRange(rest.EndBoard);
            r.Battle.AddRange(rest.Battle);
            r.Value = r.FlowValue + r.EndValue + r.FollowValue + r.BattleValue - _w.EfficiencyPerHandCard * r.HandSpent;
            return r;
        }

        private PlanResult StopResult(St s)
        {
            PlanResult r = new PlanResult();
            r.EndValue = EndOfTurn(s, r.EndBoard);
            r.FollowValue = FollowTerm(s) + s.Bonus + s.Draws * DrawExpect(s);
            r.BattleValue = BattleTerm(s, r.Battle);
            r.HandSpent = s.HandSpent;
            r.Value = r.EndValue + r.FollowValue + r.BattleValue - _w.EfficiencyPerHandCard * s.HandSpent;
            return r;
        }

        /// <summary>All moves from a state: each is the list of steps it consists of (main step + triggers) and the resulting state.</summary>
        private IEnumerable<KeyValuePair<List<PlanMove>, St>> Moves(St s, int k)
        {
            List<KeyValuePair<List<PlanMove>, St>> result = new List<KeyValuePair<List<PlanMove>, St>>();
            foreach (int id in s.Hand.Where(x => x.Value > 0).Select(x => x.Key).ToList())
            {
                CardEntry c = _db.Get(id);
                if (c == null || _w.Excluded.Contains(id)) continue;
                bool monster = (c.Data.Type & 0x1) != 0;
                if (monster)
                {
                    // Normal summon
                    if (s.NS && !s.NoNormalSummon.Contains(id) && c.Data.Level <= 4 && !IsExtraType(c))
                    {
                        St n = s.Clone();
                        n.NS = false;
                        n.TakeFromHand(id);
                        Mon m = new Mon { Id = id };
                        n.Mons.Add(m);
                        AddArrival(result, n, m, "SUMMON_SUCCESS", new PlanMove { Kind = "ns", CardId = id, Text = Name(c) + " 일반 소환" });
                    }
                    // Rule summon from hand
                    EffectEntry proc = c.Effects.FirstOrDefault(e => e.Kind.StartsWith("룰") && (e.Range == null || e.Range.Contains("HAND")));
                    if (proc != null && !s.Used.Contains(proc.UsageKey) && Conditions(proc, s) && proc.DetachCount <= s.Materials)
                    {
                        St n = s.Clone();
                        n.Used.Add(proc.UsageKey);
                        ApplyLocks(n, proc);
                        n.TakeFromHand(id);
                        double cost;
                        List<int> paid;
                        if (!PayCosts(n, c, proc, out cost, out paid)) continue;
                        double fired;
                        List<int> off = Detach(n, null, proc.DetachCount, out fired);
                        Mon m = new Mon { Id = id };
                        n.Mons.Add(m);
                        double gain = _w.BaseOf("rule_special_summon") + cost + fired + proc.DetachCount * _w.BaseOf("cost_detach") + _w.OffsetOf(id, proc.Index);
                        PlanMove rm = new PlanMove { Kind = "rule", CardId = id, EffectIndex = proc.Index, Text = Name(c) + " 룰 특수 소환" + CostText(paid) + DetachText(off), Gain = gain };
                        rm.CostIds.AddRange(paid);
                        AddArrival(result, n, m, "SPSUMMON_SUCCESS", rm);
                    }
                    // Hand ignition effects that Normal Summon a monster from the hand without using the turn's Normal Summon
                    foreach (EffectEntry e in c.Effects.Where(x => x.Kind.StartsWith("기동") && x.Range != null && x.Range.Contains("HAND") && x.Categories.Contains("SUMMON")))
                    {
                        if (s.Used.Contains(e.UsageKey) || !Conditions(e, s)) continue;
                        CardLink pick = e.Links.FirstOrDefault(l => l.Role == "effect" && !l.Opponent && l.Location.Contains("HAND") && l.Targets.Count > 0);
                        if (pick == null) continue;
                        foreach (int t in pick.Targets.Distinct())
                        {
                            CardEntry tc = _db.Get(t);
                            if (tc == null || s.Count(s.Hand, t) <= 0 || s.NoNormalSummon.Contains(t) || tc.Data.Level > 4 || _w.Excluded.Contains(t)) continue;
                            St n = s.Clone();
                            n.Used.Add(e.UsageKey);
                            ApplyLocks(n, e);
                            n.TakeFromHand(t);
                            Mon m = new Mon { Id = t };
                            n.Mons.Add(m);
                            double gain = _w.BaseOf("extra_normal_summon") + _w.OffsetOf(id, e.Index);
                            AddArrival(result, n, m, "SUMMON_SUCCESS", new PlanMove { Kind = "hand", CardId = id, EffectIndex = e.Index, TargetId = t, Text = Name(c) + " 기동(" + e.Index + ") → " + Name(tc) + " 추가 일반 소환", Gain = gain });
                        }
                    }
                }
                else if ((c.Data.Type & 0x2) != 0) // spells: activate from hand (field spells are just placed)
                {
                    if ((c.Data.Type & 0x80000) != 0)
                    {
                        if (!s.Spells.Contains(id))
                        {
                            St n = s.Clone();
                            n.TakeFromHand(id);
                            n.Spells.Add(id);
                            result.Add(new KeyValuePair<List<PlanMove>, St>(new List<PlanMove> { new PlanMove { Kind = "spell", CardId = id, Text = Name(c) + " 필드 마법 발동" } }, n));
                        }
                        continue;
                    }
                    foreach (EffectEntry e in c.Effects.Where(x => x.Kind.StartsWith("카드의 발동")))
                        foreach (var r in Resolve(s, c, e, null, "spell", true))
                            result.Add(r);
                }
            }
            // Activate a face-down spell (e.g. a field spell set by an effect): it only works face-up
            foreach (int id in s.Inactive.Distinct().ToList())
            {
                CardEntry c = _db.Get(id);
                if (c == null || _w.Excluded.Contains(id)) continue;
                EffectEntry act = c.Effects.FirstOrDefault(x => x.Kind.StartsWith("카드의 발동"));
                if (act == null || s.Used.Contains(act.UsageKey) || !Conditions(act, s)) continue;
                St b = s.Clone();
                b.Inactive.Remove(id);
                b.Spells.Add(id);
                bool hasFunction = act.Links.Any(l => l.Role == "effect" && ValueModel.FunctionOf(l) != null);
                if (!hasFunction)
                {
                    result.Add(new KeyValuePair<List<PlanMove>, St>(new List<PlanMove> { new PlanMove { Kind = "setspell", CardId = id, EffectIndex = act.Index, Text = Name(c) + " 세트 카드 발동", Gain = -0.5 /* tie-break: activate once nothing else is left, since face-up spells may restrict summons */ } }, b));
                    continue;
                }
                foreach (var r in Resolve(b, c, act, null, "setspell", false))
                    result.Add(r);
            }
            // Field ignition effects
            for (int i = 0; i < s.Mons.Count; ++i)
            {
                Mon m = s.Mons[i];
                CardEntry c = _db.Get(m.Id);
                if (c == null || m.Negated || _w.Excluded.Contains(m.Id)) continue;
                foreach (EffectEntry e in c.Effects.Where(x => x.Kind.StartsWith("기동") && (x.Range == null || x.Range.Contains("MZONE"))))
                    foreach (var r in Resolve(s, c, e, i, "ignition", false))
                        result.Add(r);
            }
            // Ignition effects of face-up spells in their zone (e.g. Ryzeal Cross ②: 2 Ryzeal from the GY to the deck, draw 1)
            foreach (int id in s.Spells.Distinct().ToList())
            {
                CardEntry c = _db.Get(id);
                if (c == null || (c.Data.Type & 0x2) == 0 || _w.Excluded.Contains(id)) continue;
                foreach (EffectEntry e in c.Effects.Where(x => x.Kind.StartsWith("기동") && x.Range != null && (x.Range.Contains("FZONE") || x.Range.Contains("SZONE"))))
                    foreach (var r in Resolve(s, c, e, null, "zone", false))
                        result.Add(r);
            }
            // Xyz summon with two level-N monsters we control
            foreach (int xid in s.Extra.Where(x => x.Value > 0).Select(x => x.Key).ToList())
            {
                CardEntry x = _db.Get(xid);
                if (x == null || x.Material == null || _w.Excluded.Contains(xid) || !x.Material.Relation.StartsWith("엑시즈") || x.XyzOfXyz) continue;
                if (SameNameBlocked(s, xid) || !ExtraAllowed(s, xid)) continue;
                int level = MaterialLevel(x);
                HashSet<int> ok = new HashSet<int>(x.Material.Targets);
                List<int> mats = Enumerable.Range(0, s.Mons.Count)
                    .Where(i => ok.Contains(s.Mons[i].Id) && !IsExtraType(_db.Get(s.Mons[i].Id)) && _db.Get(s.Mons[i].Id).Data.Level == level)
                    .OrderBy(i => UnusedEffects(s, s.Mons[i].Id)).Take(3).ToList();
                int need = x.Material.Relation.Contains("× 3") ? 3 : 2;
                if (mats.Count < need) continue;
                St n = s.Clone();
                Mon xm = new Mon { Id = xid };
                foreach (int i in mats.Take(need)) xm.MatIds.AddRange(s.Mons[i].AsMaterial());
                foreach (int i in mats.Take(need).OrderByDescending(i => i)) n.Mons.RemoveAt(i); // they become its materials
                n.Add(n.Extra, xid, -1);
                n.Mons.Add(xm);
                AddArrival(result, n, xm, "SPSUMMON_SUCCESS", new PlanMove { Kind = "xyz", CardId = xid, Text = Name(x) + " 엑시즈 소환" + MatText(xm) });
            }
            OverlaySummons(s, result);
            return result;
        }

        private int Rank(int id) { CardEntry c = _db.Get(id); return c == null ? -1 : c.Data.Level; }

        /// <summary>The turn locks an effect leaves on us (a second, different lock leaves nothing allowed).</summary>
        private static void ApplyLocks(St s, EffectEntry e)
        {
            if (e.ExtraLockRank != -1)
            {
                const int any = GameCardDb.AnyXyzRank;
                int l = e.ExtraLockRank;
                s.ExtraRank = s.ExtraRank == -1 || s.ExtraRank == any ? (s.ExtraRank == any && l == any ? any : l)
                            : l == any || l == s.ExtraRank ? s.ExtraRank : -2;
            }
            if (e.AttackLockRank != -1) s.AttackRank = s.AttackRank == -1 || s.AttackRank == e.AttackLockRank ? e.AttackLockRank : -2;
        }

        /// <summary>The Extra Deck lock allows this monster (e.g. after a Ryzeal rule summon only Rank 4 Xyz; F0 is Rank 1 by rule).</summary>
        private bool ExtraAllowed(St s, int id)
        {
            return s.ExtraRank == -1 || (s.ExtraRank >= 0 && IsXyz(id) && (s.ExtraRank == GameCardDb.AnyXyzRank || Rank(id) == s.ExtraRank));
        }

        /// <summary>
        /// Xyz summons that use Xyz monsters we control: on top of one (Ray Lancer: Rank 4 or lower, discarding a Spell/Trap,
        /// once per turn) or from two with the same Rank (F0). The material monsters and their own materials all become
        /// the new monster's materials.
        /// </summary>
        private void OverlaySummons(St s, List<KeyValuePair<List<PlanMove>, St>> result)
        {
            foreach (int xid in s.Extra.Where(x => x.Value > 0).Select(x => x.Key).ToList())
            {
                CardEntry x = _db.Get(xid);
                if (x == null || _w.Excluded.Contains(xid) || SameNameBlocked(s, xid) || !ExtraAllowed(s, xid)) continue;
                if (x.Overlay != null)
                {
                    OverlayProc o = x.Overlay;
                    string key = "overlay:" + xid;
                    if (o.OncePerTurn && s.Used.Contains(key)) goto xyzOfXyz;
                    int discard = 0;
                    if (o.DiscardFilter != null)
                    {
                        discard = s.Hand.Where(h => h.Value > 0 && o.DiscardTargets.Contains(h.Key))
                                        .OrderBy(h => StaticValue(s, _db.Get(h.Key))).Select(h => h.Key).FirstOrDefault();
                        if (discard == 0) goto xyzOfXyz;
                    }
                    for (int i = 0; i < s.Mons.Count; ++i)
                    {
                        Mon m = s.Mons[i];
                        int r = Rank(m.Id);
                        if (!IsXyz(m.Id) || m.Id == xid || r > o.RankMax || (o.RankExact >= 0 && r != o.RankExact)) continue;
                        if (s.Mons.Take(i).Any(y => y.Id == m.Id && y.MatSig() == m.MatSig())) continue; // same monster twice: one branch
                        St n = s.Clone();
                        n.Mons.RemoveAt(i);
                        n.Add(n.Extra, xid, -1);
                        if (o.OncePerTurn) n.Used.Add(key);
                        if (discard != 0) { n.TakeFromHand(discard); n.Add(n.Grave, discard, 1); }
                        Mon xm = new Mon { Id = xid };
                        xm.MatIds.AddRange(m.AsMaterial());
                        n.Mons.Add(xm);
                        PlanMove mv = new PlanMove { Kind = "xyz", CardId = xid, Text = Name(x) + " 엑시즈 소환 (" + Name(_db.Get(m.Id)) + " 위" + (discard != 0 ? ", 버림: " + Name(_db.Get(discard)) : "") + ")" };
                        mv.CostIds.Add(m.Id);
                        if (discard != 0) mv.CostIds.Add(discard);
                        AddArrival(result, n, xm, "SPSUMMON_SUCCESS", mv);
                    }
                }
            xyzOfXyz:
                if (!x.XyzOfXyz || x.Material == null) continue;
                int need = Math.Max(2, Regex.Match(x.Material.Relation, @"× (\d+)").Success ? int.Parse(Regex.Match(x.Material.Relation, @"× (\d+)").Groups[1].Value) : 2);
                List<int> cands = Enumerable.Range(0, s.Mons.Count).Where(i => IsXyz(s.Mons[i].Id) && s.Mons[i].Id != xid && x.Material.Targets.Contains(s.Mons[i].Id)).ToList();
                foreach (var group in cands.GroupBy(i => x.SameRankMaterials ? Rank(s.Mons[i].Id) : 0))
                {
                    List<int> g = group.ToList();
                    if (g.Count < need) continue;
                    HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (List<int> pick in Combinations(g, need))
                    {
                        string sig = String.Join(",", pick.Select(i => s.Mons[i].Id + ":" + s.Mons[i].MatSig()).OrderBy(t => t, StringComparer.Ordinal).ToArray());
                        if (!seen.Add(sig)) continue;
                        St n = s.Clone();
                        Mon xm = new Mon { Id = xid };
                        foreach (int i in pick) xm.MatIds.AddRange(s.Mons[i].AsMaterial());
                        foreach (int i in pick.OrderByDescending(i => i)) n.Mons.RemoveAt(i);
                        n.Add(n.Extra, xid, -1);
                        n.Mons.Add(xm);
                        PlanMove mv = new PlanMove { Kind = "xyz", CardId = xid, Text = Name(x) + " 엑시즈 소환 (" + String.Join(" + ", pick.Select(i => Name(_db.Get(s.Mons[i].Id))).ToArray()) + ")" };
                        mv.CostIds.AddRange(pick.Select(i => s.Mons[i].Id));
                        AddArrival(result, n, xm, "SPSUMMON_SUCCESS", mv);
                    }
                }
            }
        }

        private static IEnumerable<List<int>> Combinations(List<int> items, int k, int start = 0)
        {
            if (k == 0) { yield return new List<int>(); yield break; }
            for (int i = start; i <= items.Count - k; ++i)
                foreach (List<int> rest in Combinations(items, k - 1, i + 1))
                {
                    rest.Insert(0, items[i]);
                    yield return rest;
                }
        }

        // ---------------------------------------------------------- battle (breakthrough)

        /// <summary>
        /// The Battle Phase after this end board, greedily: strongest attacker first, each at its best target (or directly when
        /// the opponent has no monster left), only attacks that gain. Destroying a monster is worth battle.destroy plus the
        /// interruptions it held; damage battle.lp per 8000; total damage reaching their LP is battle.lethal.
        /// ATK changes, chain attacks and battle-start negation come from the Lua (CardEntry.Battle).
        /// </summary>
        private double BattleTerm(St s, List<string> notes)
        {
            if (!_canAttack || s.Mons.Count == 0) return 0;
            double destroy = _w.BaseOf("battle.destroy"), lpValue = _w.BaseOf("battle.lp");
            int rankSum = s.Mons.Where(m => IsXyz(m.Id)).Sum(m => Math.Max(0, Rank(m.Id)));
            int oppAtk = s.Mons.Where(m => !m.Negated).Sum(m => { CardEntry c = _db.Get(m.Id); return c == null ? 0 : c.Battle.OpponentAtk; });
            string negate = s.Mons.Where(m => !m.Negated).Select(m => _db.Get(m.Id)).Where(c => c != null && c.Battle.BattleStartNegate != null)
                                  .Select(c => c.Battle.BattleStartNegate).OrderBy(t => t == "all" ? 0 : 1).FirstOrDefault();
            double value = 0;
            List<OpponentMonster> foes = new List<OpponentMonster>();
            foreach (OpponentMonster o in _oppMons)
            {
                OpponentMonster f = o.Clone();
                if (f.FaceUp) f.Atk = Math.Max(0, f.Atk + oppAtk);
                if (negate != null && f.FaceUp && (negate == "all" || f.AttackPosition))
                {
                    // Negated for the rest of the turn: no protection of its own, no interruptions while we keep playing.
                    f.Indestructible = false;
                    f.ReplaceCost = -1;
                    value += f.Held * _w.BaseOf("battle.negate_held");
                    f.Held = 0;
                    notes.Add(Name(_db.Get(f.Id)) + " 무효");
                }
                foes.Add(f);
            }
            if (oppAtk != 0 && foes.Count > 0) notes.Add("상대 공격력 " + oppAtk);

            if (s.AttackRank != -1) notes.Add("공격 제약: " + (s.AttackRank >= 0 ? "랭크 " + s.AttackRank + " 엑시즈만" : "공격 불가"));
            var attackers = s.Mons.Select(m => new { M = m, C = _db.Get(m.Id) }).Where(a => a.C != null && a.C.Data.Atk >= 0)
                .Where(a => s.AttackRank == -1 || (s.AttackRank >= 0 && IsXyz(a.M.Id) && Rank(a.M.Id) == s.AttackRank))
                .Select(a => new { a.M, a.C, Atk = a.C.Data.Atk + (a.M.Negated ? 0 : a.C.Battle.AtkPerXyzRank * (rankSum + (a.C.Battle.RanksInOpponentGrave ? _oppGraveRanks : 0))) })
                .OrderByDescending(a => a.Atk).ToList();
            int damage = 0;
            foreach (var a in attackers)
            {
                int extra = a.C.Battle.ChainAttackDetach >= 0 && !a.M.Negated ? (a.C.Battle.ChainAttackDetach == 0 ? 1 : a.M.Mats / a.C.Battle.ChainAttackDetach) : 0;
                for (int attack = 0; attack <= extra; ++attack)
                {
                    List<OpponentMonster> alive = foes.Where(f => f != null).ToList();
                    if (alive.Count == 0)
                    {
                        if (attack > 0) break; // a chain attack needs a destroyed monster before it
                        damage += a.Atk;
                        value += a.Atk / 8000.0 * lpValue;
                        notes.Add(Name(a.C) + " 직접 " + a.Atk);
                        break;
                    }
                    OpponentMonster best = null;
                    double bestScore = 0;
                    int bestDamage = 0;
                    bool bestKills = false;
                    foreach (OpponentMonster f in alive)
                    {
                        int dmg = 0;
                        bool kills = false;
                        double score;
                        double worth = destroy + f.Held;
                        bool guarded = f.Indestructible || f.ReplaceCost >= 0;
                        if (!f.FaceUp && !f.AttackPosition) { kills = a.Atk >= 2000 && !guarded; score = kills ? worth * 0.5 : -1; }
                        else if (f.AttackPosition)
                        {
                            if (a.Atk > f.Atk) { dmg = a.Atk - f.Atk; kills = !guarded; score = (kills ? worth : f.ReplaceCost >= 0 ? f.ReplaceCost : 0) + dmg / 8000.0 * lpValue; }
                            else if (a.Atk == f.Atk) { kills = !guarded; score = (kills ? worth : 0) - destroy; }
                            else score = -1;
                        }
                        else
                        {
                            if (a.Atk > f.Def) { kills = !guarded; score = kills ? worth : f.ReplaceCost >= 0 ? f.ReplaceCost : 0; }
                            else score = -1;
                        }
                        if (score > bestScore) { bestScore = score; best = f; bestDamage = dmg; bestKills = kills; }
                    }
                    if (best == null) break;
                    value += bestScore;
                    damage += bestDamage;
                    notes.Add(Name(a.C) + " → " + Name(_db.Get(best.Id)) + (bestKills ? " 파괴" : " (보호)") + (bestDamage > 0 ? " " + bestDamage : ""));
                    if (bestKills) foes[foes.IndexOf(best)] = null;
                    else { best.ReplaceCost = -1; break; } // protection used: the next attack can destroy it, this attacker is done
                }
            }
            if (damage >= _oppLp)
            {
                notes.Add("LP " + _oppLp + " 이상: 승리");
                return _w.BaseOf("battle.lethal");
            }
            return value;
        }

        private int UnusedEffects(St s, int id)
        {
            CardEntry c = _db.Get(id);
            return c == null ? 0 : c.Effects.Count(e => !s.Used.Contains(e.UsageKey) && !e.Kind.StartsWith("적용"));
        }

        /// <summary>After a monster arrives: the move alone, or the move followed by one of its triggers.</summary>
        private void AddArrival(List<KeyValuePair<List<PlanMove>, St>> result, St n, Mon m, string ev, PlanMove main)
        {
            result.Add(new KeyValuePair<List<PlanMove>, St>(new List<PlanMove> { main }, n));
            CardEntry c = _db.Get(m.Id);
            if (c == null || m.Negated) return;
            int idx = n.Mons.IndexOf(m);
            foreach (EffectEntry e in c.Effects.Where(x => x.Kind.StartsWith("유발 효과") && x.Events.Contains(ev)))
                foreach (var r in Resolve(n, c, e, idx, "trigger", false))
                {
                    List<PlanMove> moves = new List<PlanMove> { main };
                    moves.AddRange(r.Key);
                    result.Add(new KeyValuePair<List<PlanMove>, St>(moves, r.Value));
                }
        }

        /// <summary>Resolve one effect: costs, the best picks of its main function (branching over targets), side functions.</summary>
        private IEnumerable<KeyValuePair<List<PlanMove>, St>> Resolve(St s, CardEntry c, EffectEntry e, int? sourceMon, string kind, bool fromHand)
        {
            List<KeyValuePair<List<PlanMove>, St>> outs = new List<KeyValuePair<List<PlanMove>, St>>();
            if (s.Used.Contains(e.UsageKey)) return outs;
            Mon src = sourceMon.HasValue && sourceMon.Value < s.Mons.Count ? s.Mons[sourceMon.Value] : null;
            if (e.DetachCount > (src != null && src.Mats >= e.DetachCount ? src.Mats : s.Materials)) return outs;
            if (!Conditions(e, s) || DeckBlocked(e, s)) return outs;

            St b = s.Clone();
            b.Used.Add(e.UsageKey);
            ApplyLocks(b, e);
            if (fromHand) b.TakeFromHand(c.Data.Id);
            double cost;
            List<int> paid;
            if (!PayCosts(b, c, e, out cost, out paid)) return outs;
            double fired;
            List<int> detached = Detach(b, src != null ? (int?)s.Mons.IndexOf(src) : null, e.DetachCount, out fired);

            double baseGain = cost + fired + e.DetachCount * _w.BaseOf("cost_detach") + _w.OffsetOf(c.Data.Id, e.Index);
            string label = Name(c) + " " + (kind == "trigger" ? "유발" : kind == "ignition" || kind == "zone" ? "기동" : "발동") + "(" + e.Index + ")" + CostText(paid) + DetachText(detached);

            // Side functions that do not bring a card.
            int attached = 0; // the GY card attached as material: the live pick follows it
            foreach (CardLink l in e.Links.Where(x => x.Role == "effect" && !x.Opponent))
            {
                string f = ValueModel.FunctionOf(l);
                if (f == "attach_from_grave" && b.GraveMonsters(IsMonsterId) > 0)
                {
                    Mon to = src != null && IsXyz(src.Id) ? b.Mons[s.Mons.IndexOf(src)] : b.Mons.FirstOrDefault(m => IsXyz(m.Id));
                    int gid = to != null ? AttachPick(b, to, l) : 0;
                    if (gid != 0) { b.Add(b.Grave, gid, -1); to.MatIds.Add(gid); attached = gid; baseGain += _w.BaseOf(f); label += " +소재: " + Name(_db.Get(gid)); }
                }
                else if (f == "attach_from_deck")
                {
                    Mon to = b.Mons.FirstOrDefault(m => IsXyz(m.Id) && _db.Get(m.Id).Data.Level == 4);
                    int did = l.Targets.FirstOrDefault(t => b.Count(b.Deck, t) > 0 && !_w.Excluded.Contains(t));
                    if (to != null && did != 0) { b.Add(b.Deck, did, -1); to.MatIds.Add(did); baseGain += _w.BaseOf(f); label += " +덱→소재: " + Name(_db.Get(did)); }
                }
                else if (f == "extra_to_grave")
                {
                    int xid = PickExtraFodder(b);
                    if (xid != 0) { b.Add(b.Extra, xid, -1); b.Add(b.Grave, xid, 1); baseGain += _w.BaseOf(f); }
                }
                else if (f == "grave_to_deck")
                {
                    if (NextTurnValue == null) { baseGain += _w.BaseOf(f); continue; }
                    List<int> back = PickReturns(b, l);
                    if (back == null) return outs; // not enough cards in the GY: cannot be activated
                    foreach (int id in back)
                    {
                        b.Bonus += ReturnValue(b, id);
                        b.Add(b.Grave, id, -1);
                        if (id == 0) continue;
                        if (IsExtraType(_db.Get(id))) b.Add(b.Extra, id, 1);
                        else { b.Add(b.Deck, id, 1); b.Bottom.Add(id); }
                    }
                    paid.AddRange(back.Where(x => x != 0));
                    label += " [덱으로: " + String.Join(", ", back.Select(x => x == 0 ? "소재" : Name(_db.Get(x))).ToArray()) + "]";
                }
            }
            if (e.Categories.Contains("DRAW"))
            {
                if (NextTurnValue != null) { b.Draws++; label += " +드로우"; }
                else baseGain += _w.BaseOf("draw");
            }

            // Main function: bring one card (branch over distinct targets).
            CardLink main = e.Links.Where(x => x.Role == "effect" && !x.Opponent && ValueModel.FunctionOf(x) != null && Arrival(x) != null)
                                   .OrderByDescending(x => _w.BaseOf(ValueModel.FunctionOf(x))).FirstOrDefault();
            if (main == null)
            {
                if (e.Categories.Contains("SEARCH")) baseGain += _w.BaseOf("search"); // unreadable pick (excavate etc.)
                PlanMove only = new PlanMove { Kind = kind, CardId = c.Data.Id, EffectIndex = e.Index, TargetId = attached, Text = label, Gain = baseGain };
                only.CostIds.AddRange(paid);
                outs.Add(new KeyValuePair<List<PlanMove>, St>(new List<PlanMove> { only }, b));
                return outs;
            }
            string fn = ValueModel.FunctionOf(main);
            double fBase = _w.BaseOf(fn);
            string arrival = Arrival(main);
            List<int> targets = main.Filter != null && !main.Filter.Partial && !main.AnyCard ? main.Targets.Distinct().ToList() : new List<int>();
            foreach (int t in targets)
            {
                if (_w.Excluded.Contains(t)) continue;
                bool fromDeck = main.Location.Contains("DECK"), fromGrave = main.Location.Contains("GRAVE");
                if (fromDeck && b.Count(b.Deck, t) <= 0) continue;
                if (!fromDeck && fromGrave && b.Count(b.Grave, t) <= 0) continue;
                if (!fromDeck && !fromGrave) continue;
                St n = b.Clone();
                if (fromDeck) n.Add(n.Deck, t, -1); else n.Add(n.Grave, t, -1);
                CardEntry tc = _db.Get(t);
                PlanMove mv = new PlanMove { Kind = kind, CardId = c.Data.Id, EffectIndex = e.Index, TargetId = t, Gain = baseGain + fBase, Text = label + " → " + Name(tc) };
                mv.CostIds.AddRange(paid);
                if (arrival == "HAND")
                {
                    n.Add(n.Hand, t, 1);
                    if (e.BlocksNormalSummonOfAdded) n.NoNormalSummon.Add(t);
                    outs.Add(new KeyValuePair<List<PlanMove>, St>(new List<PlanMove> { mv }, n));
                }
                else if (arrival == "SET")
                {
                    // Set traps and quick-plays wait for the opponent's turn; other spells must still be activated.
                    bool waits = tc != null && ((tc.Data.Type & 0x4) != 0 || (tc.Data.Type & 0x10000) != 0);
                    if (waits) n.Spells.Add(t); else n.Inactive.Add(t);
                    outs.Add(new KeyValuePair<List<PlanMove>, St>(new List<PlanMove> { mv }, n));
                }
                else
                {
                    Mon m = new Mon { Id = t, Negated = e.SummonsNegated };
                    n.Mons.Add(m);
                    List<KeyValuePair<List<PlanMove>, St>> arr = new List<KeyValuePair<List<PlanMove>, St>>();
                    AddArrival(arr, n, m, "SPSUMMON_SUCCESS", mv);
                    outs.AddRange(arr);
                }
            }
            if (targets.Count == 0)
            {
                // Unparsed target: count the value if the source zone has anything, but do not continue a chain.
                bool any = main.Location.Contains("DECK") || b.Grave.Values.Sum() > 0;
                if (any)
                {
                    PlanMove um = new PlanMove { Kind = kind, CardId = c.Data.Id, EffectIndex = e.Index, Text = label + " (대상 미해석)", Gain = baseGain + fBase };
                    um.CostIds.AddRange(paid);
                    outs.Add(new KeyValuePair<List<PlanMove>, St>(new List<PlanMove> { um }, b));
                }
            }
            return outs;
        }

        /// <summary>Costs: discard/send from hand or field (cheapest card), or an Extra Deck monster to the GY. False when unpayable.</summary>
        private bool PayCosts(St s, CardEntry c, EffectEntry e, out double cost) { List<int> unused; return PayCosts(s, c, e, out cost, out unused); }

        private bool PayCosts(St s, CardEntry c, EffectEntry e, out double cost, out List<int> paid)
        {
            cost = 0;
            paid = new List<int>();
            foreach (CardLink l in e.Links.Where(x => x.Role == "cost"))
            {
                if (l.Relation.Contains("엑스트라 덱에서 묘지로"))
                {
                    int xid = PickExtraFodder(s, l);
                    if (xid == 0) return false;
                    s.Add(s.Extra, xid, -1);
                    s.Add(s.Grave, xid, 1);
                    cost += _w.BaseOf("cost_extra_to_grave") + FirePayoff(s, xid);
                }
                else if (l.Location != null && l.Location.Contains("DECK") && !l.Location.Contains("HAND") && l.Relation.Contains("묘지로"))
                {
                    // e.g. Rafflesia: send a "Hole" trap from the deck. Take one that is still there.
                    int did = l.Targets.FirstOrDefault(t => s.Count(s.Deck, t) > 0 && !_w.Excluded.Contains(t));
                    if (did == 0) { if (l.Filter == null || l.Filter.Partial || l.AnyCard) continue; return false; }
                    s.Add(s.Deck, did, -1);
                    s.Add(s.Grave, did, 1);
                }
                else if (l.Relation.Contains("묘지로"))
                {
                    // cheapest card in hand (field cards are kept: they carry the interruptions)
                    int pick = 0; double best = double.MaxValue;
                    foreach (var kv in s.Hand.Where(x => x.Value > 0))
                    {
                        CardEntry fc = _db.Get(kv.Key);
                        if (fc == null) continue;
                        double v = StaticValue(s, fc);
                        if (v < best) { best = v; pick = kv.Key; }
                    }
                    if (pick == 0) return false;
                    paid.Add(pick);
                    s.TakeFromHand(pick);
                    s.Add(s.Grave, pick, 1);
                    cost -= best;
                }
            }
            return true;
        }

        /// <summary>Extra Deck monster to send as a cost: excluded cards first, then the least useful.</summary>
        private int PickExtraFodder(St s, CardLink l = null)
        {
            HashSet<int> allowed = l != null && l.Targets.Count > 0 ? new HashSet<int>(l.Targets) : null;
            return s.Extra.Where(kv => kv.Value > 0 && (allowed == null || allowed.Contains(kv.Key)))
                          .OrderBy(kv => FodderCost(s, kv.Key))
                          .Select(kv => kv.Key).FirstOrDefault();
        }

        /// <summary>
        /// Order for sending an Extra Deck card to the GY (lower first): cards whose "sent to the GY" effect
        /// pays off (attached and detached later, or a negation with an opponent's face-up card to hit), then excluded cards,
        /// then the least useful to keep (a copy that cannot be summoned now, a card worth little on our field).
        /// </summary>
        public double FodderCost(ZoneSnapshot z, int id)
        {
            return FodderCost(FromSnapshot(z, _db), id);
        }

        /// <summary>
        /// Order for banishing an Extra Deck card (Pot of Prosperity, lower first): the opposite of the GY order for payoff
        /// cards, since a banished card never reaches the GY — keep payoff cards (their kept value), cards a GY → Deck effect
        /// recycles, and useful Xyz; banish excluded and least useful cards first.
        /// </summary>
        public double BanishCost(ZoneSnapshot z, int id)
        {
            St s = FromSnapshot(z, _db);
            return PayoffKeep(id) * 100 + (_w.Excluded.Contains(id) ? -20 : 0) + (Recyclable(id) ? 10 : 0) + ExtraUsefulness(s, id) * 0.01;
        }

        /// <summary>What a card attached as material is worth for its GY payoff on the opponent's turn (attach-from-GY picks).</summary>
        public double AttachValue(int id) { return PayoffKeep(id); }

        /// <remarks>
        /// Tiers (lower first): a card that pays off in the GY (Eclipse Twins ③, Aggregator ③) → excluded cards → a spare
        /// copy of an Xyz our GY → Deck effect can bring back (Ryzeal Cross ③ returns Ryzeal Xyz to the Extra Deck), keeping
        /// one copy to summon this turn unless one is already on our field → the rest, least useful first.
        /// </remarks>
        private double FodderCost(St s, int id)
        {
            return -GraveBonus(id) * 100 + (_w.Excluded.Contains(id) ? -20 : 0) + (SpareRecyclable(s, id) ? 0 : 10) + ExtraUsefulness(s, id) * 0.01;
        }

        /// <summary>A copy beyond the one we would summon this turn, of an Xyz one of our GY → Deck effects can return.</summary>
        private bool SpareRecyclable(St s, int id)
        {
            int n;
            if (!IsXyz(id) || !Recyclable(id) || !s.Extra.TryGetValue(id, out n)) return false;
            int keep = s.Mons.Any(m => m.Id == id) ? 0 : 1; // already summoned this turn: no copy to keep
            return n - keep > 0;
        }

        private readonly Dictionary<int, bool> _recyclable = new Dictionary<int, bool>();

        /// <summary>Some effect of ours returns this card from the GY to the Deck (e.g. Ryzeal Cross ③ for Ryzeal Xyz).</summary>
        private bool Recyclable(int id)
        {
            bool r;
            if (_recyclable.TryGetValue(id, out r)) return r;
            r = _db.Cards.Values.Where(c => !c.Opponent && c.Data.Id != id)
                   .Any(c => c.Effects.Any(e => e.Links.Any(l => l.Role == "effect" && !l.Opponent && l.Relation.StartsWith("묘지에서 덱으로") && l.Targets.Contains(id))));
            _recyclable[id] = r;
            return r;
        }

        /// <summary>
        /// A face-up card of ours forbids Xyz summoning a monster with the same name as a card we control (Ryzeal Cross ①)
        /// and that monster is already on our field: its Extra Deck copies cannot come out.
        /// </summary>
        private bool SameNameBlocked(St s, int id)
        {
            if (!IsXyz(id) || !s.Mons.Any(m => m.Id == id)) return false;
            return s.Spells.Any(x => { CardEntry c = _db.Get(x); return c != null && c.NoSameNameXyz; });
        }

        /// <summary>Worth of having this card in the GY: grave_fodder × its best "sent to the GY" trigger (interruption or function value).</summary>
        public double GraveBonus(int id)
        {
            CardEntry c = _db.Get(id);
            if (c == null || _w.Excluded.Contains(id)) return 0;
            double best = 0;
            foreach (EffectEntry e in c.Effects)
            {
                if (!e.Kind.StartsWith("유발 효과") || !e.Events.Any(ev => ev.Contains("TO_GRAVE"))) continue;
                // A negation / removal fired on our own turn needs an opponent's face-up card: with none, it could only hit ours.
                best = Math.Max(best, PayoffRaw(c, e, _oppFaceUp == 0));
            }
            return best * _w.BaseOf("grave_fodder");
        }

        /// <summary>What one "sent to the GY" trigger brings when it fires: its interruption value or its best own function
        /// (no timing weight; GravePayoffReader applies that). noTargets: the opponent has no face-up card to hit.
        /// useBoard: a turn negate is valued by the opponent's monsters now (false: the plain interruption value, for a later turn).</summary>
        public double PayoffRaw(CardEntry c, EffectEntry e, bool noTargets, bool useBoard = true)
        {
            double v = _int.IsInterrupt(c, e) ? _int.Value(c, e, false) : 0;
            if (noTargets && HitsFieldCards(c, e)) v = 0;
            // Negating a face-up card until the End Phase (Aggregator ③) on our turn: worth what the best target still holds
            // against us this turn (its unused interruptions), when we know their monsters. GRAVE_PAYOFF.md 6.
            else if (useBoard && IsTurnNegate(c, e) && _oppMons.Any(m => m.FaceUp)) v = _oppMons.Where(m => m.FaceUp).Max(m => m.Held);
            foreach (CardLink l in e.Links.Where(x => x.Role == "effect" && !x.Opponent))
            {
                string f = ValueModel.FunctionOf(l);
                if (f != null) v = Math.Max(v, _w.BaseOf(f));
            }
            return v;
        }

        /// <summary>What one material of this monster is worth: the interruption uses it fuels (e.g. Detonator ①), 0 for materials only used as costs this turn.</summary>
        public double MaterialWorth(int id, int mats)
        {
            CardEntry c = _db.Get(id);
            if (c == null || _w.Excluded.Contains(id)) return 0;
            double v = 0;
            foreach (EffectEntry e in c.Effects)
            {
                if (!_int.IsInterrupt(c, e) || _int.TypeValue(c, e) <= 0) continue;
                if (e.Kind.Contains("DESTROY_REPLACE")) { if (mats <= 1) v += _int.Value(c, e, false); continue; }
                if (e.DetachCount <= 0) continue;
                bool fuels = e.Limit == null || mats / e.DetachCount <= e.Limit.Count; // one fewer material removes a use
                if (fuels) v += _int.Value(c, e, false) / e.DetachCount;
            }
            return v;
        }

        /// <summary>Negation or removal of a card on the field (e.g. Mereologic Aggregator ③): needs a target on the opponent's field.</summary>
        /// <summary>"Negate the effects of a face-up card until the End Phase" (no destruction): only what the card still has
        /// to use that turn is lost.</summary>
        public bool IsTurnNegate(CardEntry c, EffectEntry e)
        {
            return _int.TypeName(c, e) == "효과 무효" && HitsFieldCards(c, e);
        }

        public bool HitsFieldCards(CardEntry c, EffectEntry e) { return _int.HitsFieldCards(c, e); }

        /// <summary>
        /// Worth of keeping an Extra Deck card: 0 if it cannot be summoned now (same-name lock), otherwise what it would do on
        /// our field (unused functions + interruptions). Breakthrough cards with little to hold on the opponent's turn
        /// (Evilswarm, Tornado Dragon) come out low; Detonator, Duo Drive, Rafflesia high.
        /// </summary>
        private double ExtraUsefulness(St s, int id)
        {
            CardEntry c = _db.Get(id);
            if (c == null) return 0;
            if (SameNameBlocked(s, id)) return 0;
            return 1 + StaticValue(s, c);
        }

        /// <summary>Rough worth of a card in hand for choosing what to discard: its unused functions plus its hand-trap value.</summary>
        private double StaticValue(St s, CardEntry c)
        {
            double v = 0;
            foreach (EffectEntry e in c.Effects)
            {
                if (s.Used.Contains(e.UsageKey) || DeckBlocked(e, s)) continue;
                if (e.Kind.StartsWith("룰")) v += _w.BaseOf("rule_special_summon");
                foreach (CardLink l in e.Links.Where(x => x.Role == "effect"))
                {
                    if (DeckDepleted(l, s)) continue;
                    string f = ValueModel.FunctionOf(l);
                    if (f != null) v += _w.BaseOf(f);
                }
                if (_int.IsInterrupt(c, e)) v += _int.Value(c, e, (e.Range != null && e.Range.Contains("HAND")) || c.TrapActInHand);
            }
            // A card in hand whose effects are spent this turn still starts next turn (the last copy held is the one given up).
            int copies = s.Count(s.Hand, c.Data.Id);
            if (copies > 0) v = Math.Max(v, ValueModel.FollowOf(_w, NextTurn(c.Data.Id), copies));
            return v;
        }

        // ---------------------------------------------------------- returning to the deck, drawing

        /// <summary>
        /// Worth of putting this card back in the deck: if none is left there and one of our effects fetches it from the deck
        /// (search / summon / set), next turn that pick works again: follow.discount × the best such function's base.
        /// </summary>
        private double ReturnValue(St s, int id)
        {
            if (id == 0) return 0;
            CardEntry card = _db.Get(id);
            if (card == null || (!IsExtraType(card) && s.Left(id) > 0)) return 0;
            double best = 0;
            foreach (CardEntry c in _db.Cards.Values)
            {
                if (c.Opponent) continue;
                foreach (EffectEntry e in c.Effects)
                    foreach (CardLink l in e.Links)
                    {
                        if (l.Role != "effect" || l.Opponent || l.Location == null || !l.Location.Contains("DECK") || !l.Targets.Contains(id)) continue;
                        string f = ValueModel.FunctionOf(l);
                        if (f != null) best = Math.Max(best, _w.BaseOf(f));
                    }
            }
            return _w.BaseOf("follow.discount") * best;
        }

        /// <summary>What a card in the GY still does for us there: the best grave function (revive, attach) of an effect that picks it.</summary>
        private double GraveUse(int id)
        {
            if (id == 0) return 0;
            double best = 0;
            foreach (CardEntry c in _db.Cards.Values)
            {
                if (c.Opponent) continue;
                foreach (EffectEntry e in c.Effects)
                    foreach (CardLink l in e.Links)
                    {
                        if (l.Role != "effect" || l.Opponent || l.Location == null || !l.Location.Contains("GRAVE") || !l.Targets.Contains(id)) continue;
                        string f = ValueModel.FunctionOf(l);
                        if (f == "special_summon_from_grave" || f == "attach_from_grave") best = Math.Max(best, _w.BaseOf(f));
                    }
            }
            return best * _w.BaseOf("follow.discount");
        }

        /// <summary>The cards a "GY → deck" effect returns: the ones worth most back in the deck and least in the GY. Null if the GY has too few.</summary>
        private List<int> PickReturns(St s, CardLink l)
        {
            HashSet<int> ok = new HashSet<int>(l.Targets);
            bool any = l.Filter == null || l.Filter.Partial || l.AnyCard;
            List<int> pool = new List<int>();
            foreach (var kv in s.Grave.Where(x => x.Value > 0))
                if (kv.Key == 0 || any || ok.Contains(kv.Key)) // detached materials (identity unknown) are counted as fitting
                    for (int i = 0; i < kv.Value; ++i) pool.Add(kv.Key);
            if (pool.Count < l.Count) return null;
            List<int> picks = new List<int>();
            foreach (int id in pool.OrderByDescending(x => ReturnValue(s, x) - GraveUse(x)))
            {
                if (picks.Count >= l.Count) break;
                // a second copy of the same card refuels nothing more
                if (picks.Contains(id) && id != 0 && pool.Any(x => !picks.Contains(x))) continue;
                picks.Add(id);
            }
            while (picks.Count < l.Count) picks.Add(pool.First(x => picks.Count(y => y == x) < pool.Count(y => y == x)));
            return picks;
        }

        /// <summary>Priority for returning one of our GY cards to the deck (higher first), for the engine's pick.</summary>
        public double ReturnPriority(ZoneSnapshot z, int id)
        {
            return ReturnValue(FromSnapshot(z, _db), id) - GraveUse(id);
        }

        private readonly Dictionary<int, double> _drawWorth = new Dictionary<int, double>();

        /// <summary>Expected worth of one unknown draw: over the cards left in the deck above the ones put on the bottom.</summary>
        private double DrawExpect(St s)
        {
            Dictionary<int, int> pool = new Dictionary<int, int>(s.Deck);
            foreach (int id in s.Bottom) if (pool.ContainsKey(id)) pool[id] = Math.Max(0, pool[id] - 1);
            int n = pool.Values.Sum();
            if (n <= 0) return 0;
            double total = 0;
            foreach (var kv in pool.Where(x => x.Value > 0)) total += kv.Value * DrawWorth(kv.Key);
            return total / n;
        }

        /// <summary>A drawn card: an interruption we can hold for the opponent's turn (hand trap, settable trap), or its next-turn follow-up.</summary>
        private double DrawWorth(int id)
        {
            double v;
            if (_drawWorth.TryGetValue(id, out v)) return v;
            CardEntry c = _db.Get(id);
            v = 0;
            if (c != null && !_w.Excluded.Contains(id))
            {
                bool trap = (c.Data.Type & 0x4) != 0, quickPlay = (c.Data.Type & 0x10000) != 0;
                foreach (EffectEntry e in c.Effects)
                {
                    if (!_int.IsInterrupt(c, e) || _int.TypeValue(c, e) <= 0) continue;
                    if ((trap || quickPlay) && e.Kind.StartsWith("카드의 발동")) v = Math.Max(v, _int.Value(c, e, false));
                    else if (e.Range != null && e.Range.Contains("HAND")) v = Math.Max(v, _int.Value(c, e, true));
                }
                v = Math.Max(v, ValueModel.FollowOf(_w, NextTurn(id), 1));
            }
            _drawWorth[id] = v;
            return v;
        }

        /// <summary>
        /// Which material to detach (GRAVE_PAYOFF.md 5 step 2): the one whose GY payoff gains most by going now
        /// (payoff now − payoff kept for the opponent's turn). Plain materials score 0: a payoff card with a target now goes
        /// first, one that is worth more kept (no target, key already spent) stays attached. Ties: unknown, then oldest.
        /// </summary>
        private int DetachPick(St s, Mon m)
        {
            int best = m.MatIds[0];
            double bestScore = double.MinValue;
            foreach (int id in m.MatIds.Distinct().OrderBy(x => x == 0 ? 0 : 1))
            {
                double score = id == 0 ? 0 : PayoffNow(s, id) - PayoffKeep(id);
                if (score > bestScore + 1e-9) { best = id; bestScore = score; }
            }
            return best;
        }

        /// <summary>The best GY payoff of a card firing now: its once-per-turn key unused this turn, and a GY target for
        /// payoffs that take one (Eclipse Twins: another Xyz in our GY). Weighted gy_payoff.now.</summary>
        private double PayoffNow(St s, int id) { GravePayoff p; return PayoffNow(s, id, out p); }

        private double PayoffNow(St s, int id, out GravePayoff best)
        {
            best = null;
            CardEntry c = _db.Get(id);
            if (c == null || _w.Excluded.Contains(id)) return 0;
            double v = 0;
            foreach (GravePayoff p in _payoff.Of(id))
            {
                if (p.Key != null && s.Used.Contains(p.Key)) continue;
                if (!GraveTargetsReady(s, p.Effect, id)) continue;
                double x = PayoffRaw(c, p.Effect, _oppFaceUp == 0) * _payoff.TimingWeight(PayoffTiming.Now);
                if (x > v) { v = x; best = p; }
            }
            return v;
        }

        /// <summary>The same payoff kept for a detach on the opponent's turn (key fresh again): plain value × gy_payoff.opp_turn.</summary>
        private double PayoffKeep(int id)
        {
            CardEntry c = _db.Get(id);
            if (c == null || _w.Excluded.Contains(id)) return 0;
            double v = 0;
            foreach (GravePayoff p in _payoff.Of(id))
                v = Math.Max(v, PayoffRaw(c, p.Effect, false, false) * _payoff.TimingWeight(PayoffTiming.OpponentTurn));
            return v;
        }

        /// <summary>A payoff that brings a card from our GY needs one there (other than the card itself).</summary>
        private bool GraveTargetsReady(St s, EffectEntry e, int self)
        {
            foreach (CardLink l in e.Links.Where(x => x.Role == "effect" && !x.Opponent && x.Location != null && x.Location.Contains("GRAVE")))
            {
                if (l.Filter == null || l.Filter.Partial || l.AnyCard || l.Targets.Count == 0) continue;
                if (!l.Targets.Any(t => t != self ? s.Count(s.Grave, t) > 0 : s.Count(s.Grave, t) > 1)) return false;
            }
            return true;
        }

        /// <summary>A card just sent to the GY: fire its payoff now if it has one (key spent, value returned).</summary>
        private double FirePayoff(St s, int id)
        {
            GravePayoff p;
            double v = PayoffNow(s, id, out p);
            if (p != null && p.Key != null) s.Used.Add(p.Key);
            return v;
        }

        /// <summary>
        /// Which GY monster an attach-from-GY effect puts on the Xyz monster `to`: a payoff card when that monster will be able
        /// to detach it on the opponent's turn (Detonator ①/③), otherwise the first one (as before). Restricted to the
        /// effect's readable targets.
        /// </summary>
        private int AttachPick(St s, Mon to, CardLink l)
        {
            bool readable = l.Filter != null && !l.Filter.Partial && !l.AnyCard && l.Targets.Count > 0;
            HashSet<int> ok = readable ? new HashSet<int>(l.Targets) : null;
            List<int> cands = s.Grave.Where(kv => kv.Value > 0 && IsMonsterId(kv.Key) && (ok == null || ok.Contains(kv.Key))).Select(kv => kv.Key).ToList();
            if (cands.Count == 0) return 0;
            if (OppTurnDetaches(to.Id, to.Mats + 1) > 0)
            {
                int pay = cands.OrderByDescending(PayoffKeep).First();
                if (PayoffKeep(pay) > 0) return pay;
            }
            return cands[0];
        }

        /// <summary>How many materials this monster can detach on the opponent's turn: its interruption detach costs
        /// (Detonator ① up to its limit) plus a destroy replacement (Detonator ③).</summary>
        private int OppTurnDetaches(int id, int mats)
        {
            CardEntry c = _db.Get(id);
            if (c == null || mats <= 0) return 0;
            int n = 0;
            foreach (EffectEntry e in c.Effects)
            {
                if (!_int.IsInterrupt(c, e) || _int.TypeValue(c, e) <= 0) continue;
                if (e.Kind.Contains("DESTROY_REPLACE")) n += 1;
                else if (e.DetachCount > 0) n += e.Limit != null ? Math.Min(e.Limit.Count, mats / e.DetachCount) : mats / e.DetachCount;
            }
            return Math.Min(n, mats);
        }

        /// <summary>Plan-label text for the materials a move detached.</summary>
        private string DetachText(List<int> ids)
        {
            return ids.Count == 0 ? "" : " [떼기: " + String.Join(", ", ids.Select(x => x == 0 ? "소재" : Name(_db.Get(x))).ToArray()) + "]";
        }

        /// <summary>Plan-label text for an Xyz monster's materials.</summary>
        private string MatText(Mon m)
        {
            return m.Mats == 0 ? "" : " [소재: " + String.Join(", ", m.MatIds.Select(x => x == 0 ? "?" : Name(_db.Get(x))).ToArray()) + "]";
        }

        /// <summary>Detaches n materials (into the GY by id) and returns them; fired = the GY payoffs that fire now.</summary>
        private List<int> Detach(St s, int? from, int n, out double fired)
        {
            fired = 0;
            List<int> off = new List<int>();
            for (int k = 0; k < n; ++k)
            {
                // Own materials first; otherwise the monster whose material fuels the least (Duo Drive before Detonator).
                Mon m = from.HasValue && from.Value < s.Mons.Count && s.Mons[from.Value].Mats > 0 ? s.Mons[from.Value]
                      : s.Mons.Where(x => x.Mats > 0).OrderBy(x => MaterialWorth(x.Id, x.Mats)).ThenByDescending(x => x.Mats).FirstOrDefault();
                if (m == null) return off;
                int mid = DetachPick(s, m);
                off.Add(mid);
                m.MatIds.Remove(mid);
                s.Add(s.Grave, mid, 1); // the detached material goes to the GY as itself (0 when unknown)
                if (mid != 0) fired += FirePayoff(s, mid);
            }
            return off;
        }

        /// <summary>Existence / absence conditions over our monsters, hand and GY (unparsed ones count as met).</summary>
        private bool Conditions(EffectEntry e, St s)
        {
            if (e.RequiresEmptyField && (s.Mons.Count > 0 || s.Spells.Count > 0)) return false;
            foreach (CardLink l in e.Links.Where(x => x.Role == "condition" && !x.Opponent && x.Filter != null && !x.Filter.Partial && !x.AnyCard))
            {
                HashSet<int> ok = new HashSet<int>(l.Targets);
                bool any = false;
                if (l.Location.Contains("MZONE") || l.Location.Contains("ONFIELD")) any |= s.Mons.Any(m => ok.Contains(m.Id));
                if (l.Location.Contains("GRAVE")) any |= s.Grave.Any(kv => kv.Value > 0 && ok.Contains(kv.Key));
                if (l.Location.Contains("HAND")) any |= s.Hand.Any(kv => kv.Value > 0 && ok.Contains(kv.Key));
                if (l.Location.Contains("EXTRA")) any |= s.Extra.Any(kv => kv.Value > 0 && ok.Contains(kv.Key));
                if (l.Relation.StartsWith("존재") && !any) return false;
                if (l.Relation.StartsWith("부재") && any) return false;
            }
            return true;
        }

        // ---------------------------------------------------------- end of turn

        /// <summary>Interruptions we hold for the opponent's turn: hand traps, set traps / quick-plays, field effects (with materials).</summary>
        private double EndOfTurn(St s, List<string> board)
        {
            double total = 0;
            bool fieldEmpty = s.Mons.Count == 0 && s.Spells.Count == 0 && s.Inactive.Count == 0;
            int spellZones = s.Spells.Count + s.Inactive.Count;
            foreach (int id in s.Inactive) { CardEntry ic = _db.Get(id); if (ic != null) board.Add(Name(ic) + "(세트, 미발동 → 방해 0)"); }
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);

            // Hand
            foreach (var kv in s.Hand.Where(x => x.Value > 0))
            {
                CardEntry c = _db.Get(kv.Key);
                if (c == null || _w.Excluded.Contains(kv.Key)) continue;
                bool trap = (c.Data.Type & 0x4) != 0, quickPlay = (c.Data.Type & 0x10000) != 0;
                if (trap || quickPlay)
                {
                    // Per card copy: kept in hand (traps that may activate from hand) or set; its activation is the interruption.
                    EffectEntry act = c.Effects.FirstOrDefault(e => e.Kind.StartsWith("카드의 발동") && _int.IsInterrupt(c, e) && _int.TypeValue(c, e) > 0);
                    if (act == null || DeckBlocked(act, s)) continue;
                    bool fromHand = trap && c.TrapActInHand && (!c.TrapActInHandNeedsEmptyField || fieldEmpty);
                    int copies = act.Limit != null ? Math.Min(kv.Value, act.Limit.Count) : kv.Value;
                    for (int i = 0; i < copies; ++i)
                    {
                        if (fromHand) { total += _int.Value(c, act, true); board.Add(Name(c) + "(패)"); }
                        else if (spellZones < 5)
                        {
                            spellZones++;
                            int mult = _int.MultipliesByCondition(c, act) ? ConditionCount(act, s) : 1;
                            if (mult <= 0 || !FieldConditions(act, s)) { board.Add(Name(c) + "(세트, 지금은 조건 불충족)"); continue; }
                            total += _int.Value(c, act, false) * mult;
                            board.Add(Name(c) + "(세트" + (mult > 1 ? " ×" + mult : "") + ")");
                        }
                    }
                    continue;
                }
                foreach (EffectEntry e in c.Effects)
                {
                    if (!_int.IsInterrupt(c, e) || _int.TypeValue(c, e) <= 0 || DeckBlocked(e, s)) continue;
                    int uses = e.Limit != null ? Math.Min(kv.Value, e.Limit.Count) : kv.Value;
                    if (e.Range == null || !e.Range.Contains("HAND")) continue;
                    if (e.RequiresEmptyField && !fieldEmpty) continue;
                    if (!keys.Add(e.UsageKey + "#hand")) continue;
                    for (int i = 0; i < uses; ++i) { total += _int.Value(c, e, true); board.Add(Name(c) + "(패)"); }
                }
            }

            // Spell & trap zone (set traps / quick-plays, face-up field spells)
            foreach (int id in s.Spells)
            {
                CardEntry c = _db.Get(id);
                if (c == null || _w.Excluded.Contains(id)) continue;
                foreach (EffectEntry e in c.Effects)
                {
                    bool usableHere = e.Kind.StartsWith("카드의 발동") || (e.Range != null && (e.Range.Contains("SZONE") || e.Range.Contains("FZONE")));
                    if (!usableHere || !_int.IsInterrupt(c, e) || _int.TypeValue(c, e) <= 0 || DeckBlocked(e, s) || !keys.Add(e.UsageKey + "#zone")) continue;
                    if (!FieldConditions(e, s)) continue;
                    double v = _int.Value(c, e, false);
                    int mult = _int.MultipliesByCondition(c, e) ? ConditionCount(e, s) : 1;
                    if (mult <= 0) continue;
                    total += v * mult;
                    board.Add(Name(c) + (mult > 1 ? " ×" + mult : ""));
                }
            }

            // Monsters. Effects limited per card name (e.g. Detonator ③ "1턴 1번") are shared by all copies on the field.
            Dictionary<string, int> left = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Mon m in s.Mons)
            {
                CardEntry c = _db.Get(m.Id);
                if (c == null || m.Negated || _w.Excluded.Contains(m.Id)) continue;
                foreach (EffectEntry e in c.Effects)
                {
                    if (!_int.IsInterrupt(c, e) || _int.TypeValue(c, e) <= 0) continue;
                    if (DeckBlocked(e, s)) { board.Add(Name(c) + "(" + e.Index + ", 덱에 대상 없음 → 0)"); continue; }
                    if (e.Range != null && !e.Range.Contains("MZONE") && !e.Kind.Contains("DESTROY_REPLACE")) continue;
                    int uses;
                    if (e.Kind.Contains("DESTROY_REPLACE")) uses = m.Mats > 0 ? 1 : 0;
                    else if (e.DetachCount > 0) uses = e.Limit != null ? Math.Min(e.Limit.Count, m.Mats / e.DetachCount) : m.Mats / e.DetachCount;
                    else uses = e.Limit != null ? e.Limit.Count : 1;
                    if (e.Limit != null)
                    {
                        int remaining;
                        if (!left.TryGetValue(e.UsageKey, out remaining)) remaining = e.Limit.Count;
                        uses = Math.Min(uses, remaining);
                        left[e.UsageKey] = remaining - uses;
                    }
                    if (uses <= 0 || !FieldConditions(e, s)) continue;
                    total += _int.Value(c, e, false) * uses;
                    board.Add(Name(c) + "(" + e.Index + ")" + (uses > 1 ? " ×" + uses : ""));
                }
            }

            // Materials with a GY payoff that a monster can detach on the opponent's turn (Detonator ①/③ → Aggregator ③,
            // Eclipse Twins ③): their once-per-turn key is fresh again there. Best payoffs first, one per key (GRAVE_PAYOFF.md 5 step 3).
            HashSet<string> payKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Mon m in s.Mons)
            {
                if (m.Negated || m.Mats == 0) continue;
                int opp = OppTurnDetaches(m.Id, m.Mats);
                foreach (int id in m.MatIds.Where(x => x != 0).OrderByDescending(PayoffKeep).ToList())
                {
                    if (opp <= 0) break;
                    double v = PayoffKeep(id);
                    if (v <= 0) break;
                    GravePayoff p = _payoff.Of(id).FirstOrDefault();
                    if (p != null && p.Key != null && !payKeys.Add(p.Key)) continue;
                    opp--;
                    total += v;
                    board.Add(Name(_db.Get(id)) + "(" + Name(_db.Get(m.Id)) + " 소재, 상대 턴 묘지 효과)");
                }
            }
            return total;
        }

        private bool FieldConditions(EffectEntry e, St s)
        {
            foreach (CardLink l in e.Links.Where(x => x.Role == "condition" && !x.Opponent && x.Filter != null && !x.Filter.Partial && !x.AnyCard))
            {
                if (!(l.Location.Contains("MZONE") || l.Location.Contains("ONFIELD"))) continue;
                HashSet<int> ok = new HashSet<int>(l.Targets);
                bool any = s.Mons.Any(m => ok.Contains(m.Id) && (e.DetachCount == 0 || m.Mats >= e.DetachCount));
                if (l.Relation.StartsWith("존재") && !any) return false;
            }
            return true;
        }

        private int ConditionCount(EffectEntry e, St s)
        {
            CardLink l = e.Links.FirstOrDefault(x => x.Role == "condition" && !x.Opponent && x.Targets.Count > 0);
            if (l == null) return 1;
            HashSet<int> ok = new HashSet<int>(l.Targets);
            return Math.Min(s.Mons.Count(m => ok.Contains(m.Id)), _targetCap); // e.g. Plasma Hole: no more than there is to destroy
        }

        // ---------------------------------------------------------- helpers

        private static string Arrival(CardLink l)
        {
            string f = ValueModel.FunctionOf(l);
            if (f == "special_summon_from_deck" || f == "special_summon_from_grave") return "SPSUMMON_SUCCESS";
            if (f == "search") return "HAND";
            if (f == "set_from_deck") return "SET";
            return null;
        }

        private int MaterialLevel(CardEntry c)
        {
            string r = c.Material.Relation;
            int start = r.IndexOf("레벨 ", StringComparison.Ordinal);
            if (start < 0) return -1;
            start += 3;
            int end = r.IndexOf(' ', start);
            int lv;
            return int.TryParse(r.Substring(start, end - start), out lv) ? lv : -1;
        }

        /// <summary>Nothing left in the deck for this effect's deck cost or for every card it would bring: it does nothing (value 0).</summary>
        private bool DeckBlocked(EffectEntry e, St s) { return ValueModel.DeckBlocked(e, s.Left, _w.Excluded); }
        private bool DeckDepleted(CardLink l, St s) { return ValueModel.DeckDepleted(l, s.Left, _w.Excluded); }

        private bool IsMonsterId(int id) { CardEntry c = _db.Get(id); return c != null && (c.Data.Type & 0x1) != 0; }
        private bool IsXyz(int id) { CardEntry c = _db.Get(id); return c != null && (c.Data.Type & 0x800000) != 0; }
        private static bool IsExtraType(CardEntry c) { return c != null && (c.Data.Type & (0x40 | 0x2000 | 0x800000 | 0x4000000)) != 0; }
        private static string Name(CardEntry c) { return c == null ? "?" : c.Data.Name; }
        private string CostText(List<int> paid) { return paid.Count == 0 ? "" : " [코스트: " + String.Join(", ", paid.Select(id => Name(_db.Get(id))).ToArray()) + "]"; }
    }
}
