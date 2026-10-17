using System;
using UnityEngine;

namespace RobotArena
{
    // Pasang di Player DAN setiap NPC. Dinda bisa pakai Heal() untuk Support Bot.
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHP = 100;
        [SerializeField] private Team team = Team.Player;

        [Header("Status (hanya untuk dilihat, terisi saat Play)")]
        [SerializeField] private int currentHP;
        [SerializeField] private bool isDead;

        public int CurrentHP
        {
            get => currentHP;
            private set => currentHP = value;
        }
        public int MaxHP => maxHP;
        public float HPPercent => maxHP > 0 ? (float)CurrentHP / maxHP : 0f;
        public bool IsDead
        {
            get => isDead;
            private set => isDead = value;
        }
        public Team Team => team;

        // (currentHP, maxHP) - dipakai health bar UI
        public event Action<int, int> OnHealthChanged;
        public event Action OnDamaged;
        public event Action OnDeath;

        private void Awake()
        {
            CurrentHP = maxHP;
        }

        private void Start()
        {
            // Supaya UI langsung menampilkan HP penuh saat game mulai.
            OnHealthChanged?.Invoke(CurrentHP, maxHP);
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;

            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            OnHealthChanged?.Invoke(CurrentHP, maxHP);
            OnDamaged?.Invoke();

            if (CurrentHP <= 0)
            {
                IsDead = true;
                OnDeath?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;

            CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
            OnHealthChanged?.Invoke(CurrentHP, maxHP);
        }
    }
}