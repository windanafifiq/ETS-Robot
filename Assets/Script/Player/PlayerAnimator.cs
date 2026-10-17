using UnityEngine;

namespace RobotArena
{
    // Menghubungkan gerak player ke Animator.
    // Parameter yang dibutuhkan di Animator Controller:
    //   Speed (Float)  - kecepatan gerak, untuk Idle <-> Run
    //   Die   (Trigger) - dipicu saat HP habis
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(HealthSystem))]
    public class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private CharacterController controller;
        private HealthSystem health;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<HealthSystem>();

            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null) Debug.LogWarning("PlayerAnimation: Animator tidak ditemukan di model Robot.", this);
        }

        private void OnEnable()
        {
            health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            health.OnDeath -= HandleDeath;
        }

        private void Update()
        {
            if (animator == null || health.IsDead) return;

            Vector3 horizontal = controller.velocity;
            horizontal.y = 0f;

            animator.SetFloat(SpeedHash, horizontal.magnitude, 0.1f, Time.unscaledDeltaTime);
        }

        private void HandleDeath()
        {
            if (animator != null) animator.SetTrigger(DieHash);
        }
    }
}