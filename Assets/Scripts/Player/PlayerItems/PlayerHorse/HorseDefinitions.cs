using System;
using UnityEngine;

namespace Game.Data
{
    public enum HorseTier { S, A, B }

    [Serializable]
    public class HorseStatic
    {
        [Header("标识")]
        public string id;
        public string name;
        public HorseTier tier;

        [Header("数值倍率 (Atk / Def / Cmd)")]
        public float[] valueMultiplier = new float[3];

        [Header("图标")]
        public Sprite iconSprite;

        /*──────── 公式 ────────*/
        private static HorseDatabaseStatic DB => HorseDatabaseStatic.Instance;

        public (float atk, float def, float cmd) CalcStats(int level)
        {
            float coe = DB.GetValueCoe(tier);
            float pow = DB.GetValuePow(tier);
            float baseVal = coe * Mathf.Pow(level, pow);

            return (baseVal * valueMultiplier[0],
                    baseVal * valueMultiplier[1],
                    baseVal * valueMultiplier[2]);
        }

        public int CalcUpgradeCost(int level)
        {
            float coe = DB.GetCostCoe(tier);
            float pow = DB.GetCostPow(tier);
            return Mathf.CeilToInt(coe * Mathf.Pow(level, pow));
        }
    }

    /*──────── 动态条目 ────────*/
    [Serializable]
    public class PlayerHorse
    {
        public string uuid = "";
        public string staticId;
        public int level = 1;
        public bool unlocked = true;
        public string equippedById = "";     // 为空 = 未装备

        public void EnsureUuid()
        {
            if (string.IsNullOrEmpty(uuid))
                uuid = Guid.NewGuid().ToString("N");
        }

        public HorseStatic Static => HorseDatabaseStatic.Instance.Get(staticId);
    }
}
