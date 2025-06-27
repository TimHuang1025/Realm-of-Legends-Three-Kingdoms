using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>全局静态：主公称号 1~100 级表</summary>
    [CreateAssetMenu(fileName = "LordCardStaticData",
                     menuName = "Game/Lord Card Static Data")]
    public class LordCardStaticData : ScriptableObject
    {
        public int currentMax;           // 当前最高等级
        public int valuePerSkillPoint;   // 一个技能点对应声望

        public List<LordCardLevel> levels = new();

        /// <summary>按等级获取数据（越界返回 null）</summary>
        public LordCardLevel GetLevel(int level) =>
            levels.Find(l => l.level == level);
    }
}
