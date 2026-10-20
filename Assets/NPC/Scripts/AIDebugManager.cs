using UnityEngine;
using RobotArena; 

namespace RobotArena.DebugTools
{
    // Pasang SATU kali di GameObject kosong "AIDebugManager". Tekan F1 saat Play.
    public class AIDebugManager : MonoBehaviour
    {
        public static AIDebugManager Instance { get; private set; }
        public bool DebugEnabled { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (TogglePressed())
            {
                DebugEnabled = !DebugEnabled;
            }
        }

        // Mendukung Input System baru maupun Input Manager lama (Unity 6 bisa salah satunya).
        private bool TogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Keyboard.current != null &&
                   UnityEngine.InputSystem.Keyboard.current.f1Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F1);
#endif
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, Screen.height - 28, 320, 24),
                DebugEnabled ? "AI DEBUG MODE: ON (F1)" : "AI Debug Mode: OFF (F1)");
        }
    }
}