using System.Collections;
using TMPro;
using UnityEngine;

namespace RobotArena
{
    // Pesan singkat di layar saat mengambil pickup (muncul lalu memudar).
    // Pasang di objek TextMeshPro. Dipanggil lewat PickupMessageUI.Show("teks", warna).
    public class PickupMessageUI : MonoBehaviour
    {
        public static PickupMessageUI Instance { get; private set; }

        [SerializeField] private TMP_Text label;
        [SerializeField] private float showDuration = 1.8f;
        [SerializeField] private float fadeDuration = 0.5f;

        private Coroutine routine;

        private void Awake()
        {
            Instance = this;
            if (label == null) label = GetComponent<TMP_Text>();
            if (label != null) label.text = "";
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Show(string message, Color color)
        {
            if (Instance != null) Instance.Display(message, color);
        }

        private void Display(string message, Color color)
        {
            if (label == null) return;

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run(message, color));
        }

        private IEnumerator Run(string message, Color color)
        {
            color.a = 1f;
            label.text = message;
            label.color = color;

            yield return new WaitForSecondsRealtime(showDuration);

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                Color c = color;
                c.a = 1f - (t / fadeDuration);
                label.color = c;
                yield return null;
            }

            label.text = "";
            routine = null;
        }
    }
}