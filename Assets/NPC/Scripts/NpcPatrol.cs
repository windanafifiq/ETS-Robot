using UnityEngine;
using UnityEngine.AI;

namespace RobotArena.Enemy
{
    // Membuat NPC berjalan berkeliling (patroli) saat tidak sedang bertarung,
    // jadi robot tidak berdiri diam seperti patung.
    // Dipakai oleh RobotFSM (state Idle) dan MedicAI (state Idle).
    // Tidak wajib ditambahkan manual: kalau belum ada, RobotFSM/MedicAI menambahkannya sendiri
    // dengan nilai bawaan. Tambahkan manual hanya kalau mau mengubah radius patroli.
    [RequireComponent(typeof(NavMeshAgent))]
    public class NpcPatrol : MonoBehaviour
    {
        [Header("Area Patroli")]
        [Tooltip("Radius area patroli di sekitar tempat NPC pertama kali muncul.")]
        [SerializeField] private float patrolRadius = 10f;

        [Header("Jeda dan Waktu")]
        [SerializeField] private float minPause = 0.5f;
        [SerializeField] private float maxPause = 2f;
        [SerializeField] private float arriveDistance = 1f;
        [Tooltip("Batas waktu menuju satu titik. Lewat dari ini, titik diganti (mencegah macet).")]
        [SerializeField] private float maxLegTime = 10f;

        private Vector3 homePosition;
        private bool hasTarget;
        private bool waiting;
        private float pauseTimer;
        private float legTimer;

        private void Awake()
        {
            homePosition = transform.position;
        }

        // Panggil saat NPC kembali ke state Idle: langsung cari titik patroli baru.
        public void Begin()
        {
            hasTarget = false;
            waiting = false;
        }

        // Panggil setiap frame selama NPC berada di state Idle.
        public void Tick(NavMeshAgent agent)
        {
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

            if (waiting)
            {
                pauseTimer -= Time.deltaTime;
                if (pauseTimer <= 0f) waiting = false;
                return;
            }

            if (!hasTarget)
            {
                PickNewPoint(agent);
                return;
            }

            legTimer -= Time.deltaTime;
            bool arrived = !agent.pathPending && agent.remainingDistance <= arriveDistance;

            if (arrived || legTimer <= 0f)
            {
                hasTarget = false;
                waiting = true;
                pauseTimer = Random.Range(minPause, maxPause);
                agent.ResetPath();
            }
        }

        private void PickNewPoint(NavMeshAgent agent)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 offset = Random.insideUnitCircle * patrolRadius;
                Vector3 candidate = homePosition + new Vector3(offset.x, 0f, offset.y);

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;

                // Jangan pilih titik yang terlalu dekat dengan posisi sekarang
                if ((hit.position - transform.position).sqrMagnitude < 4f) continue;

                // Pastikan titik itu benar-benar bisa dicapai lewat NavMesh
                NavMeshPath path = new NavMeshPath();
                if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) continue;

                agent.isStopped = false;
                agent.SetDestination(hit.position);
                hasTarget = true;
                legTimer = maxLegTime;
                return;
            }

            // Tidak menemukan titik: tunggu sebentar lalu coba lagi
            waiting = true;
            pauseTimer = 1f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 center = Application.isPlaying ? homePosition : transform.position;
            Gizmos.DrawWireSphere(center, patrolRadius);
        }
    }
}