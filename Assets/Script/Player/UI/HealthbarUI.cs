using UnityEngine;
using UnityEngine.UI;

namespace RobotArena
{
    // Pasang di objek Slider. Slider otomatis mengikuti HP target.
    [RequireComponent(typeof(Slider))]
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private HealthSystem target;

        private Slider slider;

        private void Awake()
        {
            slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            if (target != null)
            {
                target.OnHealthChanged += UpdateBar;
                UpdateBar(target.CurrentHP, target.MaxHP);
            }
        }

        private void OnDisable()
        {
            if (target != null) target.OnHealthChanged -= UpdateBar;
        }

        private void UpdateBar(int current, int max)
        {
            slider.maxValue = max;
            slider.value = current;
        }
    }
}