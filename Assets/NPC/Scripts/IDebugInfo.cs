namespace RobotArena.DebugTools
{
    // Dipasang oleh setiap AI NPC (RobotFSM, MedicAI) supaya RobotDebugDisplay
    // bisa menampilkan "isi kepala" NPC tanpa perlu tahu jenis NPC-nya.
    public interface IDebugInfo
    {
        string GetDebugText();
    }
}