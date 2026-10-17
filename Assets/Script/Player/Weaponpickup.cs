using UnityEngine;

namespace RobotArena
{
    // Objek senjata yang berputar di arena. Disentuh player = senjata diambil.
    [RequireComponent(typeof(Collider))]
    public class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private float spinSpeed = 90f;

        private WeaponData weapon;
        private WeaponSpawner spawner;

        public void Initialize(WeaponData data, WeaponSpawner owner)
        {
            weapon = data;
            spawner = owner;

            if (data.modelPrefab != null)
            {
                // Sembunyikan kubus, tampilkan model senjata asli.
                foreach (Renderer rend in GetComponentsInChildren<Renderer>())
                {
                    rend.enabled = false;
                }

                GameObject model = Instantiate(data.modelPrefab, transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                // Kompensasi skala induk (kubus prefab berskala 0.5).
                float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);
                model.transform.localScale = Vector3.one * (data.pickupScale / parentScale);
            }
            else
            {
                Renderer r = GetComponentInChildren<Renderer>();
                if (r != null) r.material.color = data.color;
            }
        }

        private void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerShooting shooting = other.GetComponentInParent<PlayerShooting>();
            if (shooting == null || weapon == null) return;

            shooting.EquipWeapon(weapon);
            PickupMessageUI.Show("Senjata: " + weapon.weaponName, Color.white);

            if (spawner != null) spawner.NotifyTaken();
            Destroy(gameObject);
        }
    }
}