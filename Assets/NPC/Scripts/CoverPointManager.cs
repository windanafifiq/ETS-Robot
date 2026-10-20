using UnityEngine;
using RobotArena; 

namespace RobotArena.Environment
{
    // Pasang SATU kali di GameObject kosong "CoverPointManager".
    public class CoverPointManager : MonoBehaviour
    {
        public static CoverPointManager Instance { get; private set; }

        [Tooltip("Layer tembok/pilar/crate yang menghalangi pandangan (isi: Obstacle)")]
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float maxCoverDistance = 25f;

        private CoverPoint[] allCoverPoints;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            allCoverPoints = FindObjectsByType<CoverPoint>(FindObjectsInactive.Exclude);
        }

        // Mencari cover terdekat yang BENAR-BENAR menghalangi garis pandang ke 'threatPosition'.
        // 'requester' = robot yang bertanya (cover miliknya sendiri tetap dianggap tersedia).
        public CoverPoint FindNearestAvailableCover(Vector3 robotPosition, Vector3 threatPosition, Transform requester)
        {
            if (allCoverPoints == null) return null;

            CoverPoint best = null;
            float bestDistance = Mathf.Infinity;

            foreach (CoverPoint cp in allCoverPoints)
            {
                if (cp == null) continue;
                if (cp.IsOccupied && !cp.IsOccupiedBy(requester)) continue;

                float dist = Vector3.Distance(robotPosition, cp.transform.position);
                if (dist > maxCoverDistance || dist >= bestDistance) continue;

                Vector3 origin = cp.transform.position + Vector3.up;
                Vector3 toThreat = (threatPosition + Vector3.up) - origin;

                bool hidden = Physics.Raycast(origin, toThreat.normalized, toThreat.magnitude, obstacleLayer);
                if (!hidden) continue;

                bestDistance = dist;
                best = cp;
            }

            return best;
        }
    }
}