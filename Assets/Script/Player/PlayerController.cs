using System.Collections;
using UnityEngine;

namespace RobotArena
{
    // WASD untuk gerak, robot menghadap ke arah mouse. Kamera top-down (atas arena).
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(HealthSystem))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private Camera cam;

        // Dipakai power up Speed Boost.
        public float SpeedMultiplier { get; set; } = 1f;

        private CharacterController controller;
        private HealthSystem health;
        private float verticalVelocity;
        private Coroutine boostRoutine;

        public bool IsSpeedBoosted => boostRoutine != null;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<HealthSystem>();
            if (cam == null) cam = Camera.main;
        }

        private void OnEnable()
        {
            health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            health.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            enabled = false;
        }

        // Dipanggil PowerUpPickup. Kalau boost masih jalan, durasinya diulang dari awal.
        public void ApplySpeedBoost(float multiplier, float duration)
        {
            if (boostRoutine != null) StopCoroutine(boostRoutine);
            boostRoutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
        }

        private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
        {
            SpeedMultiplier = multiplier;
            yield return new WaitForSeconds(duration);
            SpeedMultiplier = 1f;
            boostRoutine = null;
        }

        private void Update()
        {
            Move();
            AimAtMouse();
        }

        private void Move()
        {
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = input * moveSpeed * SpeedMultiplier;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        private void AimAtMouse()
        {
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

            if (plane.Raycast(ray, out float distance))
            {
                Vector3 direction = ray.GetPoint(distance) - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }
    }
}