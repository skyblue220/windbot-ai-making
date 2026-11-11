using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindBot.Agent.Knowledge
{
    /// <summary>One card a removal could take, as seen now.</summary>
    public sealed class RemovalTarget
    {
        public int Id;
        public bool FaceUp = true;
        public bool Monster;
        /// <summary>The opponent's card (ours are legal for some removals, e.g. Detonator ①, but never worth taking).</summary>
        public bool Theirs = true;
        /// <summary>The effect of this card on the current chain, or null.</summary>
        public EffectEntry Chained;
        /// <summary>That chain link is the latest one (the link a response like Ghost Ogre answers).</summary>
        public bool LastLink;
        /// <summary>Xyz materials attached to it (a detach cost of its own response needs them); -1 = not known, use the field total.</summary>
        public int Materials = -1;
        /// <summary>Caller's handle (a ClientCard in the bridge, an index in the dump).</summary>
        public object Tag;
    }

    public sealed class RemovalWorth
    {
        public double Value;
        public double Body;
        public double Stop;
        public readonly List<string> Notes = new List<string>();

        public string Describe()
        {
            return F(Value) + " (몸체 " + F(Body) + (Stop > 0 ? " + 효과 정지 " + F(Stop) : "") + (Notes.Count > 0 ? "; " + String.Join(", ", Notes.ToArray()) : "") + ")";
        }

        internal static string F(double v) { return v.ToString("0.#", CultureInfo.InvariantCulture); }
    }

    public sealed class RemovalChoice
    {
        public double Gain;
        public double Hold;
        public int Max;
        /// <summary>Detach uses left this turn that the hold was scaled by (0: not scaled).</summary>
        public int UsesLeft;
        public readonly List<KeyValuePair<RemovalTarget, RemovalWorth>> Picks = new List<KeyValuePair<RemovalTarget, RemovalWorth>>();
        /// <summary>Cards it could otherwise reach that a protection keeps out (name + reason).</summary>
        public readonly List<string> Blocked = new List<string>();
        public bool Fire { get { return Gain > Hold; } }

        public string Describe()
        {
            return (Fire ? "발동" : "보류") + ": 얻는 값 " + RemovalWorth.F(Gain) + " vs 아껴 둘 값 " + RemovalWorth.F(Hold)
                 + (UsesLeft > 0 ? " (이번 턴 남은 사용 " + UsesLeft + "회)" : "")
                 + (Picks.Count > 0 ? " (최대 " + Max + "장)" : " (칠 카드 없음)")
                 + (Blocked.Count > 0 ? "; 보호로 못 침: " + String.Join(", ", Blocked.ToArray()) : "");
        }
    }

    /// <summary>
    /// What removing a card takes from the opponent right now, from game rules and Lua data (docs/CHAIN_REMOVAL.md):
    ///  1. an activated effect resolves even when its card is gone, unless its operation needs the card (EffectEntry.SourceStop);
    ///  2. a normal / quick-play spell or normal trap activated on the chain goes to the GY after resolving anyway: no body to take.
    /// One value serves every removal: which card to pick (destroy / banish / bounce), and whether to fire a removal now
    /// (Detonator ①, Ghost Ogre, Tornado Dragon, Plasma Hole ①) or keep it for a later window.
    /// </summary>
    public sealed class ChainRemovalModel
    {
        private readonly GameCardDb _db;
        private readonly ValueWeights _w;
        private readonly OpponentLossModel _loss;
        private readonly InterruptModel _im;
        /// <summary>The card's own worth as a body on the field (planner Worth), by card id.</summary>
        public Func<int, double> StaticWorth;

        public ChainRemovalModel(GameCardDb db, ValueWeights w, OpponentLossModel loss, InterruptModel im)
        {
            _db = db;
            _w = w;
            _loss = loss;
            _im = im;
        }

        /// <summary>Normal / quick-play spell or normal trap card activation: the card leaves the field once it resolves.</summary>
        public static bool LeavesAfterResolve(CardData d, EffectEntry e)
        {
            if (d == null || e == null || e.Kind != "카드의 발동") return false;
            bool spellTrap = (d.Type & 0x6) != 0;
            bool stays = (d.Type & (0x20000 /* continuous */ | 0x80000 /* field */ | 0x40000 /* equip */ | 0x1000000 /* pendulum */)) != 0;
            return spellTrap && !stays;
        }

        /// <summary>Is this one of the removals the model decides (destroy / banish / bounce of field cards, read from Lua)?</summary>
        public bool IsRemoval(CardEntry c, EffectEntry e)
        {
            if (c == null || e == null || e.Reach == null || e.NegateScope != null) return false;
            string t = _im.TypeName(c, e);
            return t == "단순 파괴" || t == "단순 제외" || t == "패로 바운스" || t == "덱으로 바운스";
        }

        /// <param name="z">The opponent's zones.</param>
        /// <param name="theirTurn">Whose turn it is: on their turn the card's unused ignition effects count too.</param>
        public RemovalWorth WorthNow(RemovalTarget t, IZoneView z, bool theirTurn)
        {
            RemovalWorth r = new RemovalWorth();
            if (!t.Theirs) { r.Notes.Add("우리 카드"); return r; }
            if (!t.FaceUp)
            {
                r.Body = r.Value = _w.BaseOf("removal.facedown");
                r.Notes.Add("뒷면: 기대값");
                return r;
            }
            CardEntry c = _db.EnsureCard(t.Id, true);
            if (c == null) return r;
            if (t.Chained != null && LeavesAfterResolve(c.Data, t.Chained))
            {
                // Its activation resolves anyway and the card leaves the field after it: nothing to take, not a target.
                r.Notes.Add("발동 중인 일반·속공 마법/일반 함정: 처리 후 필드를 벗어남, 제거 대상 아님");
                return r;
            }
            {
                // A card that plays from the field (ignition / quick / interruption effects): what it can still use this turn plus
                // its body (removal.body), so a spent Detonator (no material left) is worth far less than an untouched one.
                // A card without such effects (continuous, normal monster): its static worth.
                bool plays = c.Effects.Any(e => e.Range != null && (e.Range.Contains("MZONE") || e.Range.Contains("SZONE") || e.Range.Contains("FZONE"))
                                              && (e.Kind == "기동 효과" || e.Kind.StartsWith("유발 즉시") || _im.IsInterrupt(c, e)));
                if (plays)
                {
                    List<string> left = new List<string>();
                    double residual = _loss.TurnResidual(c, z, _im, theirTurn, left);
                    r.Body = residual + _w.BaseOf("removal.body");
                    r.Notes.Add(left.Count > 0 ? "이번 턴 남은 효과 " + String.Join(", ", left.ToArray()) : "이번 턴 남은 효과 없음");
                    // A continuous / field / equip spell or trap stays on the field and keeps working on later turns (its
                    // activation, if chained now, resolves too): at least its card worth.
                    if ((c.Data.Type & 0x1) == 0 && StaticWorth != null)
                    {
                        double stat = StaticWorth(t.Id);
                        if (stat > r.Body) { r.Body = stat; r.Notes.Add("필드에 남는 마법·함정: 카드 가치 " + RemovalWorth.F(stat)); }
                    }
                }
                else if ((c.Data.Type & 0x1) != 0)
                {
                    // A monster with nothing to use from the field (a main deck Ryzeal after its search): its effects are spent or
                    // resolve anyway, so what is left is a body and, with a Level, Xyz material. Not its hand worth (a plan from hand).
                    bool material = c.Data.Level > 0 && (c.Data.Type & (0x40 | 0x2000 | 0x800000 | 0x4000000)) == 0;
                    r.Body = _w.BaseOf("removal.body") + (material ? _w.BaseOf("removal.material") : 0);
                    r.Notes.Add(material ? "필드에서 쓰는 효과 없음: 몸체 + 엑시즈 소재" : "필드에서 쓰는 효과 없음: 몸체");
                }
                else
                {
                    r.Body = StaticWorth != null ? Math.Max(0, StaticWorth(t.Id)) : 0;
                    r.Notes.Add("필드에서 쓰는 효과 없음: 카드 가치");
                }
                if (t.Chained != null) r.Notes.Add("체인 위 효과는 그대로 처리됨");
            }
            if (t.Chained != null && t.Chained.SourceStop != null)
            {
                double chain = _loss.Theirs.ValueOfActivating(t.Id, t.Chained, z).Value;
                r.Stop = t.Chained.SourceStop == "all" ? chain : chain * _w.BaseOf("removal.partial_stop");
                r.Notes.Add(t.Chained.SourceStop == "all" ? "카드가 없으면 효과 " + t.Chained.Index + " 처리 안 됨" : "효과 " + t.Chained.Index + " 일부만 멈춤");
            }
            r.Value = r.Body + r.Stop;
            return r;
        }

        /// <summary>The card is resolving a normal / quick-play spell or normal trap activation: it leaves the field anyway.</summary>
        public bool LeavesNow(RemovalTarget t)
        {
            if (t.Chained == null || t.Id == 0) return false;
            CardEntry c = _db.EnsureCard(t.Id, true);
            return c != null && LeavesAfterResolve(c.Data, t.Chained);
        }

        /// <summary>Can the removal (c, e) pick this card?</summary>
        public static bool CanReach(EffectEntry e, RemovalTarget t)
        {
            RemovalReach r = e.Reach;
            if (r == null) return false;
            if (r.ChainCard) return t.Theirs && t.Chained != null && t.LastLink;
            if (r.OpponentOnly && !t.Theirs) return false;
            if (r.FaceUpOnly && !t.FaceUp) return false;
            if (r.SpellTrapOnly && t.Monster) return false;
            if (r.MonsterOnly && !t.Monster) return false;
            return true;
        }

        /// <summary>
        /// A protection on the opponent's field stops this removal on t (docs/CHAIN_REMOVAL.md 2): "cannot be targeted" against a
        /// targeting removal, "cannot be destroyed" against a destroy; on t itself (single) or from another face-up card of theirs
        /// over the rest of their field (Number F0 ④). Returns the reason, or null when t can be taken.
        /// </summary>
        public string Shielded(EffectEntry e, RemovalTarget t, IList<RemovalTarget> all)
        {
            if (e.Reach == null || !t.Theirs) return null;
            foreach (RemovalTarget s in all)
            {
                if (!s.Theirs || !s.FaceUp || s.Id == 0) continue;
                CardEntry sc = _db.EnsureCard(s.Id, true);
                if (sc == null) continue;
                foreach (EffectEntry se in sc.Effects)
                {
                    CardShield sh = se.Shield;
                    if (sh == null) continue;
                    if (se.Range != null && !(s.Monster ? se.Range.Contains("MZONE") : se.Range.Contains("SZONE") || se.Range.Contains("FZONE"))) continue;
                    if (sh.What == "target" ? !e.Reach.Targets : !e.Reach.Destroys) continue;
                    bool self = ReferenceEquals(s, t) || (s.Tag != null && ReferenceEquals(s.Tag, t.Tag));
                    if (self ? sh.Self : sh.Others)
                        return sc.Data.Name + " 효과 " + se.Index + ": " + sh.Describe();
                }
            }
            return null;
        }

        /// <summary>
        /// t can answer this removal with an unused quick effect that makes itself indestructible (Number F0 ⑤): a destroy
        /// would do nothing. Returns that effect, or null.
        /// </summary>
        public EffectEntry SelfGuard(CardEntry c, EffectEntry e, RemovalTarget t, IZoneView z)
        {
            if (e != null && (e.Reach == null || !e.Reach.Destroys)) return null;
            if (!t.Theirs || !t.FaceUp || t.Id == 0) return null;
            CardEntry tc = _db.EnsureCard(t.Id, true);
            if (tc == null) return null;
            foreach (EffectEntry g in tc.Effects)
            {
                if (!g.GuardsSelf) continue;
                if (g.Range != null && !(t.Monster ? g.Range.Contains("MZONE") : g.Range.Contains("SZONE") || g.Range.Contains("FZONE"))) continue;
                KeyUsage u = _db.PeekUsage(1, g);
                if (u != null && g.Limit != null && u.Used >= g.Limit.Count) continue;
                int mats = t.Materials >= 0 ? t.Materials : z.FieldMaterials();
                if (g.DetachCount > mats) continue;
                if (c != null && e != null && g.Response != null && _db.CanHit(tc, g, c, e) == HitKind.None) continue;
                return g;
            }
            return null;
        }

        /// <summary>WorthNow with t's self-protecting response counted: a destroy it can dodge is worth removal.self_guard of it.</summary>
        public RemovalWorth WorthAgainst(CardEntry c, EffectEntry e, RemovalTarget t, IZoneView z, bool theirTurn, bool destroys)
        {
            RemovalWorth w = WorthNow(t, z, theirTurn);
            EffectEntry g = destroys ? SelfGuard(c, e, t, z) : null;
            if (g != null && w.Value > 0)
            {
                double f = _w.BaseOf("removal.self_guard");
                w.Value *= f;
                w.Notes.Add("효과 " + g.Index + "로 응답해 파괴 내성 (×" + RemovalWorth.F(f) + ")");
            }
            return w;
        }

        /// <summary>
        /// Fire now or keep: the best <paramref name="max"/> reachable targets' WorthNow against what the removal is worth kept
        /// for a later window (its interruption value × removal.hold, the cost a detach or a set card pays).
        /// </summary>
        /// <param name="usesLeft">
        /// Uses of this removal left on our field this turn for one paid by detaching (Detonator ①: materials / detach over our
        /// copies); -1 when not known or the removal is a single card. The fewer are left, the more a use is worth keeping for a
        /// bigger target later this turn: hold × max(1, removal.reserve / usesLeft).
        /// </param>
        public RemovalChoice Choose(CardEntry c, EffectEntry e, bool inHand, IEnumerable<RemovalTarget> targets, int max, IZoneView z, bool theirTurn, int usesLeft = -1)
        {
            RemovalChoice r = new RemovalChoice { Max = Math.Max(1, max) };
            r.Hold = _im.Value(c, e, inHand) * _w.BaseOf("removal.hold");
            if (e.DetachCount > 0 && usesLeft > 0)
            {
                double scarce = Math.Max(1, _w.BaseOf("removal.reserve") / usesLeft);
                r.Hold *= scarce;
                r.UsesLeft = usesLeft;
            }
            // A response that hits the answered card (Ghost Ogre) also needs that effect to be one it can answer (CanHit).
            IList<RemovalTarget> all = targets as IList<RemovalTarget> ?? targets.ToList();
            var scored = new List<KeyValuePair<RemovalTarget, RemovalWorth>>();
            foreach (RemovalTarget t in all)
            {
                if (!CanReach(e, t) || LeavesNow(t)) continue;
                if (e.Reach.ChainCard && _db.CanHit(c, e, _db.EnsureCard(t.Id, true), t.Chained) == HitKind.None) continue;
                string shield = Shielded(e, t, all);
                if (shield != null) { r.Blocked.Add((t.Id != 0 && _db.Get(t.Id) != null ? _db.Get(t.Id).Data.Name : "뒷면") + " (" + shield + ")"); continue; }
                RemovalWorth w = WorthAgainst(c, e, t, z, theirTurn, e.Reach.Destroys);
                if (w.Value > 0) scored.Add(new KeyValuePair<RemovalTarget, RemovalWorth>(t, w));
            }
            scored = scored.OrderByDescending(kv => kv.Value.Value).Take(r.Max).ToList();
            r.Picks.AddRange(scored);
            r.Gain = scored.Sum(kv => kv.Value.Value);
            return r;
        }
    }
}
