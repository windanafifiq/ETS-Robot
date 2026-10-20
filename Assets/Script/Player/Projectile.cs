using System.Collections.Generic;
using UnityEngine;

namespace RobotArena
{
    // Dipakai bersama oleh Player dan NPC. Pengirim memanggil Initialize().
    // explosionRadius > 0 = meledak dan melukai semua musuh dalam radius (Bazooka).
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifetime = 3f;
        [Tooltip("Opsional: efek yang dimunculkan saat meledak.")]
        [SerializeField] private GameObject explosionEffect;

        private int damage = 10;
        private Team ownerTeam = Team.Player;
        private float explosionRadius = 0f;
        private Color effectColor = Color.white;

        // radius bersifat opsional, jadi pemanggilan lama Initialize(damage, team) tetap bisa dipakai.
        public void Initialize(int damageAmount, Team team, float radius = 0f)
        {
            damage = damageAmount;
            ownerTeam = team;
            explosionRadius = radius;

            effectColor = ShootEffect.ColorFor(team);
            ShootEffect.AddTrail(gameObject, effectColor);
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
            // Abaikan trigger lain (misalnya pickup).
            if (other.isTrigger) return;

            HealthSystem target = other.GetComponentInParent<HealthSystem>();
            if (target != null && target.Team == ownerTeam) return; // jangan kena sekutu

            if (explosionRadius > 0f)
            {
                Explode();
            }
            else if (target != null)
            {
                target.TakeDamage(damage);
            }

            ShootEffect.Impact(transform.position, effectColor);
            Destroy(gameObject);
        }

        private void Explode()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            HashSet<HealthSystem> alreadyHit = new HashSet<HealthSystem>();

            foreach (Collider hit in hits)
            {
                if (hit.isTrigger) continue;

                HealthSystem h = hit.GetComponentInParent<HealthSystem>();
                if (h == null || h.Team == ownerTeam) continue;
                if (!alreadyHit.Add(h)) continue; // satu target hanya kena sekali

                h.TakeDamage(damage);
            }

            if (explosionEffect != null)
            {
                GameObject fx = Instantiate(explosionEffect, transform.position, Quaternion.identity);
                Destroy(fx, 2f);
            }
        }
    }
}