using UnityEngine;

namespace RobotArena
{
    // Titik spawn Heal / Speed Boost. Memilih jenis acak dari daftar,
    // lalu muncul lagi setelah diambil.
    public class PowerUpSpawner : MonoBehaviour
    {
        [SerializeField] private PowerUpPickup pickupPrefab;
        [SerializeField] private PowerUpType[] possibleTypes = { PowerUpType.Heal, PowerUpType.Speed };
        [SerializeField] private float respawnDelay = 20f;
        [SerializeField] private float heightOffset = 0.6f;

        private void Start()
        {
            Spawn();
        }

        private void Spawn()
        {
            if (pickupPrefab == null || possibleTypes == null || possibleTypes.Length == 0)
            {
                Debug.LogWarning("PowerUpSpawner: Pickup Prefab atau daftar jenis belum diisi.", this);
                return;
            }

            PowerUpType chosen = possibleTypes[Random.Range(0, possibleTypes.Length)];
            Vector3 pos = transform.position + Vector3.up * heightOffset;

            PowerUpPickup pickup = Instantiate(pickupPrefab, pos, Quaternion.identity);
            pickup.Initialize(chosen, this);
        }

        public void NotifyTaken()
        {
            Invoke(nameof(Spawn), respawnDelay);
        }
    }
}