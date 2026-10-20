using UnityEngine;
using RobotArena; 

namespace RobotArena.DebugTools
{
    // Tempel di prefab NPC. Menampilkan teks di atas kepala NPC saat Debug Mode (F1) aktif.
    // Memakai OnGUI sehingga TIDAK perlu membuat child object atau Canvas.
    public class RobotDebugDisplay : MonoBehaviour
    {
        [SerializeField] private float labelHeight = 2.4f;

        private IDebugInfo info;
        private Enemy.RobotPerception perception;
        private Camera cam;
        private GUIStyle style;
        private GUIStyle shadowStyle;

        private void Awake()
        {
            info = GetComponent<IDebugInfo>();
            perception = GetComponent<Enemy.RobotPerception>();
        }

        private bool DebugOn =>
            AIDebugManager.Instance != null && AIDebugManager.Instance.DebugEnabled;

        private void OnGUI()
        {
            if (!DebugOn || info == null) return;

            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * labelHeight);
            if (screen.z < 0f) return; // di belakang kamera

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.LowerCenter;
                style.fontSize = 13;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.white;

                shadowStyle = new GUIStyle(style);
                shadowStyle.normal.textColor = Color.black;
            }

            string text = info.GetDebugText();
            Vector2 size = style.CalcSize(new GUIContent(text));
            Rect rect = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y, size.x, size.y);

            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, shadowStyle);
            GUI.Label(rect, text, style);
        }

        // Lingkaran perception + garis ke posisi terakhir Player yang diingat (Scene view)
        private void OnDrawGizmos()
        {
            if (!DebugOn || perception == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, perception.DetectionRadius);

            if (perception.HasMemoryOfPlayer)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, perception.LastKnownPlayerPosition);
                Gizmos.DrawWireSphere(perception.LastKnownPlayerPosition, 0.4f);
            }
        }
    }
}