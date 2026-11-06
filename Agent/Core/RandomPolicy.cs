using System;
using System.Collections.Generic;

namespace WindBot.Agent.Core
{
    /// <summary>
    /// Uniformly random legal answers for every request shape. Used to prove the skeleton
    /// can answer every engine prompt, and later as a rollout policy for experiments.
    /// It never cancels when a non-empty answer is possible, to avoid activate/cancel loops.
    /// </summary>
    public sealed class RandomPolicy : IDecisionPolicy
    {
        private readonly Random _rand;

        public RandomPolicy(int seed)
        {
            _rand = new Random(seed);
        }

        public string Name { get { return "random"; } }

        public Decision Decide(DecisionRequest request, StateSnapshot state)
        {
            Decision decision = new Decision();
            int n = request.Options.Count;
            switch (request.Shape)
            {
                case RequestShape.One:
                    if (n == 0) { decision.Canceled = true; break; }
                    decision.Indices.Add(_rand.Next(n));
                    break;

                case RequestShape.Many:
                    {
                        int min = Math.Max(request.Min, 0);
                        int max = Math.Min(request.Max, n);
                        if (max <= 0 || min > max)
                        {
                            decision.Canceled = request.Cancelable;
                            break;
                        }
                        int low = Math.Max(min, 1);
                        int count = low > max ? max : low + _rand.Next(max - low + 1);
                        foreach (int i in Shuffled(n))
                        {
                            if (decision.Indices.Count >= count) break;
                            decision.Indices.Add(i);
                        }
                        break;
                    }

                case RequestShape.Sum:
                    {
                        List<int> found = SolveSum(request);
                        if (found == null) decision.Canceled = true;
                        else decision.Indices.AddRange(found);
                        break;
                    }

                case RequestShape.Counter:
                    {
                        int[] counts = new int[n];
                        int remaining = request.Quantity;
                        int guard = 0;
                        while (remaining > 0 && guard++ < 10000)
                        {
                            List<int> open = new List<int>();
                            for (int i = 0; i < n; ++i)
                                if (counts[i] < request.Options[i].Param1) open.Add(i);
                            if (open.Count == 0) break;
                            counts[open[_rand.Next(open.Count)]]++;
                            remaining--;
                        }
                        decision.Indices.AddRange(counts);
                        break;
                    }

                case RequestShape.Order:
                    decision.Indices.AddRange(Shuffled(n));
                    break;
            }
            return decision;
        }

        private List<int> Shuffled(int n)
        {
            List<int> order = new List<int>(n);
            for (int i = 0; i < n; ++i) order.Add(i);
            for (int i = n - 1; i > 0; --i)
            {
                int j = _rand.Next(i + 1);
                int t = order[i]; order[i] = order[j]; order[j] = t;
            }
            return order;
        }

        // Random-order depth-first search for a subset whose sum meets the target.
        // Value of an option: Param1, or Param2 as the alternative value when it is non-zero.
        private List<int> SolveSum(DecisionRequest request)
        {
            int n = request.Options.Count;
            int min = Math.Max(request.Min, 0);
            int max = Math.Min(request.Max <= 0 ? n : request.Max, n);
            if (request.Target <= 0 && min == 0) return new List<int>();

            List<int> order = Shuffled(n);
            List<int> picked = new List<int>();
            int budget = 200000;
            return Search(request, order, 0, picked, 0, 0, min, max, ref budget) ? picked : null;
        }

        private static bool Search(DecisionRequest request, List<int> order, int start, List<int> picked,
            int s1, int s2, int min, int max, ref int budget)
        {
            if (--budget < 0) return false;
            if (picked.Count >= min && picked.Count > 0 && Meets(request, s1, s2)) return true;
            if (picked.Count >= max) return false;
            for (int k = start; k < order.Count; ++k)
            {
                DecisionOption o = request.Options[order[k]];
                int v1 = o.Param1;
                int v2 = o.Param2 != 0 ? o.Param2 : o.Param1;
                if (request.ExactSum && s1 + v1 > request.Target && s2 + v2 > request.Target) continue;
                picked.Add(order[k]);
                if (Search(request, order, k + 1, picked, s1 + v1, s2 + v2, min, max, ref budget)) return true;
                picked.RemoveAt(picked.Count - 1);
            }
            return false;
        }

        private static bool Meets(DecisionRequest request, int s1, int s2)
        {
            if (request.ExactSum) return s1 == request.Target || s2 == request.Target;
            return s1 >= request.Target || s2 >= request.Target;
        }
    }
}
