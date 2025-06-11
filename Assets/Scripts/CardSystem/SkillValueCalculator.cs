using System.Collections.Generic;
using UnityEngine;
using Game.Data;           // CardDatabaseStatic, Tier

namespace Game.Core
{
    public interface ISkillMultiplierProvider
    {
        IReadOnlyDictionary<Tier, float> TierMultiplier  { get; }
        IReadOnlyDictionary<int,  float> LevelMultiplier { get; }
    }

    public static class SkillValueCalculator
    {
        static CardDatabaseStatic _db;
        static CardDatabaseStatic DB =>
            _db ??= Resources.Load<CardDatabaseStatic>("CardDatabaseStatic");

        /* 你原来写的等级→属性函数都可以留着，
           但要再加上真正的 CalcPercent ↓ */

        /// <summary>base × 等级倍率 × 品阶倍率 × 星级倍率 ×100</summary>
        public static float CalcPercent(
            float baseValue,
            CardInfoStatic info,
            PlayerCard dyn,
            ISkillMultiplierProvider provider,
            int skillLv)                      // 1‒5
        {
            if (info == null || provider == null) return 0f;

            float lvMul   = provider.LevelMultiplier.TryGetValue(skillLv, out var lm) ? lm : 1f;
            float tierMul = DB.GetTierMultiplier(info.tier);
            float starMul = GetStarMul(dyn?.star ?? 0);

            return baseValue * lvMul * tierMul * starMul * 100f;
        }

        /* 兼容旧代码：默认技能等级 1 */
        public static float CalcPercent(
        float baseVal, CardInfoStatic info, float lvMul)
        {
            float tierMul = DB.GetTierMultiplier(info.tier);
            return baseVal * lvMul * tierMul * 100f;
        }


        static float GetStarMul(int star) => 1f;
    }
}
