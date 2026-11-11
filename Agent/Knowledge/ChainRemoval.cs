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
        public readonly List<KeyValuePair<RemovalTarget, RemovalWorth>> Picks = new List<KeyValuePair<RemovalTarget, RemovalWorth>>();
        public bool Fire { get { return Gain > Hold; } }

        public string Describe()
        {
            return (Fire ? "발동" : "보류") + ": 얻는 값 " + RemovalWorth.F(Gain) + " vs 아껴 둘 값 " + RemovalWorth.F(Hold)
                 + (Picks.Count > 0 ? " (최대 " + Max + "장)" : " (칠 카드 없음)");
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
                r.Notes.Add("발동 중인 일반 마법·함정: 처리 후 어차피 묘지로");
            }
            else
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
        /// Fire now or keep: the best <paramref name="max"/> reachable targets' WorthNow against what the removal is worth kept
        /// for a later window (its interruption value × removal.hold, the cost a detach or a set card pays).
        /// </summary>
        public RemovalChoice Choose(CardEntry c, EffectEntry e, bool inHand, IEnumerable<RemovalTarget> targets, int max, IZoneView z, bool theirTurn)
        {
            RemovalChoice r = new RemovalChoice { Max = Math.Max(1, max) };
            r.Hold = _im.Value(c, e, inHand) * _w.BaseOf("removal.hold");
            // A response that hits the answered card (Ghost Ogre) also needs that effect to be one it can answer (CanHit).
            var scored = targets.Where(t => CanReach(e, t) && (!e.Reach.ChainCard || _db.CanHit(c, e, _db.EnsureCard(t.Id, true), t.Chained) != HitKind.None)).Select(t => new KeyValuePair<RemovalTarget, RemovalWorth>(t, WorthNow(t, z, theirTurn)))
                                .Where(kv => kv.Value.Value > 0).OrderByDescending(kv => kv.Value.Value).Take(r.Max).ToList();
            r.Picks.AddRange(scored);
            r.Gain = scored.Sum(kv => kv.Value.Value);
            return r;
        }
    }
}
