using System;
using System.Collections.Generic;
using WindBot.Game;

namespace WindBot.Agent.Knowledge
{
    /// <summary>
    /// Battle outcomes from the stats on the field (no card effects yet: protection, piercing, stat changes in the damage step).
    /// Scores only compare battle options with each other; 0 is "no attack".
    /// </summary>
    public static class BattleMath
    {
        /// <summary>Destroying a monster by battle, before the damage it deals.</summary>
        public const double DestroyValue = 1000;
        /// <summary>Losing our attacker.</summary>
        public const double LoseValue = 1000;
        /// <summary>An attack whose damage would take our own LP to 0.</summary>
        public const double SelfKill = -1e6;

        /// <summary>Outcome of <paramref name="attacker"/> (ATK <paramref name="atk"/>) attacking <paramref name="target"/>, or a direct attack when null.</summary>
        public static double Score(int atk, ClientCard target, int ownLp, int opponentLp)
        {
            if (target == null) return atk >= opponentLp ? -SelfKill : atk;
            if (target.IsFacedown() && target.IsDefense())
            {
                // Unknown DEF: worth trying only with a big attacker; the flip itself may hurt.
                return atk >= 2000 ? DestroyValue * 0.5 : -1;
            }
            if (target.IsAttack())
            {
                int t = target.Attack;
                if (atk > t)
                {
                    int dmg = atk - t;
                    return dmg >= opponentLp ? -SelfKill : DestroyValue + dmg;
                }
                if (atk == t) return DestroyValue - LoseValue; // both destroyed: an even trade
                int recoil = t - atk;
                return recoil >= ownLp ? SelfKill : -LoseValue - recoil;
            }
            int def = target.Defense;
            if (atk > def) return DestroyValue;
            if (atk == def) return -1; // nothing happens
            int back = def - atk;
            return back >= ownLp ? SelfKill : -back;
        }

        /// <summary>Best outcome of this attacker over the possible targets (direct attack when allowed).</summary>
        public static double Best(ClientCard attacker, IList<ClientCard> targets, int ownLp, int opponentLp, out ClientCard bestTarget)
        {
            bestTarget = null;
            double best = double.NegativeInfinity;
            bool any = false;
            foreach (ClientCard t in targets)
            {
                if (t == null) continue;
                any = true;
                double s = Score(attacker.Attack, t, ownLp, opponentLp);
                if (s > best) { best = s; bestTarget = t; }
            }
            if (!any || attacker.CanDirectAttack)
            {
                double s = Score(attacker.Attack, null, ownLp, opponentLp);
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
