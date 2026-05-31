using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>主菜单 → 玩法场景之间传递的难度与玩家名。</summary>
    public static class GameSessionData
    {
        public static GameDifficulty Difficulty { get; set; } = GameDifficulty.Easy;
        public static string PlayerName { get; set; } = "玩家";

        /// <summary>简单：三名巡查老师（爱音）；普通：无巡查，仅班主任（素世）。</summary>
        public static int PatrolTeacherCount =>
            Difficulty == GameDifficulty.Easy ? 3 : 0;

        public static bool SpawnHeadTeacherAtLevelStart =>
            Difficulty == GameDifficulty.Normal;

        public static float CountdownSeconds =>
            Difficulty == GameDifficulty.Normal ? 480f : 0f;

        public static bool HasCountdown => Difficulty == GameDifficulty.Normal;
    }
}
