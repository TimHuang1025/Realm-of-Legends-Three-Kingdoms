using System;
using UnityEngine;     // Mathf
using Game.Core;      // Stats4

public static class LevelStatCalculator
{
    /*──────────── 常量 ───────────*/
    public const int MaxLevel = 200;   // 若有上限，外部可用

    /*──────────── 内部工具 ───────*/
    static float GetTierMultiplier(Tier tier) => tier switch
    {
        Tier.S => 1f,
        Tier.A => 0.8f,
        Tier.B => 0.5f,
        _      => 1f
    };

    /// <summary>真正的曲线计算核心</summary>
    static Stats4 CalcStatsCore(int level, Tier tier, float[] mul)
    {
        level = Mathf.Max(level, 1);

        // ▸ (1) 等级 → 基础值：
        //    ≤100 走立方曲线，>100 走指数衰减段
        double baseVal = level <= 100
            ? Math.Ceiling(0.2 * Math.Pow(level, 3))
            : 200_000 + Math.Ceiling(650 * level + 81_250
                       - 7_984_980 * Math.Exp(-0.04 * level));

        baseVal *= GetTierMultiplier(tier);

        // ▸ (2) 专属倍率（数组不足时补 1）
        float mAtk = mul.Length > 0 ? mul[0] : 1f;
        float mDef = mul.Length > 1 ? mul[1] : 1f;
        float mInt = mul.Length > 2 ? mul[2] : 1f;
        float mCmd = mul.Length > 3 ? mul[3] : 1f;

        return new Stats4(
            Mathf.CeilToInt((float)(baseVal * mAtk)),
            Mathf.CeilToInt((float)(baseVal * mDef)),
            Mathf.CeilToInt((float)(baseVal * mInt)),
            Mathf.CeilToInt((float)(baseVal * mCmd))
        );
    }

    /*──────────── 公共 API ───────*/

    /// <summary>当前等级四维属性</summary>
    public static Stats4 CalculateStats(CardInfoStatic info, PlayerCard dyn)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));

        int level = dyn?.level ?? 1;
        return CalcStatsCore(level, info.tier, info.base_value_multiplier);
    }

    /// <summary>下一等级 − 当前等级 的增量</summary>
    public static Stats4 CalculateDeltaNextLevel(CardInfoStatic info, PlayerCard dyn)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));

        int level = dyn?.level ?? 1;

        var cur  = CalcStatsCore(level,     info.tier, info.base_value_multiplier);
        var next = CalcStatsCore(level + 1, info.tier, info.base_value_multiplier);

        return new Stats4(
            next.Atk - cur.Atk,
            next.Def - cur.Def,
            next.Int - cur.Int,
            next.Cmd - cur.Cmd
        );
    }

    /// <summary>升级到 <paramref name="level"/> → <c>level + 1</c> 所需 (exp, extraMat)</summary>
    public static (int exp, int extraMat) GetUpgradeCost(int level)
    {
        if (level < 1) throw new ArgumentException("等级必须 ≥ 1");

        int expNeeded = level <= 99
            ? Mathf.CeilToInt(0.6f * level * level)   // 前 99 级：二次曲线
            : 60_000 + Mathf.CeilToInt((level - 100) / 10f) * 5_000;

        int extraMaterial = (level % 2 == 0) ? 1_000 : 0;    // 偶数级需额外材料
        return (expNeeded, extraMaterial);
    }
}
