using System;
using System.Collections.Generic;

namespace Game
{
    [Serializable]
    public class LordCardReward
    {
        public string itemCode;   // 物品代码
        public int amount;        // 数量
    }

    [Serializable]
    public class LordCardLevel
    {
        public int level;               // 等级（1~100）
        public string title;            // 称号
        public int requiredFame;        // 升级所需声望
        public int copperIncome;        // 铜钱收入
        public bool addSoldierSymbol;   // 是否增加兵符
        public int baseSoldierCount;    // 基础带兵数量
        public int rewardSoldier;       // 奖励士兵
        public int skillPoints;         // 技能点

        public List<LordCardReward> rewards = new(); // 额外奖励列表
    }
}
