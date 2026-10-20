using UnityEngine;

namespace RobotArena.Enemy
{
    // ALAT BANTU TES. Tempel di NPC (atau Player) lalu, saat Play, klik titik tiga (⋮)
    // di komponen ini untuk memberi damage / heal manual tanpa harus menembak.
    // Boleh dihapus dari prefab saat versi final.
    [RequireComponent(typeof(HealthSystem))]
    public class NpcTestHelper : MonoBehaviour
    {
        [ContextMenu("TEST - Kena damage 10")]
        private void Damage10()
        {
            GetComponent<HealthSystem>().TakeDamage(10);
        }

        [ContextMenu("TEST - Heal 10")]
        private void Heal10()
        {
            GetComponent<HealthSystem>().Heal(10);
        }

        [ContextMenu("TEST - Matikan (damage 9999)")]
        private void Kill()
        {
            GetComponent<HealthSystem>().TakeDamage(9999);
        }
    }
}