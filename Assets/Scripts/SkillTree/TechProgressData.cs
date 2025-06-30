/******************************************************
 * TechProgressData.cs
 * ----------------------------------------------------
 * 1. 总体进度：CurrentStage / Progress
 * 2. 行布局：StageLayouts   （首次随机后写入，保持 UI 不变）
 * 3. techtreeJson：可选拖入，供 TechTreeCalculator 使用
 * 4. RecalculateBonus：调用 TechTreeCalculator 刷新加成
 *****************************************************/
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "TechProgressData", menuName = "Game/Tech Progress Data")]
public sealed class TechProgressData : ScriptableObject
{
    [Header("引用 techtree.json（可选）")]
    public TextAsset techtreeJson;                   // 注入给 TechTreeCalculator

    [Header("总体信息")]
    public int    CurrentStage = 1;                  // 1 基
    public string Progress     = string.Empty;       // 如 "0133011003"

    [Header("行布局（首次生成后持久化）")]
    public List<StageRowLayout> StageLayouts = new();

    [Header("玩家科技加成 (读取时覆盖)")]
    public BonusData Bonus = new BonusData();

    /*──────────────── 行布局读写 ────────────────*/
    /// <summary>获取某阶段的行布局（行号 0-4，长度 = 小项数量）</summary>
    public List<int> GetRowLayout(string stageKey)
    {
        return StageLayouts.Find(x => x.StageKey == stageKey)?.Rows;
    }

    /// <summary>保存行布局；若已存在则覆盖</summary>
    public void SetRowLayout(string stageKey, List<int> rows)
    {
        var s = StageLayouts.Find(x => x.StageKey == stageKey);
        if (s == null)
            StageLayouts.Add(new StageRowLayout { StageKey = stageKey, Rows = rows });
        else
            s.Rows = rows;

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    /*──────────────── 计算加成 ────────────────*/
    public void RecalculateBonus()
    {
        if (techtreeJson != null) TechTreeCalculator.JsonFile = techtreeJson;

        var dict = TechTreeCalculator.Calc(CurrentStage, Progress);
        Bonus.FromDictionary(dict);
    }

#if UNITY_EDITOR
    private void OnValidate() => RecalculateBonus();
#endif
}

/* ---------- 可序列化行布局 ---------- */
[Serializable]
public sealed class StageRowLayout
{
    public string     StageKey;  // "1" "2" "3" …
    public List<int>  Rows;      // 每小项所在行索引 0-4
}
/*──────────────────── 数据结构 ────────────────────*/
[Serializable]
public sealed class BonusData
{
    public float recruit_speed;
    public float money_speed;
    public float food_speed;
    public float mining_speed;
    public float all_army_attack_bonus;
    public float damage_taken_reduction;
    public float soldier_damage_bonus;
    public float captain_damage_bonus;
    public float cavalry_flatland_damage_bonus;
    public float ranged_forest_damage_bonus;
    public float infantry_hill_damage_bonus;
    public float attack_clan_building_damage_bonus;
    public float attack_map_building_damage_bonus;
    public float attack_player_camp_damage_bonus;
    public float defend_clan_building_damage_bonus;
    public float defend_map_building_damage_bonus;
    public float defend_camp_damage_bonus;
    public float all_captain_attack_power_bonus;
    public float all_captain_defense_power_bonus;
    public float all_captain_wit_bonus;
    public float army_speed_bonus;
    public float rally_speed_bonus;
    public float recall_speed_bonus;
    public float army_cap_bonus;
    public float rally_cap_bonus;
    public float camp_garrison_cap_bonus;
    public float camp_garrison_from_clam_cap_bonus;
    public float rally_summon_time_reduction;
    public float food_consumption_reduction;
    public float money_consumption_reduction;
    public float teleport_cost_reduction;

    public void FromDictionary(Dictionary<string, float> d)
    {
        recruit_speed                      = d["recruit_speed"];
        money_speed                        = d["money_speed"];
        food_speed                         = d["food_speed"];
        mining_speed                       = d["mining_speed"];
        all_army_attack_bonus              = d["all_army_attack_bonus"];
        damage_taken_reduction             = d["damage_taken_reduction"];
        soldier_damage_bonus               = d["soldier_damage_bonus"];
        captain_damage_bonus               = d["captain_damage_bonus"];
        cavalry_flatland_damage_bonus      = d["cavalry_flatland_damage_bonus"];
        ranged_forest_damage_bonus         = d["ranged_forest_damage_bonus"];
        infantry_hill_damage_bonus         = d["infantry_hill_damage_bonus"];
        attack_clan_building_damage_bonus  = d["attack_clan_building_damage_bonus"];
        attack_map_building_damage_bonus   = d["attack_map_building_damage_bonus"];
        attack_player_camp_damage_bonus    = d["attack_player_camp_damage_bonus"];
        defend_clan_building_damage_bonus  = d["defend_clan_building_damage_bonus"];
        defend_map_building_damage_bonus   = d["defend_map_building_damage_bonus"];
        defend_camp_damage_bonus           = d["defend_camp_damage_bonus"];
        all_captain_attack_power_bonus     = d["all_captain_attack_power_bonus"];
        all_captain_defense_power_bonus    = d["all_captain_defense_power_bonus"];
        all_captain_wit_bonus              = d["all_captain_wit_bonus"];
        army_speed_bonus                   = d["army_speed_bonus"];
        rally_speed_bonus                  = d["rally_speed_bonus"];
        recall_speed_bonus                 = d["recall_speed_bonus"];
        army_cap_bonus                     = d["army_cap_bonus"];
        rally_cap_bonus                    = d["rally_cap_bonus"];
        camp_garrison_cap_bonus            = d["camp_garrison_cap_bonus"];
        camp_garrison_from_clam_cap_bonus  = d["camp_garrison_from_clam_cap_bonus"];
        rally_summon_time_reduction        = d["rally_summon_time_reduction"];
        food_consumption_reduction         = d["food_consumption_reduction"];
        money_consumption_reduction        = d["money_consumption_reduction"];
        teleport_cost_reduction            = d["teleport_cost_reduction"];
    }
}
