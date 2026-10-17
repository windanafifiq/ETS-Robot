using TMPro;
using UnityEngine;

namespace RobotArena
{
    // Menampilkan nama senjata dan sisa amunisi. Pasang di objek TextMeshPro.
    // Kotak Target dan Label boleh dikosongkan: dicari otomatis.
    public class AmmoUI : MonoBehaviour
    {
        [SerializeField] private PlayerShooting target;
        [SerializeField] private TMP_Text label;

        private void Awake()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            if (target == null) target = FindFirstObjectByType<PlayerShooting>();
        }

        private void OnEnable()
        {
            if (target == null) return;

            target.OnAmmoChanged += UpdateLabel;
            if (target.CurrentWeapon != null) UpdateLabel(target.CurrentWeapon, target.CurrentAmmo);
        }

        private void OnDisable()
        {
            if (target != null) target.OnAmmoChanged -= UpdateLabel;
        }

        private void UpdateLabel(WeaponData weapon, int ammo)
        {
            if (label == null) return;

            label.text = weapon.infiniteAmmo
                ? weapon.weaponName + "  -  Tak terbatas"
                : weapon.weaponName + "  -  " + ammo + " / " + weapon.maxAmmo;
        }
    }
}