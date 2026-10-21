using UnityEngine;
using UnityEngine.AI;

namespace RobotArena.Enemy
{
    // Menghubungkan MedicAI ke Animator TANPA mengubah MedicAI.cs.
    // Parameter di Animator Controller (semuanya OPSIONAL; yang tidak ada dilewati tanpa error):
    //   Speed   (Float)   : kecepatan jalan, untuk Idle <-> Jalan
    //   Healing (Bool)    : true saat Medic sedang menyembuhkan
    //   Die     (Trigger) : dipicu saat Medic mati
    [RequireComponent(typeof(MedicAI))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(HealthSystem))]
    public class MedicAnimation : MonoBehaviour
    {
        [Tooltip("Boleh dikosongkan: dicari otomatis di model (child).")]
        [SerializeField] private Animator animator;
        [SerializeField] private float speedDamp = 0.1f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int HealingHash = Animator.StringToHash("Healing");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private MedicAI medic;
        private NavMeshAgent agent;
        private HealthSystem health;

        private bool hasSpeed;
        private bool hasHealing;
        private bool hasDie;

        private void Awake()
        {
            medic = GetComponent<MedicAI>();
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<HealthSystem>();

            if (animator == null) animator = GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Debug.LogWarning("MedicAnimation: Animator tidak ditemukan di model Medic.", this);
                enabled = false;
                return;
            }

            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("MedicAnimation: kotak Controller di Animator masih kosong.", this);
                enabled = false;
                return;
            }

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == "Speed" && p.type == AnimatorControllerParameterType.Float) hasSpeed = true;
                if (p.name == "Healing" && p.type == AnimatorControllerParameterType.Bool) hasHealing = true;
                if (p.name == "Die" && p.type == AnimatorControllerParameterType.Trigger) hasDie = true;
            }

            if (!hasSpeed)
            {
                Debug.LogWarning("MedicAnimation: controller tidak punya parameter Float bernama 'Speed', jadi animasi jalan tidak akan berganti.", this);
            }
        }

        private void OnEnable()
        {
            if (health != null) health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (health != null) health.OnDeath -= HandleDeath;
        }

        private void Update()
        {
            if (medic.State == MedicState.Dead) return;

            if (hasSpeed)
            {
                animator.SetFloat(SpeedHash, agent.velocity.magnitude, speedDamp, Time.deltaTime);
            }

            if (hasHealing)
            {
                animator.SetBool(HealingHash, medic.State == MedicState.Healing);
            }
        }

        private void HandleDeath()
        {
            if (hasDie) animator.SetTrigger(DieHash);
        }
    }
}