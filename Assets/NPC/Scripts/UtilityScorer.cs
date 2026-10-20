using UnityEngine;
using RobotArena; 

namespace RobotArena.Enemy
{
    public enum UtilityAction
    {
        Attack,
        MaintainDistance,
        TakeCover,
        Retreat
    }

    // Class C# biasa (BUKAN MonoBehaviour). Dipakai dari dalam RobotFSM.
    // Tugasnya: menghitung skor tiap aksi lalu memilih yang tertinggi (Utility AI).
    public class UtilityScorer
    {
        private readonly RobotUtilityWeights weights;

        public UtilityScorer(RobotUtilityWeights weights)
        {
            this.weights = weights;
        }

        public UtilityAction ChooseBestAction(
            float distanceToPlayer, float hpPercent,
            bool wasDamagedRecently, bool coverAvailable, bool weaponReady)
        {
            var s = GetScores(distanceToPlayer, hpPercent, wasDamagedRecently, coverAvailable, weaponReady);

            UtilityAction best = UtilityAction.Attack;
            float bestScore = s.attack;

            if (s.distance > bestScore) { best = UtilityAction.MaintainDistance; bestScore = s.distance; }
            if (s.cover > bestScore)    { best = UtilityAction.TakeCover;        bestScore = s.cover; }
            if (s.retreat > bestScore)  { best = UtilityAction.Retreat;          bestScore = s.retreat; }

            return best;
        }

        // skor akhir = skor mentah x bobot role
        public (float attack, float distance, float cover, float retreat) GetScores(
            float distanceToPlayer, float hpPercent,
            bool wasDamagedRecently, bool coverAvailable, bool weaponReady)
        {
            return (
                ScoreAttack(distanceToPlayer, weaponReady) * weights.attackWeight,
                ScoreMaintainDistance(distanceToPlayer) * weights.maintainDistanceWeight,
                ScoreTakeCover(wasDamagedRecently, coverAvailable) * weights.coverWeight,
                ScoreRetreat(hpPercent) * weights.retreatWeight
            );
        }

        private float ScoreAttack(float distance, bool weaponReady)
        {
            if (!weaponReady) return 0f;
            float closeness = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, weights.idealCombatDistance));
            return Mathf.Clamp01(0.4f + closeness * 0.6f);
        }

        private float ScoreMaintainDistance(float distance)
        {
            if (distance >= weights.idealCombatDistance) return 0f;
            return 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, weights.idealCombatDistance));
        }

        private float ScoreTakeCover(bool wasDamagedRecently, bool coverAvailable)
        {
            if (!coverAvailable) return 0f;
            return wasDamagedRecently ? 0.8f : 0.2f;
        }

        private float ScoreRetreat(float hpPercent)
        {
            if (hpPercent > weights.retreatHPThreshold) return 0f;
            return 1f - (hpPercent / weights.retreatHPThreshold);
        }
    }
}