using UnityEngine;

namespace RobotArena
{
    // Data satu jenis senjata. Buat lewat: klik kanan di Project > Create > RobotArena > Weapon
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "RobotArena/Weapon")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identitas")]
        public string weaponName = "Pistol";
        public Color color = Color.white;          // warna pickup di arena
        public GameObject modelPrefab;             // model senjata (dipakai di tangan robot & di lantai)

        [Header("Penempatan di tangan robot (isi setelah dicoba saat Play)")]
        public Vector3 heldPosition = Vector3.zero;
        public Vector3 heldRotation = Vector3.zero;
        public float heldScale = 1f;

        [Header("Tampilan di lantai (pickup)")]
        public float pickupScale = 1f;

        [Header("Tembakan")]
        public int damage = 10;
        public float fireRate = 0.25f;             // jeda antar tembakan (detik)
        public int pellets = 1;                    // jumlah peluru per tembakan (shotgun > 1)
        public float spreadAngle = 0f;             // sebaran peluru (derajat)

        [Header("Amunisi")]
        public bool infiniteAmmo = true;
        public int maxAmmo = 20;
    }
}