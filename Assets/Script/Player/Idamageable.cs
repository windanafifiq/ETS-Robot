using UnityEngine;

namespace RobotArena
{
    // KONTRAK DAMAGE: semua objek yang bisa terkena tembakan (Player maupun NPC)
    // harus punya komponen yang mengimplementasikan interface ini.
    public interface IDamageable
    {
        void TakeDamage(int amount);
    }
}