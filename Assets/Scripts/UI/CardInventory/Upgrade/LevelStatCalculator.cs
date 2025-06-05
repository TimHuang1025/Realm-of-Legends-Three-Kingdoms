using System;
using UnityEngine;

/// <summary>
/// 等级-属性 / 升级消耗 统一计算器
/// 不挂场景，纯静态工具类
/// </summary>
public static class LevelStatCalculator
{
    /* ───────── 常量 ───────── */

    // 三维倍率顺序：Atk / Def / Int
    private static readonly float[] BaseValueMultiplier = { 1.4f, 1.3f, 0.3f };

    /* ───────── 私有工具 ───────── */

    private static float GetTierMultiplier(string quality)
    {
        switch (quality)
        {
            case "S": return 1f;
            case "A": return 0.8f;
            case "B": return 0.5f;
            default : return 1f;      // 未知品质，默认 1 倍
        }
    }

    /* ───────── 属性计算 ───────── */

    /// <summary>直接从 <paramref name="card"/> 计算三围</summary>
    public static (int atk, int def, int intel) CalculateStats(CardInfo card)
    {
        if (card == null) throw new ArgumentNullException(nameof(card));
        return CalculateStatsAtLevel(card.level, card.quality);
    }

    /// <summary>按 <paramref name="level"/> + <paramref name="quality"/> 计算三围</summary>
    public static (int atk, int def, int intel) CalculateStatsAtLevel(int level, string quality)
    {
        level = Mathf.Max(level, 1);
        double baseVal;

        // ① 基础值
        if (level <= 100)
            baseVal = Math.Ceiling(0.2 * Math.Pow(level, 3));
        else
            baseVal = 200_000 +
                      Math.Ceiling(650 * level + 81_250 - 7_984_980 * Math.Exp(-0.04 * level));

        // ② 品质倍率
        baseVal *= GetTierMultiplier(quality);

        // ③ 三围
        int atk   = Mathf.CeilToInt((float)(baseVal * BaseValueMultiplier[0]));
        int def   = Mathf.CeilToInt((float)(baseVal * BaseValueMultiplier[1]));
        int intel = Mathf.CeilToInt((float)(baseVal * BaseValueMultiplier[2]));

        return (atk, def, intel);
    }

    /// <summary>返回“下一级 − 当前级”三围差值</summary>
    public static (int dAtk, int dDef, int dIntel) CalculateDeltaNextLevel(CardInfo card)
    {
        if (card == null) throw new ArgumentNullException(nameof(card));

        var now  = CalculateStatsAtLevel(card.level,     card.quality);
        var next = CalculateStatsAtLevel(card.level + 1, card.quality);

        return (next.atk   - now.atk,
                next.def   - now.def,
                next.intel - now.intel);
    }

    /* ───────── 升级消耗 ───────── */

    /// <summary>
    /// 返回升级到下一级所需 (exp, extraMat)。<br/>
    /// · exp     : 经验值<br/>
    /// · extraMat: 当等级为偶数时=1000，否则 0
    /// </summary>
    public static (int exp, int extraMat) GetUpgradeCost(int level)
    {
        if (level < 1) throw new ArgumentException("等级必须 ≥ 1");

        // 经验值
        int expNeeded = (level <= 99)
            ? Mathf.CeilToInt(0.6f * level * level)
            : 60_000 + Mathf.CeilToInt((level - 100) / 10f) * 5_000;

        // 偶数等级额外材料
        int extraMaterial = (level % 2 == 0) ? 1_000 : 0;

        return (expNeeded, extraMaterial);
    }
}
