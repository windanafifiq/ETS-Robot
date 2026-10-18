using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace RobotArena.Enemy
{
    // ALAT DIAGNOSA SEMENTARA. Tempel di NPC yang tidak bergerak, tekan Play,
    // lalu baca Console: bagian "TEMUAN" menyebut penyebab yang paling mungkin.
    // Hapus dari prefab setelah masalah selesai.
    public class NpcDiagnostics : MonoBehaviour
    {
        [SerializeField] private bool autoPrint = true;
        [SerializeField] private float interval = 3f;

        private float timer = 0.5f;

        private void Update()
        {
            if (!autoPrint) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = interval;
                Print();
            }
        }

        [ContextMenu("Cetak Diagnosa")]
        private void Print()
        {
            StringBuilder report = new StringBuilder();
            StringBuilder findings = new StringBuilder();

            report.AppendLine($"=== DIAGNOSA {name} ===");
            report.AppendLine($"Layer NPC: {LayerMask.LayerToName(gameObject.layer)}");

            // ---- HealthSystem ----
            HealthSystem health = GetComponent<HealthSystem>();
            if (health == null)
            {
                findings.AppendLine("- NPC tidak punya komponen HealthSystem.");
            }
            else
            {
                report.AppendLine($"Team NPC: {health.Team} | HP: {health.CurrentHP}/{health.MaxHP}");
                if (health.Team != Team.Enemy)
                    findings.AppendLine("- Team NPC bukan Enemy. Ubah di komponen HealthSystem.");
            }

            // ---- FSM / Medic ----
            RobotFSM fsm = GetComponent<RobotFSM>();
            MedicAI medic = GetComponent<MedicAI>();
            if (fsm != null)
            {
                report.AppendLine($"State RobotFSM: {fsm.CurrentState} | aksi terakhir: {fsm.LastChosenAction}");
                if (!fsm.enabled)
                    findings.AppendLine("- Komponen RobotFSM mati (disabled). Biasanya karena 'Utility Weights' kosong; lihat Console.");
            }
            else if (medic != null)
            {
                report.AppendLine($"State MedicAI: {medic.State}");
            }
            else
            {
                findings.AppendLine("- Tidak ada RobotFSM maupun MedicAI di NPC ini.");
            }

            // ---- NavMeshAgent ----
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                findings.AppendLine("- NPC tidak punya NavMeshAgent.");
            }
            else
            {
                report.AppendLine($"Agent: enabled={agent.enabled}, onNavMesh={agent.isOnNavMesh}, speed={agent.speed}, " +
                                  $"hasPath={agent.hasPath}, isStopped={(agent.isOnNavMesh ? agent.isStopped.ToString() : "?")}, " +
                                  $"velocity={agent.velocity.magnitude:F2}");
                if (!agent.enabled) findings.AppendLine("- NavMeshAgent mati (disabled).");
                else if (!agent.isOnNavMesh) findings.AppendLine("- NPC TIDAK berdiri di atas NavMesh (area biru). Geser NPC ke area biru atau Bake ulang NavMesh, dan cek Base Offset.");
                if (agent.speed <= 0f) findings.AppendLine("- Speed NavMeshAgent = 0.");
            }

            // ---- Perception ----
            RobotPerception perception = GetComponent<RobotPerception>();
            LayerMask playerMask = 0;
            if (perception == null)
            {
                findings.AppendLine("- NPC tidak punya RobotPerception.");
            }
            else
            {
                playerMask = ReadMask(perception, "playerLayer");
                LayerMask obstacleMask = ReadMask(perception, "obstacleLayer");

                report.AppendLine($"Perception: radius={perception.DetectionRadius}, bisaLihatPlayer={perception.CanSeePlayer}, " +
                                  $"jarak={FormatDistance(perception.DistanceToPlayer)}, punyaIngatan={perception.HasMemoryOfPlayer}");
                report.AppendLine($"Player Layer mask: {DescribeMask(playerMask)} | Obstacle Layer mask: {DescribeMask(obstacleMask)}");

                if (playerMask.value == 0)
                    findings.AppendLine("- 'Player Layer' di RobotPerception masih NOTHING. Centang layer Player.");
                if (obstacleMask.value == 0)
                    findings.AppendLine("- 'Obstacle Layer' di RobotPerception masih NOTHING. Centang layer Obstacle.");
            }

            // ---- Player ----
            HealthSystem player = null;
            foreach (HealthSystem hs in FindObjectsByType<HealthSystem>(FindObjectsInactive.Exclude))
            {
                if (hs.Team == Team.Player) { player = hs; break; }
            }

            if (player == null)
            {
                report.AppendLine("Player: TIDAK DITEMUKAN (tidak ada HealthSystem ber-Team Player di scene)");
                findings.AppendLine("- Tidak ada Player ber-Team 'Player' yang punya HealthSystem. NPC tidak punya target.");
            }
            else
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                report.AppendLine($"Player: {player.name} | layer={LayerMask.LayerToName(player.gameObject.layer)} | " +
                                  $"jarak={dist:F1} m | mati={player.IsDead}");

                if (playerMask.value != 0 && (playerMask.value & (1 << player.gameObject.layer)) == 0)
                    findings.AppendLine($"- Layer Player ('{LayerMask.LayerToName(player.gameObject.layer)}') tidak dicentang di 'Player Layer' RobotPerception.");

                if (perception != null && dist > perception.DetectionRadius)
                    findings.AppendLine("- Player berada di luar Detection Radius (lingkaran kuning).");

                if (player.GetComponentInChildren<Collider>() == null)
                    findings.AppendLine("- Player tidak punya Collider, jadi tidak terdeteksi.");
            }

            if (findings.Length == 0)
            {
                findings.AppendLine("- Tidak ada masalah konfigurasi yang jelas. Kemungkinan: penghalang di antara NPC dan Player (garis pandang terhalang), " +
                                    "atau EyePoint berada di dalam objek lain. Coba dekatkan Player tanpa penghalang.");
            }

            report.AppendLine("--- TEMUAN ---");
            report.Append(findings);
            Debug.Log(report.ToString(), this);
        }

        private static LayerMask ReadMask(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return 0;
            return (LayerMask)field.GetValue(target);
        }

        private static string DescribeMask(LayerMask mask)
        {
            if (mask.value == 0) return "Nothing";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 32; i++)
            {
                if ((mask.value & (1 << i)) != 0)
                {
                    string layerName = LayerMask.LayerToName(i);
                    sb.Append(string.IsNullOrEmpty(layerName) ? $"#{i}" : layerName).Append(' ');
                }
            }
            return sb.ToString().Trim();
        }

        private static string FormatDistance(float d)
        {
            return float.IsInfinity(d) ? "tidak ada target" : $"{d:F1} m";
        }
    }
}