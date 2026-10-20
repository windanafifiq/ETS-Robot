using System;
using UnityEngine;

namespace RobotArena
{
    // Klik kiri (tahan) untuk menembak. Mendukung senjata pickup dengan amunisi terbatas.
    // Kalau amunisi habis, otomatis kembali ke senjata dasar (Default Weapon).
    [RequireComponent(typeof(HealthSystem))]
    public class PlayerShooting : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private WeaponData defaultWeapon;
        [Tooltip("Opsional: titik di tangan robot tempat model senjata dipasang.")]
        [SerializeField] private Transform weaponHolder;

        // Dipakai power up Damage Boost (kalau nanti ada).
        public float DamageMultiplier { get; set; } = 1f;

        public WeaponData CurrentWeapon { get; private set; }
        public int CurrentAmmo { get; private set; }

        // (senjata aktif, sisa amunisi) - dipakai AmmoUI
        public event Action<WeaponData, int> OnAmmoChanged;

        private HealthSystem health;
        private float nextFireTime;
        private GameObject heldModel;

        private void Awake()
        {
            health = GetComponent<HealthSystem>();

            // Cadangan 1: kalau kosong, cari asset Pistol di Assets/Resources/Weapons.
            if (defaultWeapon == null)
            {
                defaultWeapon = Resources.Load<WeaponData>("Weapons/Pistol");
            }

            // Cadangan 2: kalau tetap tidak ada, buat pistol standar otomatis (tanpa model).
            if (defaultWeapon == null)
            {
                defaultWeapon = ScriptableObject.CreateInstance<WeaponData>();
                defaultWeapon.weaponName = "Pistol";
                Debug.LogWarning("PlayerShooting: kotak Default Weapon KOSONG, memakai pistol cadangan tanpa suara dan tanpa model. Isi dengan asset Pistol.", this);
            }
        }

        private void OnEnable()
        {
            health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            health.OnDeath -= HandleDeath;
        }

        private void Start()
        {
            EquipWeapon(defaultWeapon);
        }

        private void HandleDeath()
        {
            enabled = false;
        }

        private void Update()
        {
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                Shoot();
            }
        }

        // Dipanggil WeaponPickup saat player mengambil senjata.
        public void EquipWeapon(WeaponData weapon)
        {
            if (weapon == null) weapon = defaultWeapon;

            CurrentWeapon = weapon;
            CurrentAmmo = weapon.infiniteAmmo ? -1 : weapon.maxAmmo;

            if (weapon.shootSound == null)
            {
                Debug.LogWarning("PlayerShooting: senjata '" + weapon.weaponName + "' belum punya Shoot Sound.", this);
            }

            if (heldModel != null) Destroy(heldModel);
            if (weapon.modelPrefab != null && weaponHolder != null)
            {
                heldModel = Instantiate(weapon.modelPrefab, weaponHolder);
                heldModel.transform.localPosition = weapon.heldPosition;
                heldModel.transform.localRotation = Quaternion.Euler(weapon.heldRotation);
                heldModel.transform.localScale = Vector3.one * weapon.heldScale;
            }

            OnAmmoChanged?.Invoke(CurrentWeapon, CurrentAmmo);
        }

        private void Shoot()
        {
            if (projectilePrefab == null || firePoint == null)
            {
                Debug.LogWarning("PlayerShooting: Projectile Prefab atau Fire Point belum diisi.");
                nextFireTime = Time.time + 0.5f;
                return;
            }

            WeaponData w = CurrentWeapon;
            nextFireTime = Time.time + w.fireRate;

            int damage = Mathf.RoundToInt(w.damage * DamageMultiplier);
            int count = Mathf.Max(1, w.pellets);

            for (int i = 0; i < count; i++)
            {
                float angle = count > 1 ? UnityEngine.Random.Range(-w.spreadAngle / 2f, w.spreadAngle / 2f) : 0f;
                Quaternion rotation = firePoint.rotation * Quaternion.Euler(0f, angle, 0f);

                Projectile prefab = w.projectileOverride != null ? w.projectileOverride : projectilePrefab;
                Projectile p = Instantiate(prefab, firePoint.position, rotation);
                p.Initialize(damage, health.Team, w.explosionRadius);
            }

            ShootEffect.MuzzleFlash(firePoint, ShootEffect.PlayerColor);
            ShootEffect.PlaySound2D(w.shootSound, 1f);

            if (!w.infiniteAmmo)
            {
                CurrentAmmo--;
                if (CurrentAmmo <= 0)
                {
                    EquipWeapon(defaultWeapon); // amunisi habis, kembali ke pistol
                    return;
                }
            }

            OnAmmoChanged?.Invoke(CurrentWeapon, CurrentAmmo);
        }
    }
}