using System;
using System.Collections.Generic;
using WindBot.Game;

namespace WindBot.Agent.Knowledge
{
    /// <summary>How a monster is protected from battle destruction right now (from the Lua of the cards on its side).</summary>
    public enum GuardState { None, Replace, Indestructible }

    /// <summary>What the battle math needs from the rest of the agent.</summary>
    public sealed class BattleContext
    {
        public int OwnLp;
        public int OpponentLp;
        /// <summary>Value of the damage dealt for a full 8000 LP.</summary>
        public double LpValue = 100;
        /// <summary>Value of a monster leaving the field: base plus the interruptions it holds there.</summary>
        public Func<ClientCard, double> Worth = c => 30;
        /// <summary>Its protection from battle destruction, and what using a replacement costs its controller.</summary>
        public Func<ClientCard, KeyValuePair<GuardState, double>> Guard = c => new KeyValuePair<GuardState, double>(GuardState.None, 0);
    }

    /// <summary>
    /// Battle outcomes from the field: stats, the monsters' worth, and battle protection read from Lua.
    /// Not yet: stat changes during the damage step, piercing, damage prevention. 0 means "no attack".
    /// </summary>
    public static class BattleMath
    {
        public const double Lethal = 1e6;

        private static double Damage(int amount, BattleContext ctx) { return amount / 8000.0 * ctx.LpValue; }

        /// <summary>What the controller loses when this monster is destroyed by battle (nothing when it cannot be).</summary>
        private static double Loss(ClientCard c, BattleContext ctx)
        {
            KeyValuePair<GuardState, double> g = ctx.Guard(c);
            if (g.Key == GuardState.Indestructible) return 0;
            if (g.Key == GuardState.Replace) return g.Value;
            return ctx.Worth(c);
        }

        /// <summary>Outcome of this attacker attacking target, or a direct attack when target is null.</summary>
        public static double Score(ClientCard attacker, ClientCard target, BattleContext ctx)
        {
            int atk = attacker.Attack;
            if (target == null) return atk >= ctx.OpponentLp ? Lethal : Damage(atk, ctx);
            if (target.IsFacedown() && target.IsDefense())
                return atk >= 2000 ? ctx.Worth(target) * 0.5 : -1; // unknown DEF: worth trying only with a big attacker
            if (target.IsAttack())
            {
                int t = target.Attack;
                if (atk > t) return atk - t >= ctx.OpponentLp ? Lethal : Loss(target, ctx) + Damage(atk - t, ctx);
                if (atk == t) return Loss(target, ctx) - Loss(attacker, ctx);
                return t - atk >= ctx.OwnLp ? -Lethal : -Loss(attacker, ctx) - Damage(t - atk, ctx);
            }
            int def = target.Defense;
            if (atk > def) return Loss(target, ctx);
            if (atk == def) return -1;
            return def - atk >= ctx.OwnLp ? -Lethal : -Damage(def - atk, ctx);
        }

        /// <summary>Best outcome of this attacker over the possible targets (direct attack when allowed).</summary>
        public static double Best(ClientCard attacker, IList<ClientCard> targets, BattleContext ctx, out ClientCard bestTarget)
        {
            bestTarget = null;
            double best = double.NegativeInfinity;
            bool any = false;
            foreach (ClientCard t in targets)
            {
                if (t == null) continue;
                any = true;
                double s = Score(attacker, t, ctx);
                if (s > best) { best = s; bestTarget = t; }
            }
            if (!any || attacker.CanDirectAttack)
            {
                double s = Score(attacker, null, ctx);
                if (s > best) { best = s; bestTarget = null; }
            }
            return best;
        }

        /// <summary>Position a monster with these printed stats should take: attack unless its DEF is higher ("?" counts as attack).</summary>
        public static bool PreferAttack(int atk, int def)
        {
            return atk < 0 || atk >= def;
        }
    }
}
