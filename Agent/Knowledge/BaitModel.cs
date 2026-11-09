using System;
using System.Collections.Generic;
using System.Linq;

namespace WindBot.Agent.Knowledge
{
    /// <summary>
    /// What we know of the opponent's cards right now: copies we have seen (field, GY, banished), the hidden pool
    /// (deck + hand + face-down cards), how many of those they can use right now (hand + set cards), and the face-up board.
    /// </summary>
    public sealed class OpponentPool
    {
        /// <summary>Cards we cannot see: deck + hand + face-down cards on the field.</summary>
        public int Hidden;
        /// <summary>Unknown cards in their hand.</summary>
        public int Hand;
        /// <summary>Their face-down spells/traps.</summary>
        public int Set;
        /// <summary>Their field is empty (Impermanence can be activated from the hand).</summary>
        public bool FieldEmpty;
        /// <summary>It is the opponent's turn: their quick-play spells work from the hand too.</summary>
        public bool TheirTurn;
        /// <summary>Visible copies per card id (field face-up, GY, banished).</summary>
        public readonly Dictionary<int, int> Seen = new Dictionary<int, int>();
        /// <summary>Face-up cards on their field with their Xyz material count.</summary>
        public readonly List<KeyValuePair<int, int>> Board = new List<KeyValuePair<int, int>>();
        /// <summary>True when the opponent has used up this effect's per-turn limit.</summary>
        public Func<EffectEntry, bool> Exhausted = e => false;

        public int SeenOf(int id) { int n; return Seen.TryGetValue(id, out n) ? n : 0; }
    }

    /// <summary>
    /// Bait ordering seed (VALUE_MODEL.md 7.5): when two or more of our interruptions can answer the same play,
    /// fire first the one the opponent is likely to stop and that is worth less; keep the one that is hard to stop or worth more.
    /// Stop chance = the opponent holds (or shows on board) at least one card whose response can negate that effect.
    /// </summary>
    public sealed class BaitModel
    {
        private readonly GameCardDb _db;
        private readonly ValueWeights _w;

        public BaitModel(GameCardDb db, ValueWeights w) { _db = db; _w = w; }

        /// <summary>P(at least one of k marked cards among d drawn from n), drawn without replacement.</summary>
        public static double AtLeastOne(int n, int k, int d)
        {
            if (k <= 0 || d <= 0 || n <= 0) return 0;
            if (k >= n) return 1;
            double none = 1;
            for (int t = 0; t < d; ++t)
            {
                if (n - k - t <= 0) return 1;
                none *= (double)(n - k - t) / (n - t);
            }
            return Math.Max(0, Math.Min(1, 1 - none));
        }

        /// <summary>
        /// Chance the opponent can negate every one of these effects with a single card (one effect: its stop chance;
        /// two: the chance one card of theirs answers both, which is what firing the first one draws out).
        /// </summary>
        public double StopChance(OpponentPool pool, params KeyValuePair<CardEntry, EffectEntry>[] ours)
        {
            if (pool == null || ours.Length == 0) return 0;
            int fromHand = 0, fromSet = 0;
            bool board = false;
            foreach (CardEntry o in _db.Cards.Values)
            {
                if (o.OpponentCopies <= 0) continue;
                EffectEntry hit = o.Effects.FirstOrDefault(oe =>
                    !pool.Exhausted(oe) && ours.All(x => _db.CanHit(o, oe, x.Key, x.Value) == HitKind.Negate));
                if (hit == null) continue;

                // On their field face-up: it is there (with the material it needs).
                foreach (var b in pool.Board)
                    if (b.Key == o.Data.Id && (hit.DetachCount == 0 || b.Value >= hit.DetachCount)
                        && hit.Range != null && (hit.Range.Contains("MZONE") || hit.Range.Contains("SZONE") || hit.Range.Contains("FZONE")))
                        board = true;

                // In the hidden pool: a Main Deck card used from the hand (hand trap) or from a set (trap / quick-play activation).
                // An Extra Deck card, or an effect that works only from a zone (Ryzeal Cross ③ in the field zone), is board-only.
                if (_db.IsExtraDeckCard(o.Data)) continue;
                int left = Math.Max(0, o.OpponentCopies - pool.SeenOf(o.Data.Id));
                bool handUse = (hit.Range != null && hit.Range.Contains("HAND")) || (o.TrapActInHand && hit.Kind == "카드의 발동" && pool.FieldEmpty);
                bool setUse = hit.Kind == "카드의 발동" && !_db.IsMonsterCard(o.Data);
                if (setUse && pool.TheirTurn && _db.IsQuickPlay(o.Data)) handUse = true;
                if (handUse) fromHand += left;
                else if (setUse) fromSet += left;
            }
            if (board) return 1;
            double none = (1 - AtLeastOne(pool.Hidden, fromHand, pool.Hand)) * (1 - AtLeastOne(pool.Hidden, fromSet, pool.Set));
            return Math.Max(0, Math.Min(1, (1 - none) * HoldFactor()));
        }

        /// <summary>Learnable scale on "they hold it" (bluffing, dead draws): bait.hold, 1 by default.</summary>
        private double HoldFactor() { return _w.BaseOf("bait.hold"); }

        /// <summary>
        /// Chain-response estimate for each interruption offered together (≥ 2):
        /// S + (S − V_i)(1 − p_i) + p_i · D_i, where S = the best value offered, V_i = this one's value, p_i = its stop chance,
        /// D_i = bait.draw_out × max over the others of V_j × P(one card of theirs stops both i and j).
        /// With nothing to fear it fires the cheapest first; with a likely negation it leads with the card worth losing.
        /// </summary>
        public List<double> Order(OpponentPool pool, List<KeyValuePair<CardEntry, EffectEntry>> fx, List<double> values, List<string> notes)
        {
            int n = fx.Count;
            List<double> p = fx.Select(x => StopChance(pool, x)).ToList();
            double s = values.Max();
            double drawOut = _w.BaseOf("bait.draw_out");
            List<double> r = new List<double>();
            for (int i = 0; i < n; ++i)
            {
                double d = 0;
                for (int j = 0; j < n; ++j)
                    if (j != i) d = Math.Max(d, values[j] * StopChance(pool, fx[i], fx[j]));
                d *= drawOut;
                r.Add(s + (s - values[i]) * (1 - p[i]) + p[i] * d);
                notes.Add("미끼: 막힐 확률 " + p[i].ToString("0.##") + ", 가치 " + values[i].ToString("0.#") + ", 끌어내기 " + d.ToString("0.#"));
            }
            return r;
        }
    }
}
