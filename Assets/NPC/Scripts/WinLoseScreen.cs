using System.Reflection;
using UnityEngine;

namespace RobotArena
{
    // Layar MENANG / KALAH tanpa Canvas (digambar lewat OnGUI).
    // Membaca status dari GameManager milik teman (IsGameOver), tidak mengubah logikanya.
    //   Menang = tidak ada lagi NPC (Team.Enemy) yang hidup
    //   Kalah  = game berakhir tapi masih ada NPC hidup (HP Player habis)
    // Jika GameManager sudah punya panel Win/Lose sendiri, script ini mematikan dirinya.
    // Pasang di GameObject kosong bernama "WinLoseScreen".
    public class WinLoseScreen : MonoBehaviour
    {
        [Header("Teks Menang")]
        [SerializeField] private string winTitle = "SIMULATION PASSED";
        [SerializeField] private string winSubtitle = "ARC-7 dinyatakan LULUS dan siap diterjunkan.";

        [Header("Teks Kalah")]
        [SerializeField] private string loseTitle = "SYSTEM FAILURE";
        [SerializeField] private string loseSubtitle = "ARC-7 gagal menyelesaikan simulasi.";

        [Header("Tombol")]
        [SerializeField] private string retryLabel = "COBA LAGI";

        private GameManager gameManager;
        private bool shown;
        private bool won;
        private int totalEnemies;
        private int defeatedEnemies;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;

        private void Start()
        {
            gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("WinLoseScreen: GameManager tidak ditemukan di scene.", this);
                enabled = false;
                return;
            }

            if (HasPanel(gameManager, "winPanel") || HasPanel(gameManager, "losePanel"))
            {
                Debug.Log("WinLoseScreen: GameManager sudah punya panel Win/Lose sendiri, layar ini dinonaktifkan agar tidak dobel.", this);
                enabled = false;
                return;
            }

            totalEnemies = CountAliveEnemies();
        }

        private void Update()
        {
            if (shown || gameManager == null) return;
            if (!gameManager.IsGameOver) return;

            shown = true;
            int alive = CountAliveEnemies();
            won = alive == 0;
            defeatedEnemies = Mathf.Max(0, totalEnemies - alive);
        }

        private void OnGUI()
        {
            if (!shown) return;

            EnsureStyles();

            // Layar redup
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            // Kartu di tengah
            float width = Mathf.Min(680f, Screen.width * 0.9f);
            float height = Mathf.Min(360f, Screen.height * 0.8f);
            Rect card = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            GUI.color = new Color(0.07f, 0.09f, 0.13f, 0.96f);
            GUI.DrawTexture(card, Texture2D.whiteTexture);

            Color accent = won ? new Color(0.25f, 0.95f, 0.55f) : new Color(1f, 0.35f, 0.35f);

            // Garis aksen di atas kartu
            GUI.color = accent;
            GUI.DrawTexture(new Rect(card.x, card.y, card.width, 6f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.06f), 26, 54);
            bodyStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.028f), 16, 26);
            buttonStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.03f), 18, 28);

            titleStyle.normal.textColor = accent;
            GUI.Label(new Rect(card.x + 20f, card.y + 34f, card.width - 40f, titleStyle.fontSize + 24f),
                won ? winTitle : loseTitle, titleStyle);

            GUI.Label(new Rect(card.x + 30f, card.y + 120f, card.width - 60f, bodyStyle.fontSize * 3f),
                won ? winSubtitle : loseSubtitle, bodyStyle);

            if (totalEnemies > 0)
            {
                GUI.Label(new Rect(card.x + 30f, card.y + 120f + bodyStyle.fontSize * 3f, card.width - 60f, bodyStyle.fontSize + 12f),
                    $"Robot penguji dikalahkan: {defeatedEnemies} / {totalEnemies}", bodyStyle);
            }

            Rect button = new Rect(card.center.x - 120f, card.yMax - 88f, 240f, 56f);
            if (GUI.Button(button, retryLabel, buttonStyle))
            {
                gameManager.Restart();
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;

            bodyStyle = new GUIStyle(GUI.skin.label);
            bodyStyle.alignment = TextAnchor.UpperCenter;
            bodyStyle.wordWrap = true;
            bodyStyle.normal.textColor = new Color(0.9f, 0.92f, 0.95f);

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
        }

        private static int CountAliveEnemies()
        {
            int count = 0;
            foreach (HealthSystem hs in FindObjectsByType<HealthSystem>(FindObjectsInactive.Exclude))
            {
                if (hs.Team == Team.Enemy && !hs.IsDead) count++;
            }
            return count;
        }

        // Mengecek apakah GameManager sudah punya panel UI sendiri (field winPanel / losePanel terisi)
        private static bool HasPanel(GameManager manager, string fieldName)
        {
            FieldInfo field = typeof(GameManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return false;

            Object panel = field.GetValue(manager) as Object;
            return panel != null;
        }
    }
}