using UnityEngine;

namespace RobotArena
{
    // Dipakai bersama oleh Player dan NPC. Pengirim memanggil Initialize().
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifetime = 3f;

        private int damage = 10;
        private Team ownerTeam = Team.Player;

        public void Initialize(int damageAmount, Team team)
        {
            damage = damageAmount;
            ownerTeam = team;
        }

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Abaikan trigger lain (misalnya power up).
            if (other.isTrigger) return;

            HealthSystem target = other.GetComponentInParent<HealthSystem>();
            if (target != null)
            {
                if (target.Team == ownerTeam) return; // jangan kena sekutu
                target.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}