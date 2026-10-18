using UnityEngine;
using RobotArena; 

namespace RobotArena.Enemy
{
    // Data "kepribadian" NPC. Buat asset lewat:
    // klik kanan di Project > Create > RobotArena > Robot Utility Weights
    [CreateAssetMenu(fileName = "NewRobotUtilityWeights", menuName = "RobotArena/Robot Utility Weights")]
    public class RobotUtilityWeights : ScriptableObject
    {
        [Header("Bobot per Aksi (0 - 2, 1 = normal)")]
        [Range(0f, 2f)] public float attackWeight = 1f;
        [Range(0f, 2f)] public float maintainDistanceWeight = 1f;
        [Range(0f, 2f)] public float coverWeight = 1f;
        [Range(0f, 2f)] public float retreatWeight = 1f;

        [Header("Jarak")]
        [Tooltip("Jarak ideal bertarung. Sniper = besar, Attacker = kecil.")]
        public float idealCombatDistance = 5f;
        [Tooltip("Robot baru mau menembak kalau jarak ke player <= nilai ini (dan player terlihat).")]
        public float attackRange = 9f;

        [Header("Retreat")]
        [Tooltip("Di bawah persentase HP ini robot masuk state Retreat (0.25 = 25%).")]
        [Range(0.05f, 0.9f)] public float retreatHPThreshold = 0.25f;

        [Header("Perilaku Khusus")]
        [Tooltip("Centang untuk Sniper: setelah menembak, robot ingin bersembunyi di cover.")]
        public bool hideAfterShooting = false;
    }
}