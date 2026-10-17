using UnityEngine;

namespace RobotArena
{
    public enum PowerUpType
    {
        Heal,
        Speed
    }

    // Pickup Heal / Speed Boost. Disentuh player = efek diterapkan + efek partikel.
    [RequireComponent(typeof(Collider))]
    public class PowerUpPickup : MonoBehaviour
    {
        [Header("Heal")]
        [SerializeField] private int healAmount = 30;

        [Header("Speed Boost")]
        [SerializeField] private float speedMultiplier = 1.5f;
        [SerializeField] private float speedDuration = 5f;

        [Header("Tampilan")]
        [SerializeField] private float spinSpeed = 90f;
        [SerializeField] private Color healColor = new Color(0.2f, 0.9f, 0.3f);
        [SerializeField] private Color speedColor = new Color(0.2f, 0.8f, 1f);

        [Header("Bentuk (opsional)")]
        [Tooltip("Objek anak yang tampil khusus untuk Heal. Kosong = pakai bentuk bawaan prefab.")]
        [SerializeField] private GameObject healVisual;
        [Tooltip("Objek anak yang tampil khusus untuk Speed. Kosong = pakai bentuk bawaan prefab.")]
        [SerializeField] private GameObject speedVisual;

        private PowerUpType type;
        private PowerUpSpawner spawner;

        public void Initialize(PowerUpType powerUpType, PowerUpSpawner owner)
        {
            type = powerUpType;
            spawner = owner;

            Color tint = type == PowerUpType.Heal ? healColor : speedColor;
            bool hasCustomVisuals = healVisual != null || speedVisual != null;

            if (hasCustomVisuals)
            {
                // Sembunyikan bentuk bawaan, tampilkan hanya yang sesuai jenisnya.
                Renderer rootRenderer = GetComponent<Renderer>();
                if (rootRenderer != null) rootRenderer.enabled = false;

                if (healVisual != null) healVisual.SetActive(type == PowerUpType.Heal);
                if (speedVisual != null) speedVisual.SetActive(type == PowerUpType.Speed);

                GameObject active = type == PowerUpType.Heal ? healVisual : speedVisual;
                if (active != null)
                {
                    foreach (Renderer r in active.GetComponentsInChildren<Renderer>())
                    {
                        r.material.color = tint;
                    }
                }
            }
            else
            {
                Renderer r = GetComponentInChildren<Renderer>();
                if (r != null) r.material.color = tint;
            }
        }

        private void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            Vector3 effectPos = player.transform.position + Vector3.up * 1f;

            if (type == PowerUpType.Heal)
            {
                HealthSystem health = player.GetComponent<HealthSystem>();
                if (health == null || health.IsDead) return;
                if (health.HPPercent >= 1f) return; // HP penuh: pickup tidak terpakai

                health.Heal(healAmount);
                PickupEffect.Burst(effectPos, healColor);
                PickupMessageUI.Show("+" + healAmount + " HP", healColor);
            }
            else
            {
                player.ApplySpeedBoost(speedMultiplier, speedDuration);
                PickupEffect.Burst(effectPos, speedColor);
                PickupEffect.Attach(player.transform, speedColor, speedDuration);
                PickupMessageUI.Show("Speed Boost " + speedDuration.ToString("0") + " detik", speedColor);
            }

            if (spawner != null) spawner.NotifyTaken();
            Destroy(gameObject);
        }
    }
}