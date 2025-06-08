// Assets/Scripts/Game/Data/GearDefinitions.cs
using System;
using UnityEngine;

namespace Game.Data
{
    /*───────── 基础枚举 ─────────*/
    public enum Tier
    {
        S1, S2, S3, S4, S5,
        A1, A2, A3, A4, A5,
        B1, B2, B3, B4, B5
    }

    public enum GearKind { Weapon, Armor }

    /*───────── 静态条目 ─────────*/
    [Serializable]
    public class GearStatic
    {
        [Header("标识")]
        public string   id;
        public string   name;
        public GearKind kind;
        public Tier     tier;

        [Header("数值倍率 (Atk / Def)")]
        public float[]  valueMultiplier = new float[2];

        [Header("图标")]
        public Sprite   iconSprite;

        /*──── 公式计算 ────*/
        // 获取单例引用仅走一次
        private static GearDatabaseStatic DB => GearDatabaseStatic.Instance;

        /// <summary>当前等级的攻击 / 防御</summary>
        public (float atk, float def) CalcStats(int level)
        {
            // 基础数值
            float coe = DB.GetValueCoe(tier);
            float pow = DB.GetValuePow(tier);
            float statBase = coe * Mathf.Pow(level, pow);

            return (statBase * valueMultiplier[0],
                    statBase * valueMultiplier[1]);
        }

        /// <summary>升级到该等级所需材料 (铁, 精木, 精钢)</summary>
        public (int iron,        // 任何等级都需要
                int fineWood,    // 10 级及以上才会大于 0
                int steel)       // 20 级及以上才会大于 0
            CalcUpgradeCost(int level)
        {
            float iron  = DB.GetIronCostCoe(tier)      * Mathf.Pow(level, DB.GetIronCostPow(tier));
            float wood  = level >= 10
                        ? DB.GetFineWoodCoe(tier)      * Mathf.Pow(level, DB.GetFineWoodPow(tier))
                        : 0;
            float steel = level >= 20
                        ? DB.GetSteelCoe(tier)         * Mathf.Pow(level, DB.GetSteelPow(tier))
                        : 0;

            // 向上取整并返回
            return (Mathf.CeilToInt(iron),
                    Mathf.CeilToInt(wood),
                    Mathf.CeilToInt(steel));
        }
    }

    /*───────── 动态背包条目 ─────────*/
    [Serializable]
    public class PlayerGear
    {
        public string staticId;
        public int    level = 1;
        public bool   unlocked = true;

        public GearStatic Static => GearDatabaseStatic.Instance.Get(staticId);
    }
}
