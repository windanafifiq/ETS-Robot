namespace RobotArena
{
    // Kontrak bersama: semua yang bisa kena damage (Player & NPC) implement ini.
    public interface IDamageable
    {
        void TakeDamage(int amount);
    }
}