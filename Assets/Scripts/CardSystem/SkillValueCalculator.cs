// Assets/Scripts/Game/Core/SkillValueCalculator.cs
using System.Collections.Generic;
using UnityEngine;   // Mathf

namespace Game.Core
{
    /// <summary>
    /// 用于提供 Tier / Level 的倍率查表。
    /// 让 ActiveSkillDatabase、PassiveSkillDatabase 等 ScriptableObject 实现即可。
    /// </summary>
    public interface ISkillMultiplierProvider
    {
        /// <summary>S, A, B … → 倍率</summary>
        IReadOnlyDictionary<Tier, float> TierMultiplier { get; }

        /// <summary>1–4 → 倍率（通常映射到 0–3 星）</summary>
        IReadOnlyDictionary<int, float> LevelMultiplier { get; }
    }

    /// <summary>
    /// 技能数值计算工具：
    ///  - “基础 value” × 星级倍率 × 品阶倍率 × 100 ⇒ 百分比数值
    ///  - 可同时服务于主动 / 被动技能
    /// </summary>
    public static class SkillValueCalculator
    {
        /// <param name="baseValue">
        ///   技能条目里的 <c>value</c> 字段，通常是 0.1（代表 10 %）
        /// </param>
        /// <param name="info">
        ///   卡牌静态信息，用来取 <c>tier</c>
        /// </param>
        /// <param name="dyn">
        ///   玩家动态信息，用来取 <c>star</c>；传 <c>null</c> 表示未拥有，按 0 星算
        /// </param>
        /// <param name="provider">
        ///   实现了倍率表的数据库（Passive/ActiveSkillDatabase 均可）
        /// </param>
        /// <returns>最终百分比值（0–100 之间）</returns>
        public static float CalcPercent(
            float                 baseValue,
            CardInfoStatic        info,
            PlayerCard            dyn,
            ISkillMultiplierProvider provider)
        {
            if (info == null || provider == null) return 0f;

            /*── 1. 取星级对应的 LevelMultiplier ────────────────────────*/
            // 约定：0 星 → levelKey 1，1 星 → 2，2 星 → 3，≥3 星 → 4
            int star      = dyn?.star ?? 0;
            int levelKey  = Mathf.Clamp(star + 1, 1, 4);

            float levelMul = provider.LevelMultiplier.TryGetValue(levelKey, out var lm)
                           ? lm : 1f;

            /*── 2. 取品阶对应的 TierMultiplier ────────────────────────*/
            float tierMul  = provider.TierMultiplier.TryGetValue(info.tier, out var tm)
                           ? tm : 1f;

            /*── 3. 公式：基础 × 星级 × 品阶 × 100 → 百分比 ─────────────*/
            return baseValue * levelMul * tierMul * 100f;
        }
    }
}
