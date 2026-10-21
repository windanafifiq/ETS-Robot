
using UnityEngine;

namespace RobotArena.Enemy
{
    public enum SniperAction
    {
        Shoot,
        TrackTarget,
        DefensiveHold
    }

    // Utility AI khusus Sniper yang menjaga tower.
    // Tidak mengatur pergerakan NavMeshAgent.
    public class SniperUtilityScorer
    {
        public struct Scores
        {
            public float shoot;
            public float track;
            public float defensive;
        }

        public Scores GetScores(
            bool playerVisible,
            bool playerInZone,
            bool hasMemory,
            bool weaponReady,
            float hpPercent)
        {
            Scores scores = new Scores();

            // Menembak hanya jika semua kondisi terpenuhi.
            scores.shoot =
                playerVisible && playerInZone && weaponReady
                ? 1.0f
                : 0f;

            // Mengawasi posisi terakhir Player.
            scores.track =
                hasMemory && playerInZone && !playerVisible
                ? 0.8f
                : 0f;

            // Mempertahankan posisi jika HP rendah.
            scores.defensive =
                hpPercent <= 0.3f
                ? 0.9f
                : 0.1f;

            return scores;
        }

        public SniperAction ChooseBestAction(Scores scores)
        {
            SniperAction best = SniperAction.DefensiveHold;
            float bestScore = scores.defensive;

            if (scores.track > bestScore)
            {
                best = SniperAction.TrackTarget;
                bestScore = scores.track;
            }

            if (scores.shoot > bestScore)
            {
                best = SniperAction.Shoot;
            }

            return best;
        }
    }
}