using UnityEngine;
using RobotArena; 

namespace RobotArena.Environment
{
    // Titik tempat NPC bersembunyi. Letakkan di BELAKANG pilar/tembok/crate.
    public class CoverPoint : MonoBehaviour
    {
        private Transform occupant;

        public bool IsOccupied => occupant != null;

        public bool IsOccupiedBy(Transform robot)
        {
            return occupant == robot;
        }

        public void Reserve(Transform robot)
        {
            occupant = robot;
        }

        public void Release(Transform robot)
        {
            if (occupant == robot) occupant = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.25f, new Vector3(0.6f, 0.5f, 0.6f));
        }
    }
}