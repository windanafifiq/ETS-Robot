using UnityEngine;
using UnityEngine.AI;
using RobotArena; 

namespace RobotArena.Enemy
{
    // Pembantu kecil agar NPC tidak "macet" saat tujuan jatuh di luar NavMesh
    // (misal: tujuan mundur ternyata di dalam tembok).
    public static class NavUtil
    {
        public static bool TryMoveTo(NavMeshAgent agent, Vector3 worldPosition, float maxSnapDistance = 3f)
        {
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;

            if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, maxSnapDistance, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                return agent.SetDestination(hit.position);
            }

            return false;
        }
    }
}