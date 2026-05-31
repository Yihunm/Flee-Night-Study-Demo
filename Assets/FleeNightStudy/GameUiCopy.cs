namespace FleeNightStudy
{
    /// <summary>主菜单与玩法 UI 共用文案。</summary>
    public static class GameUiCopy
    {
        public const string InstructionsBody =
            "WASD — 移动\n鼠标 — 视角\nE — 开门\n1~5 — 切换道具\nZ — 使用道具\nH — 操作手册\nEsc — 暂停菜单（可返回主菜单）\n\n" +
            "收集课本解锁大门，躲开老师，从校门口逃离即胜利。\n游戏过程中可随时 Esc 暂停并返回主菜单；胜负结算界面也可返回主菜单。";

        public const string GameplayHelpHint = "按 H 操作手册  |  按 Esc 暂停/返回主菜单";

        public const string PauseMenuTitle = "暂停";
        public const string PauseContinueLabel = "继续游戏";
        public const string PauseMainMenuLabel = "返回主菜单";

        public const string TextbookLockedFormat = "再收集{0}本课本以解锁大门（还剩{0}本）";

        public const string TextbookUnlockedText = "课本已集齐，前往校门口按 E 离开学校";

        public const string NoItemText = "当前无可用道具";

        public const string AllUiCharacters =
            InstructionsBody + GameplayHelpHint + PauseMenuTitle + PauseContinueLabel + PauseMainMenuLabel +
            TextbookLockedFormat + TextbookUnlockedText + NoItemText +
            "道具短时加速秒隐身吸附引开老师致盲按Z使用当前" +
            "加速辣条隐身校服课本磁铁闹钟粉笔弹校门口离开简单普通模式暂无排名记录" +
            "请选择要查看的难度排行榜用时排行玩家难度收集评分胜利失败返回主菜单请输入玩家名字" +
            "《逃离晚自习》开始游戏操作说明退出按E开门WASD移动0123456789Esc暂停菜单可随时";

        public static string GetItemDisplayName(ItemType type)
        {
            switch (type)
            {
                case ItemType.SpeedSnack: return "加速辣条";
                case ItemType.InvisibilityUniform: return "隐身校服";
                case ItemType.TextbookMagnet: return "课本磁铁";
                case ItemType.AlarmClock: return "闹钟道具";
                case ItemType.ChalkBomb: return "粉笔弹";
                default: return type.ToString();
            }
        }

        public static string GetItemDescription(ItemType type)
        {
            switch (type)
            {
                case ItemType.SpeedSnack: return "短时加速";
                case ItemType.InvisibilityUniform: return "5 秒隐身";
                case ItemType.TextbookMagnet: return "3 秒吸附课本";
                case ItemType.AlarmClock: return "引开老师";
                case ItemType.ChalkBomb: return "致盲老师 2 秒";
                default: return "";
            }
        }

        public static string FormatItemHint(ItemType type, int slot, int count)
        {
            if (count <= 0)
                return NoItemText;

            string name = GetItemDisplayName(type);
            string desc = GetItemDescription(type);
            return $"道具{slot} {name} - {desc} x{count}  按Z使用";
        }
    }
}
