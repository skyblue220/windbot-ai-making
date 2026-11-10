using System;
using System.Collections.Generic;
using System.Linq;

namespace WindBot.Agent.Knowledge
{
    /// <summary>When a GY payoff fires, relative to the turn being planned (docs/GRAVE_PAYOFF.md).</summary>
    public enum PayoffTiming
    {
        /// <summary>This turn, while we plan: needs its once-per-turn key unused this turn.</summary>
        Now,
        /// <summary>On the opponent's turn: the once-per-turn key is fresh again, but it fires late (after they act).</summary>
        OpponentTurn,
        /// <summary>On our next turn: fresh key again, but the board may be gone by then.</summary>
        NextTurn
    }

    /// <summary>How the card reaches the GY. Xyz material detach is the first channel the planner uses;
    /// the others are the same payoff delivered another way and plug in later.</summary>
    public enum PayoffChannel
    {
        /// <summary>Detached Xyz material (our own detach costs, or Detonator ③ when it would be destroyed).</summary>
        Detach,
        /// <summary>Sent as a cost from hand, field or Extra Deck (Ext's Extra Deck cost, discards).</summary>
        Cost,
        /// <summary>Destroyed on the field (battle, the opponent's removal).</summary>
        Destroyed,
        /// <summary>Sent by an effect (mill, tribute, leaving the field otherwise).</summary>
        Effect
    }

    /// <summary>One "sent to the GY" trigger a card has: what fires and under which once-per-turn key.</summary>
    public sealed class GravePayoff
    {
        public int CardId;
        public EffectEntry Effect;
        /// <summary>The once-per-turn key (null: no limit, usable any number of times).</summary>
        public string Key;
        /// <summary>Once per Duel: never fresh again on a later turn.</summary>
        public bool PerDuel;
        /// <summary>Its effect targets or affects an opponent card: worthless while they have none (our own turn, empty board).</summary>
        public bool HitsOpponent;
    }

    /// <summary>
    /// Reads every card's "sent to the GY" payoffs from the card DB, independent of how the card gets there.
    /// The OPT rule is the core of the idea: a payoff used this turn is fresh again on the opponent's turn and on our next,
    /// so the same card can pay twice if it is delivered again later (e.g. a material detached on their turn).
    /// Not used in decisions yet: the planner keeps GraveBonus until the keep/now choice lands.
    /// </summary>
    public sealed class GravePayoffReader
    {
        private readonly GameCardDb _db;
        private readonly ValueWeights _w;
        private readonly Func<CardEntry, EffectEntry, bool> _hits;
        private readonly Dictionary<int, List<GravePayoff>> _cache = new Dictionary<int, List<GravePayoff>>();

        /// <param name="hitsOpponent">Whether an effect hits field cards (the planner's HitsFieldCards); default: any opponent link.</param>
        public GravePayoffReader(GameCardDb db, ValueWeights w, Func<CardEntry, EffectEntry, bool> hitsOpponent = null) { _db = db; _w = w; _hits = hitsOpponent; }

        /// <summary>The card's GY payoffs (empty if it has none).</summary>
        public IList<GravePayoff> Of(int cardId)
        {
            List<GravePayoff> list;
            if (_cache.TryGetValue(cardId, out list)) return list;
            list = new List<GravePayoff>();
            CardEntry c = _db.Get(cardId);
            if (c != null)
                foreach (EffectEntry e in c.Effects)
                {
                    if (!e.Kind.StartsWith("유발 효과") || !e.Events.Any(ev => ev.Contains("TO_GRAVE"))) continue;
                    list.Add(new GravePayoff
                    {
                        CardId = cardId,
                        Effect = e,
                        Key = e.Limit != null ? e.UsageKey : null,
                        PerDuel = e.Limit != null && e.Limit.PerDuel,
                        HitsOpponent = _hits != null ? _hits(c, e) : e.Links.Any(l => l.Role == "effect" && l.Opponent)
                    });
                }
            _cache[cardId] = list;
            return list;
        }

        public bool HasPayoff(int cardId) { return Of(cardId).Count > 0; }

        /// <summary>Whether the payoff can still fire at that timing. Once-per-turn keys spent this turn only block Now;
        /// the opponent's turn and our next turn start fresh. Once per Duel blocks every later use.</summary>
        public static bool Available(GravePayoff p, PayoffTiming t, ICollection<string> usedThisTurn, ICollection<string> usedThisDuel = null)
        {
            if (p.Key == null) return true;
            if (p.PerDuel && usedThisDuel != null && usedThisDuel.Contains(p.Key)) return false;
            return t != PayoffTiming.Now || usedThisTurn == null || !usedThisTurn.Contains(p.Key);
        }

        /// <summary>How much a payoff is worth at that timing, relative to firing it now (learnable: gy_payoff.*).</summary>
        public double TimingWeight(PayoffTiming t)
        {
            switch (t)
            {
                case PayoffTiming.Now: return _w.BaseOf("gy_payoff.now");
                case PayoffTiming.OpponentTurn: return _w.BaseOf("gy_payoff.opp_turn");
                default: return _w.BaseOf("gy_payoff.next_turn");
            }
        }

        /// <summary>The best payoff value at a timing: raw value (caller's model: interruption or function value) × timing weight.
        /// A payoff hitting the opponent's cards is worth 0 now while they have no face-up card.</summary>
        public double Value(int cardId, PayoffTiming t, Func<GravePayoff, double> raw, ICollection<string> usedThisTurn, int opponentFaceUp = -1)
        {
            double best = 0;
            foreach (GravePayoff p in Of(cardId))
            {
                if (!Available(p, t, usedThisTurn)) continue;
                if (t == PayoffTiming.Now && p.HitsOpponent && opponentFaceUp == 0) continue;
                best = Math.Max(best, raw(p) * TimingWeight(t));
            }
            return best;
        }
    }
}
