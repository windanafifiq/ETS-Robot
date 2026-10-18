using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RobotArena
{
    // Menang: semua musuh mati. Kalah: HP player habis.
    public class GameManager : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private HealthSystem player;

        [Header("Musuh")]
        [Tooltip("Kalau dicentang, semua HealthSystem ber-Team Enemy di scene dicari otomatis saat Start.")]
        [SerializeField] private bool autoFindEnemies = true;
        [SerializeField] private List<HealthSystem> enemies = new List<HealthSystem>();

        [Header("UI")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;

        public bool IsGameOver { get; private set; }

        private int aliveEnemies;

        private void Start()
        {
            Time.timeScale = 1f;

            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);

            if (autoFindEnemies)
            {
                enemies.Clear();
                HealthSystem[] all = FindObjectsByType<HealthSystem>(FindObjectsInactive.Exclude);
                foreach (HealthSystem h in all)
                {
                    if (h.Team == Team.Enemy) enemies.Add(h);
                }
            }

            aliveEnemies = 0;
            foreach (HealthSystem e in enemies)
            {
                if (e == null) continue;
                aliveEnemies++;
                e.OnDeath += HandleEnemyDeath;
            }

            if (player != null) player.OnDeath += HandlePlayerDeath;
            else Debug.LogWarning("GameManager: Player belum diisi.");
        }

        private void OnDestroy()
        {
            if (player != null) player.OnDeath -= HandlePlayerDeath;
            foreach (HealthSystem e in enemies)
            {
                if (e != null) e.OnDeath -= HandleEnemyDeath;
            }
        }

        private void HandleEnemyDeath()
        {
            if (IsGameOver) return;

            aliveEnemies--;
            if (aliveEnemies <= 0) EndGame(true);
        }

        private void HandlePlayerDeath()
        {
            if (IsGameOver) return;
            EndGame(false);
        }

        private void EndGame(bool win)
        {
            IsGameOver = true;

            if (win && winPanel != null) winPanel.SetActive(true);
            if (!win && losePanel != null) losePanel.SetActive(true);

            Time.timeScale = 0f; // bekukan game
        }

        // Dipanggil dari tombol Restart (OnClick di Inspector).
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}