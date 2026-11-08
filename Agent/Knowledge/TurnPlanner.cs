using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindBot.Agent.Knowledge
{
    /// <summary>Our own visible cards, as plain ids, for the planner.</summary>
    public sealed class ZoneSnapshot
    {
        public readonly List<int> Hand = new List<int>();
        /// <summary>Monster id and attached material count.</summary>
        public readonly List<KeyValuePair<int, int>> Monsters = new List<KeyValuePair<int, int>>();
        public readonly List<int> Spells = new List<int>();
        /// <summary>Face-down spells that do nothing until activated (field / continuous / normal spells). Set traps and quick-plays stay in Spells.</summary>
        public readonly List<int> SetSpells = new List<int>();
        public readonly List<int> Grave = new List<int>();
        public readonly List<int> Banished = new List<int>();
        public bool NormalSummonAvailable = true;
        /// <summary>Opponent's face-up cards on the field, or -1 if not known (offline tests).</summary>
        public int OpponentFaceUp = -1;
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
              .Append(" + 후속 ").Append(FollowValue.ToString("0.#", CultureInfo.InvariantCulture))
              .Append(" − 효율 ").Append(HandSpent).Append("장)");
            if (Moves.Count > 0) sb.Append(" 행동: ").Append(String.Join(" → ", Moves.Select(m => m.Text).ToArray()));
            if (EndBoard.Count > 0) sb.Append(" | 방해: ").Append(String.Join(", ", EndBoard.ToArray()));
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
        private Dictionary<string, PlanResult> _memo;
        public int BudgetPerFirstMove = 40000;
        public int MaxDepth = 16; // main moves per turn (a Duo Drive + 2 Detonator line already takes about 10)
        public long MillisPerPlan = 4000; // wall-clock cap for one Plan call, shared evenly by the first moves left (results past it count as truncated)
        private readonly System.Diagnostics.Stopwatch _clock = new System.Diagnostics.Stopwatch();
        private long _moveDeadline;

        public TurnPlanner(GameCardDb db, ValueWeights w)
        {
            _db = db;
            _w = w;
            _int = new InterruptModel(w);
        }

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
            public int Mats;
            public bool Negated;
            public Mon Clone() { return new Mon { Id = Id, Mats = Mats, Negated = Negated }; }
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
                St s = new St { NS = NS, HandSpent = HandSpent, Draws = Draws, Bonus = Bonus };
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
                foreach (Mon m in Mons.OrderBy(x => x.Id).ThenBy(x => x.Mats)) sb.Append(m.Id).Append(':').Append(m.Mats).Append(m.Negated ? "n" : "").Append(',');
                sb.Append('|');
                foreach (int i in Spells.OrderBy(x => x)) sb.Append(i).Append(',');
                foreach (int i in Inactive.OrderBy(x => x)) sb.Append('s').Append(i).Append(',');
                sb.Append('|').Append(Grave.Values.Sum()).Append('|');
                foreach (string u in Used.OrderBy(x => x, StringComparer.Ordinal)) sb.Append(u).Append(',');
                sb.Append('|').Append(NS ? 1 : 0).Append('|').Append(HandSpent).Append('|').Append(Draws);
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

        private St FromSnapshot(ZoneSnapshot z, GameCardDb db)
        {
            _targetCap = Math.Max(z.OpponentFaceUp, (int)_w.BaseOf("int.expected_targets"));
            _oppFaceUp = z.OpponentFaceUp;
            St s = new St { NS = z.NormalSummonAvailable };
            foreach (int id in z.Hand) { s.Add(s.Hand, id, 1); s.Add(s.Orig, id, 1); }
            foreach (var m in z.Monsters) s.Mons.Add(new Mon { Id = m.Key, Mats = m.Value });
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
                }
            return s;
        }

        // ---------------------------------------------------------- public API

        /// <summary>Best plan from this state, and the best plan for each possible first move.</summary>
        public PlanResult Plan(ZoneSnapshot z, out List<PlanResult> byFirstMove, out PlanResult stop)
        {
            _memo = new Dictionary<string, PlanResult>(StringComparer.Ordinal);
            _next.Clear();
            _drawWorth.Clear();
            St s = FromSnapshot(z, _db);
            stop = StopResult(s);
            byFirstMove = new List<PlanResult>();
            PlanResult best = stop;
            var firstMoves = Moves(s, 0).ToList();
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
                PlanResult rest = Search(mv.Value, 1);
                PlanResult r = Combine(mv.Key, rest, 0);
                byFirstMove.Add(r);
                if (r.Value > best.Value) best = r;
            }
            return best;
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

        private PlanResult Search(St s, int k)
        {
            string key = k + "#" + s.Key();
            PlanResult cached;
            if (_memo.TryGetValue(key, out cached)) return cached;
            PlanResult best = StopResult(s);
            bool truncated = false;
            if (k < MaxDepth)
            {
                if (--_budget > 0 && _clock.ElapsedMilliseconds < _moveDeadline)
                {
                    foreach (var mv in Moves(s, k))
                    {
                        PlanResult sub = Search(mv.Value, k + 1);
                        truncated |= sub.Truncated;
                        PlanResult r = Combine(mv.Key, sub, k);
                        if (r.Value > best.Value) best = r;
                    }
                }
                else truncated = Moves(s, k).Any();
            }
            best.Truncated = truncated;
            if (!truncated) _memo[key] = best;
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
            r.HandSpent = rest.HandSpent;
            r.Truncated = rest.Truncated;
            r.EndBoard.AddRange(rest.EndBoard);
            r.Value = r.FlowValue + r.EndValue + r.FollowValue - _w.EfficiencyPerHandCard * r.HandSpent;
            return r;
        }

        private PlanResult StopResult(St s)
        {
            PlanResult r = new PlanResult();
            r.EndValue = EndOfTurn(s, r.EndBoard);
            r.FollowValue = FollowTerm(s) + s.Bonus + s.Draws * DrawExpect(s);
            r.HandSpent = s.HandSpent;
            r.Value = r.EndValue + r.FollowValue - _w.EfficiencyPerHandCard * s.HandSpent;
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
                        n.TakeFromHand(id);
                        double cost;
                        List<int> paid;
                        if (!PayCosts(n, c, proc, out cost, out paid)) continue;
                        Detach(n, null, proc.DetachCount);
                        Mon m = new Mon { Id = id };
                        n.Mons.Add(m);
                        double gain = _w.BaseOf("rule_special_summon") + cost + proc.DetachCount * _w.BaseOf("cost_detach") + _w.OffsetOf(id, proc.Index);
                        PlanMove rm = new PlanMove { Kind = "rule", CardId = id, EffectIndex = proc.Index, Text = Name(c) + " 룰 특수 소환" + CostText(paid), Gain = gain };
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
                if (x == null || x.Material == null || _w.Excluded.Contains(xid) || !x.Material.Relation.StartsWith("엑시즈")) continue;
                int level = MaterialLevel(x);
                HashSet<int> ok = new HashSet<int>(x.Material.Targets);
                List<int> mats = Enumerable.Range(0, s.Mons.Count)
                    .Where(i => ok.Contains(s.Mons[i].Id) && !IsExtraType(_db.Get(s.Mons[i].Id)) && _db.Get(s.Mons[i].Id).Data.Level == level)
                    .OrderBy(i => UnusedEffects(s, s.Mons[i].Id)).Take(3).ToList();
                int need = x.Material.Relation.Contains("× 3") ? 3 : 2;
                if (mats.Count < need) continue;
                St n = s.Clone();
                foreach (int i in mats.Take(need).OrderByDescending(i => i)) n.Mons.RemoveAt(i); // they become its materials
                n.Add(n.Extra, xid, -1);
                Mon xm = new Mon { Id = xid, Mats = need };
                n.Mons.Add(xm);
                AddArrival(result, n, xm, "SPSUMMON_SUCCESS", new PlanMove { Kind = "xyz", CardId = xid, Text = Name(x) + " 엑시즈 소환" });
            }
            return result;
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
            if (fromHand) b.TakeFromHand(c.Data.Id);
            double cost;
            List<int> paid;
            if (!PayCosts(b, c, e, out cost, out paid)) return outs;
            Detach(b, src != null ? (int?)s.Mons.IndexOf(src) : null, e.DetachCount);

            double baseGain = cost + e.DetachCount * _w.BaseOf("cost_detach") + _w.OffsetOf(c.Data.Id, e.Index);
            string label = Name(c) + " " + (kind == "trigger" ? "유발" : kind == "ignition" || kind == "zone" ? "기동" : "발동") + "(" + e.Index + ")" + CostText(paid);

            // Side functions that do not bring a card.
            foreach (CardLink l in e.Links.Where(x => x.Role == "effect" && !x.Opponent))
            {
                string f = ValueModel.FunctionOf(l);
                if (f == "attach_from_grave" && b.GraveMonsters(IsMonsterId) > 0)
                {
                    Mon to = src != null && IsXyz(src.Id) ? b.Mons[s.Mons.IndexOf(src)] : b.Mons.FirstOrDefault(m => IsXyz(m.Id));
                    if (to != null) { int gid = b.Grave.First(kv => kv.Value > 0 && IsMonsterId(kv.Key)).Key; b.Add(b.Grave, gid, -1); to.Mats++; baseGain += _w.BaseOf(f); label += " +소재"; }
                }
                else if (f == "attach_from_deck")
                {
                    Mon to = b.Mons.FirstOrDefault(m => IsXyz(m.Id) && _db.Get(m.Id).Data.Level == 4);
                    int did = l.Targets.FirstOrDefault(t => b.Count(b.Deck, t) > 0 && !_w.Excluded.Contains(t));
                    if (to != null && did != 0) { b.Add(b.Deck, did, -1); to.Mats++; baseGain += _w.BaseOf(f); label += " +덱→소재"; }
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
                PlanMove only = new PlanMove { Kind = kind, CardId = c.Data.Id, EffectIndex = e.Index, Text = label, Gain = baseGain };
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
                    cost += _w.BaseOf("cost_extra_to_grave") + GraveBonus(xid);
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

        private double FodderCost(St s, int id)
        {
            return -GraveBonus(id) * 100 + (_w.Excluded.Contains(id) ? -1 : 0) + ExtraUsefulness(s, id) * 0.01;
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
                double v = _int.IsInterrupt(c, e) ? _int.Value(c, e, false) : 0;
                // A negation / removal fired on our own turn needs an opponent's face-up card: with none, it could only hit ours.
                if (_oppFaceUp == 0 && HitsFieldCards(c, e)) v = 0;
                foreach (CardLink l in e.Links.Where(x => x.Role == "effect" && !x.Opponent))
                {
                    string f = ValueModel.FunctionOf(l);
                    if (f != null) v = Math.Max(v, _w.BaseOf(f));
                }
                best = Math.Max(best, v);
            }
            return best * _w.BaseOf("grave_fodder");
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
        public bool HitsFieldCards(CardEntry c, EffectEntry e)
        {
            string t = _int.TypeName(c, e);
            return e.NegateScope != null || t == "단순 파괴" || t == "단순 제외" || t == "패로 바운스" || t == "덱으로 바운스" || t == "컨트롤 탈취"
                || t == "효과 무효" || t == "발동 무효" || t == "효과 무효 후 파괴" || t == "발동 무효 후 파괴";
        }

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

        private void Detach(St s, int? from, int n)
        {
            for (int k = 0; k < n; ++k)
            {
                // Own materials first; otherwise the monster whose material fuels the least (Duo Drive before Detonator).
                Mon m = from.HasValue && from.Value < s.Mons.Count && s.Mons[from.Value].Mats > 0 ? s.Mons[from.Value]
                      : s.Mons.Where(x => x.Mats > 0).OrderBy(x => MaterialWorth(x.Id, x.Mats)).ThenByDescending(x => x.Mats).FirstOrDefault();
                if (m == null) return;
                m.Mats--;
                s.Add(s.Grave, 0, 1); // a detached material goes to the GY (identity unknown)
            }
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
