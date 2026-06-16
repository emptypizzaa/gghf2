using UnityEngine;

namespace PaperHeroes
{
    /// <summary>Single-lane targeting helpers — all distance is 1D along world X.</summary>
    public static class Targeting
    {
        /// <summary>Nearest living enemy Unit within <paramref name="range"/> on X, or null.</summary>
        public static Unit FindNearestEnemyInRange(float x, Team myTeam, float range)
        {
            Unit best = null;
            float bestDist = float.MaxValue;
            var all = Unit.All;
            for (int i = 0; i < all.Count; i++)
            {
                var u = all[i];
                if (u == null || u.IsDead || u.Team == myTeam) continue;
                float d = Mathf.Abs(u.transform.position.x - x);
                if (d <= range && d < bestDist)
                {
                    bestDist = d;
                    best = u;
                }
            }
            return best;
        }
    }
}
