// Assets/Scripts/Game/Core/LevelStatCalculator.cs
using System;
using UnityEngine;          // Mathf, Resources
using Game.Core;            // Stats4
using Game.Data;            // CardDatabaseStatic, StarRule

public static class LevelStatCalculator
{
    /*──────── 常量 ────────*/
    public const int MaxLevel = 200;

    /*──────── 数据库 ─────*/
    static CardDatabaseStatic DB => _db ??= Resources.Load<CardDatabaseStatic>("CardDatabaseStatic");
    static CardDatabaseStatic _db;

    /*──────── 内部工具 ───*/
    static float GetTierMul(Tier t) => t switch
    {
        Tier.S => 1f,
        Tier.A => 0.8f,
        Tier.B => 0.5f,
        _      => 1f
    };

    static int GetStarBonus(int star) => DB?.GetStar(star)?.battlePowerAdd ?? 0;

    /*──────── 属性核心 ───*/

    /// <summary>等级对应的基础值 f(level)</summary>
    static double BaseStatCurve(int lv)
    {
        return lv <= 100
             ? Math.Ceiling(0.2 * Math.Pow(lv, 3))
             : 200_000 + Math.Ceiling(650 * lv + 81_250
                          - 7_984_980 * Math.Exp(-0.04 * lv));
    }

    /// <summary>统御(带兵)基础值 g(level)</summary>
    static double CommandCurve(int lv)
    {
        return lv <= 100
             ? Math.Ceiling(0.09333 * Math.Pow(lv, 3)
                            + 0.14   * Math.Pow(lv, 2)
                            + 0.04667* lv)
             : Math.Ceiling(130 * lv + 150_450
                            - 68_750 * Math.Exp(-0.04 * lv) + 4);
    }

    static Stats4 CalcStatsCore(int lv, Tier tier, float[] mul, int star)
    {
        lv   = Mathf.Max(lv, 1);
        var  baseVal     = BaseStatCurve(lv);
        var  cmdBaseVal  = CommandCurve(lv);
        var  tierMul     = GetTierMul(tier);
        var  mAtk = mul.Length > 0 ? mul[0] : 1f;
        var  mDef = mul.Length > 1 ? mul[1] : 1f;
        var  mInt = mul.Length > 2 ? mul[2] : 1f;
        var  mCmd = mul.Length > 3 ? mul[3] : 1f;

        int starBonus = GetStarBonus(star);

        int atk = Mathf.CeilToInt( starBonus + (float)(baseVal * mAtk * tierMul) );
        int def = Mathf.CeilToInt( starBonus + (float)(baseVal * mDef * tierMul) );
        int iq  = Mathf.CeilToInt( starBonus + (float)(baseVal * mInt * tierMul) );
        int cmd = Mathf.CeilToInt( (float)(cmdBaseVal * mCmd * tierMul) );   // 统御无星级加成

        return new Stats4(atk, def, iq, cmd);
    }

    /*──────── Public API ───*/

    /// <summary>当前等级四维</summary>
    public static Stats4 CalculateStats(CardInfoStatic info, PlayerCard dyn)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        int lv   = dyn?.level ?? 1;
        int star = dyn?.star  ?? 0;
        return CalcStatsCore(lv, info.tier, info.base_value_multiplier, star);
    }

    /// <summary>下一等级 - 当前等级 的增量</summary>
    public static Stats4 CalculateDeltaNextLevel(CardInfoStatic info, PlayerCard dyn)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        int lv   = dyn?.level ?? 1;
        int star = dyn?.star  ?? 0;

        var cur  = CalcStatsCore(lv,     info.tier, info.base_value_multiplier, star);
        var next = CalcStatsCore(lv + 1, info.tier, info.base_value_multiplier, star);

        return new Stats4(
            next.Atk - cur.Atk,
            next.Def - cur.Def,
            next.Int - cur.Int,
            next.Cmd - cur.Cmd
        );
    }

    /// <summary>升级到 lv → lv+1 所需 (exp, extraMat)</summary>
    public static (int exp, int extraMat) GetUpgradeCost(int lv)
    {
        if (lv < 1) throw new ArgumentException("等级必须 ≥ 1");

        int exp = lv <= 99
            ? Mathf.CeilToInt(0.6f * lv * lv)
            : 60_000 + Mathf.CeilToInt((lv - 100) / 10f) * 5_000;

        int extraMat = lv % 10 == 0 ? lv * 2 : 0;   // 每 10 级一次
        return (exp, extraMat);
    }

    /// <summary>仅获取统御值</summary>
    public static int CalculateCommand(CardInfoStatic info, PlayerCard dyn)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        int lv = dyn?.level ?? 1;
        int star = dyn?.star ?? 0;   // star 没用，但保持签名一致
        return CalcStatsCore(lv, info.tier, info.base_value_multiplier, star).Cmd;
    }

    /// <summary>战力=Atk+Def+Int+Cmd</summary>
    public static int CalculateBattlePower(CardInfoStatic info, PlayerCard dyn)
    {
        var s = CalculateStats(info, dyn);
        return s.Atk + s.Def + s.Int + s.Cmd;
    }
}
