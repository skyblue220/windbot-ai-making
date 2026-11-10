using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindBot.Agent.Knowledge
{
    /// <summary>What hitting one opponent effect costs them (docs/INTERRUPTION_RISK.md 8.1).</summary>
    public sealed class HitLoss
    {
        /// <summary>What the effect would still bring if it resolves: its chain value on the opponent's side.</summary>
        public double Chain;
        /// <summary>Expected best start they still have from their unknown hand once this effect is gone.</summary>
        public double Restart;
        /// <summary>Loss we inflict: Chain − Restart for a negation, the body for a hit that lets the effect resolve.</summary>
        public double Loss;
        public HitKind Kind;
        public ChainResult Path = new ChainResult();
        public readonly List<string> Notes = new List<string>();

        public string Describe()
        {
            string s = "상대 손실 " + F(Loss);
            if (Kind == HitKind.Disrupt) s += " (효과는 처리됨: 몸체만)";
            else s += " (연쇄 " + F(Chain) + " − 재시작 " + F(Restart) + ")";
            if (Notes.Count > 0) s += " [" + String.Join("; ", Notes.ToArray()) + "]";
            return s;
        }

        private static string F(double v) { return v.ToString("0.#", CultureInfo.InvariantCulture); }
    }

    /// <summary>
    /// Loss we inflict by negating (or destroying) the effect the opponent is activating now. A negated effect is gone for
    /// the rest of the tree: nearly every Ryzeal effect has a "use" once-per-turn limit (spent even when negated), so the
    /// whole chain it starts is lost. What they keep is the best start among their unknown hand cards (hypergeometric over
    /// the hidden pool), which is why hitting a starter is worth less the more cards they hold.
    /// Exceptions kept for other themes (not separated yet):
    ///  - a hit that lets the effect resolve (Ghost Ogre: destroy) costs the body only;
    ///  - an "activate" (OATH) limit is refunded when the activation itself is negated (NegateActivation), not by an effect
    ///    negation (NegateEffect, Ash): our response reading does not tell the two apart yet;
    ///  - an effect without a once-per-turn limit can be used again: the loss is that one use.
    /// </summary>
    public sealed class OpponentLossModel
    {
        private readonly GameCardDb _db;
        private readonly ValueWeights _w;
        private readonly ValueModel _theirs;

        public OpponentLossModel(GameCardDb db, ValueWeights w)
        {
            _db = db;
            _w = w;
            _theirs = new ValueModel(db, w, 1);
        }

        public ValueModel Theirs { get { return _theirs; } }

        /// <param name="z">The opponent's zones (their hand = revealed cards only).</param>
        /// <param name="pool">Their hidden pool and unknown hand size.</param>
        public HitLoss Loss(CardEntry card, EffectEntry e, HitKind kind, IZoneView z, OpponentPool pool)
        {
            HitLoss r = new HitLoss { Kind = kind };
            if (card == null || e == null || kind == HitKind.None) return r;
            r.Path = _theirs.ValueOfActivating(card.Data.Id, e, z);
            r.Chain = r.Path.Value;
            if (kind == HitKind.Disrupt)
            {
                r.Loss = _w.BaseOf("hit.disrupt_body");
                return r;
            }
            r.Restart = ExpectedRestart(e, z, pool, r.Notes);
            r.Loss = Math.Max(0, r.Chain - r.Restart);
            if (e.Limit == null) r.Notes.Add("1턴 횟수 제한 없음: 다시 쓸 수 있음(미반영)");
            else if (e.Limit.Oath) r.Notes.Add("'발동' 제약: 발동 무효면 다시 발동 가능(미구분)");
            return r;
        }

        /// <summary>
        /// E[max value of a card in their unknown hand], each card valued as a start now with the hit effect's key spent.
        /// Sorted by value v1 ≥ v2 ≥ ...: P(best is group i) = P(no copy of groups 1..i−1) − P(no copy of groups 1..i),
        /// P(no copy of m cards) = C(N−m, h) / C(N, h) with N hidden cards and h unknown hand cards.
        /// </summary>
        public double ExpectedRestart(EffectEntry hit, IZoneView z, OpponentPool pool, List<string> notes)
        {
            int h = pool.Hand;
            if (h <= 0) return 0;
            string[] spent = { hit.UsageKey };
            List<KeyValuePair<double, int>> groups = new List<KeyValuePair<double, int>>();
            int known = 0;
            foreach (int id in _db.OpponentMainDeck.Distinct())
            {
                int hidden = _db.OpponentMainDeck.Count(x => x == id) - pool.SeenOf(id);
                if (hidden <= 0) continue;
                known += hidden;
                double v = _theirs.ValueInHand(id, z, spent).Value;
                if (v > 0) groups.Add(new KeyValuePair<double, int>(v, hidden));
            }
            int n = Math.Max(pool.Hidden, known);
            if (n <= 0) return 0;
            double expect = 0, noneBefore = 1;
            int m = 0;
            string top = null;
            foreach (var g in groups.OrderByDescending(x => x.Key))
            {
                m += g.Value;
                double none = NoneOf(n, m, h);
                expect += g.Key * (noneBefore - none);
                noneBefore = none;
                if (top == null) top = g.Key.ToString("0.#", CultureInfo.InvariantCulture);
            }
            if (notes != null) notes.Add("손패 " + h + "장, 숨은 " + n + "장, 시작 가능 " + m + "장" + (top != null ? ", 최고 " + top : ""));
            return expect;
        }

        /// <summary>
        /// What negating the effects of one of their face-up cards until the End Phase takes from them (Aggregator ③ type,
        /// docs/GRAVE_PAYOFF.md 6): only the field effects it can still use this turn. Such a negation comes late (a GY trigger
        /// fires after the chain that sent it), so the effect being activated now is already spent and counts 0.
        ///  - Their turn: unused ignition / quick effects valued as their plays (side-1 chain value, e.g. Duo Drive ③ search)
        ///    or as interruptions they hold (Detonator ①), whichever is more.
        ///  - Our turn: only the interruptions they hold against us (quick effects, destroy replacement).
        /// Past triggers (summon triggers) and continuous effects count 0 (not read yet).
        /// </summary>
        /// <param name="z">The opponent's zones.</param>
        public double TurnResidual(CardEntry c, IZoneView z, InterruptModel im, bool theirTurn, List<string> notes = null)
        {
            if (c == null) return 0;
            double sum = 0;
            int fieldMaterials = z.FieldMaterials(); // a detach cost needs them
            foreach (EffectEntry e in c.Effects)
            {
                if (e.Range == null || !(e.Range.Contains("MZONE") || e.Range.Contains("SZONE") || e.Range.Contains("FZONE"))) continue;
                bool ignition = e.Kind == "기동 효과", quick = e.Kind == "유발 즉시 효과", interrupt = im.IsInterrupt(c, e);
                if (!interrupt && !(theirTurn && (ignition || quick))) continue;
                KeyUsage u = _db.PeekUsage(1, e);
                if (u != null && e.Limit != null && u.Used >= e.Limit.Count) continue;
                if (e.DetachCount > 0 && fieldMaterials < e.DetachCount) continue;
                double v = interrupt ? im.Value(c, e, false) : 0;
                if (theirTurn && (ignition || quick)) v = Math.Max(v, _theirs.ValueOfEffect(c.Data.Id, e, z).Value);
                if (v <= 0) continue;
                sum += v;
                if (notes != null) notes.Add("효과 " + e.Index + " " + v.ToString("0.#", CultureInfo.InvariantCulture));
            }
            return sum;
        }

        /// <summary>P(no copy of m given cards among h drawn from n) = C(n−m, h) / C(n, h).</summary>
        public static double NoneOf(int n, int m, int h)
        {
            if (m <= 0) return 1;
            if (h > n - m) return 0;
            double p = 1;
            for (int i = 0; i < h; ++i) p *= (double)(n - m - i) / (n - i);
            return p;
        }
    }
}
