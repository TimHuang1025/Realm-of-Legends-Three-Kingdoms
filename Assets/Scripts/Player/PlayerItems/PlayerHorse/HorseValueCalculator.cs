using UnityEngine;

namespace Game.Data
{
    /// <summary>根据品阶 & 等级计算战马属性和升级消耗</summary>
    public class HorseValueCalculator : ScriptableObject
    {
        public static HorseValueCalculator Instance
        {
            get
            {
                if (_inst == null) _inst = CreateInstance<HorseValueCalculator>();
                return _inst;
            }
        }
        private static HorseValueCalculator _inst;

        /*──────── 在 Importer 中填充 ────────*/
        public float coeValueS, coeValueA, coeValueB;
        public float powValueS, powValueA, powValueB;
        public float coeCostS,  coeCostA,  coeCostB;
        public float powCostS,  powCostA,  powCostB;

        /*──────── 公式计算 ────────*/
        public (float atk, float def, float intel) GetStat(
            HorseTier tier, float[] mulArr, int level)
        {
            var (coe, pow) = tier switch
            {
                HorseTier.S => (coeValueS, powValueS),
                HorseTier.A => (coeValueA, powValueA),
                _           => (coeValueB, powValueB)
            };

            float baseVal = coe * Mathf.Pow(level, pow);
            return (baseVal * mulArr[0],
                    baseVal * mulArr[1],
                    baseVal * mulArr[2]);
        }

        public int GetUpgradeCost(HorseTier tier, int level)
        {
            var (coe, pow) = tier switch
            {
                HorseTier.S => (coeCostS, powCostS),
                HorseTier.A => (coeCostA, powCostA),
                _           => (coeCostB, powCostB)
            };
            return Mathf.CeilToInt(coe * Mathf.Pow(level, pow));
        }
    }
}
