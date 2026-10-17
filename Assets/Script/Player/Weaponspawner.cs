using UnityEngine;

namespace RobotArena
{
    // Pasang di GameObject kosong sebagai titik spawn senjata.
    // Memilih senjata acak dari daftar, lalu muncul lagi setelah diambil.
    public class WeaponSpawner : MonoBehaviour
    {
        [SerializeField] private WeaponPickup pickupPrefab;
        [SerializeField] private WeaponData[] possibleWeapons;
        [SerializeField] private float respawnDelay = 15f;
        [SerializeField] private float heightOffset = 0.6f;

        private void Start()
        {
            Spawn();
        }

        private void Spawn()
        {
            if (pickupPrefab == null || possibleWeapons == null || possibleWeapons.Length == 0)
            {
                Debug.LogWarning("WeaponSpawner: Pickup Prefab atau daftar senjata belum diisi.", this);
                return;
            }

            WeaponData chosen = possibleWeapons[Random.Range(0, possibleWeapons.Length)];
            Vector3 pos = transform.position + Vector3.up * heightOffset;

            WeaponPickup pickup = Instantiate(pickupPrefab, pos, Quaternion.identity);
            pickup.Initialize(chosen, this);
        }

        public void NotifyTaken()
        {
            Invoke(nameof(Spawn), respawnDelay);
        }
    }
}