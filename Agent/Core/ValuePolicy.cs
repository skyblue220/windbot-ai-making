using System;
using System.Collections.Generic;
using System.Linq;

namespace WindBot.Agent.Core
{
    /// <summary>
    /// Picks by the option estimates the bridge attached (plan value, card worth, interruption value).
    /// One: highest estimate (lowest when PreferLow). Many: the best Min (at least 1) by estimate.
    /// Options without an estimate are not chosen while any option has one; with no estimates at all,
    /// or with probability Explore, it falls back to a uniformly random legal answer so that
    /// the logs also contain outcomes of actions the current values would not pick.
    /// </summary>
    public sealed class ValuePolicy : IDecisionPolicy
    {
        private readonly Random _rand;
        private readonly RandomPolicy _random;

        public double Explore = 0.1;

        public ValuePolicy(int seed)
        {
            _rand = new Random(seed);
            _random = new RandomPolicy(seed ^ 0x5f3759df);
        }

        public string Name { get { return "value(explore=" + Explore + ")"; } }

        /// <summary>True when the last decision was a random exploration step.</summary>
        public bool LastExplored { get; private set; }

        public Decision Decide(DecisionRequest request, StateSnapshot state)
        {
            LastExplored = false;
            List<int> scored = Enumerable.Range(0, request.Options.Count).Where(i => !double.IsNaN(request.Options[i].Estimate)).ToList();
            bool canScore = scored.Count > 0 && (request.Shape == RequestShape.One || request.Shape == RequestShape.Many);
            if (!canScore || (!request.NoExplore && _rand.NextDouble() < Explore))
            {
                LastExplored = canScore;
                return _random.Decide(request, state);
            }

            Decision d = new Decision();
            if (request.CancelIfNotPositive && request.Cancelable && scored.All(i => request.Options[i].Estimate <= 0))
            {
                d.Canceled = true;
                return d;
            }
            Func<int, double> key = i => request.PreferLow ? -request.Options[i].Estimate : request.Options[i].Estimate;
            if (request.Shape == RequestShape.One)
            {
                double best = scored.Max(key);
                List<int> ties = scored.Where(i => key(i) >= best - 1e-9).ToList();
                d.Indices.Add(ties[_rand.Next(ties.Count)]);
                return d;
            }

            // Many: estimated options first (best first), then the rest in random order.
            // Min 0 means "may stop" only in SelectUnselect (enough already selected); elsewhere canceling aborts the action (e.g. a summon cost).
            int n = request.Options.Count;
            if (request.Min == 0 && request.Cancelable && request.Kind == RequestKind.SelectUnselect)
            {
                // Enough is already selected (e.g. Xyz materials after the minimum): stop when giving up cards,
                // otherwise take one more only if it is worth something.
                int top = scored.OrderByDescending(key).First();
                if (request.PreferLow || request.Options[top].Estimate <= 0) { d.Canceled = true; return d; }
                d.Indices.Add(top);
                return d;
            }
            int count = Math.Min(Math.Max(request.Min, 1), Math.Min(request.Max, n));
            if (count <= 0) { d.Canceled = request.Cancelable; return d; }
            List<int> order = scored.OrderByDescending(key).ToList();
            order.AddRange(Enumerable.Range(0, n).Where(i => !scored.Contains(i)).OrderBy(i => _rand.Next()));
            d.Indices.AddRange(order.Take(count));
            return d;
        }
    }
}
