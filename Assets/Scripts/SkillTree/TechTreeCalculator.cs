/******************************************************
 * TechTreeCalculator.cs – 2025-06-30
 * ----------------------------------------------------
 * 功能：根据当前阶段 & 进度字符串计算所有科技加成。
 * 关键改动：
 *   1. 任何数组访问前都核对下标，避免越界。
 *   2. 支持通过 Inspector 注入 TextAsset，也可回退
 *      StreamingAssets/techtree.json。
 *****************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class TechTreeCalculator
{
    /*──────── 可在 Inspector 中拖入 ────────*/
    public static TextAsset JsonFile;

    /*──────── 公共接口 ────────*/
    /// <summary>
    /// 计算 currentStage（含）以内的所有加成。
    /// progress 形如 "0120"（第 n 位 = 第 n 项当前等级）。
    /// </summary>
    public static Dictionary<string, float> Calc(int currentStage, string progress)
    {
        if (_tree == null) LoadTree();

        // 初始化结果
        var result = AllStatKeys.ToDictionary(k => k, _ => 0f);

        for (int stage = 1; stage <= currentStage; stage++)
        {
            if (!_tree.TryGetValue(stage, out var items)) continue;

            bool isCurrentStage = stage == currentStage;
            int  progIdx        = 0;

            foreach (var kv in items.OrderBy(k => k.Key))
            {
                var item      = kv.Value;
                int maxLvl    = item.MaxLevel;
                int levelChar = isCurrentStage ? CharLevel(progress, progIdx) : maxLvl;

                // 保险起见再和数据长度比一次
                int safeLevel = Mathf.Clamp(levelChar, 0,
                                            Math.Min(maxLvl, item.Values.Length));

                if (safeLevel > 0)
                    result[item.StatName] += item.Values[safeLevel - 1];

                progIdx++;
            }
        }
        return result;
    }

    /*──────── 内部结构 ────────*/
    class TechItem
    {
        public string  StatName;
        public int     MaxLevel;
        public float[] Values;
    }

    /*──────── 私有字段 ────────*/
    static Dictionary<int, Dictionary<int, TechItem>> _tree;

    static readonly string[] AllStatKeys =
    {
        "recruit_speed","money_speed","food_speed","mining_speed",
        "all_army_attack_bonus","damage_taken_reduction","soldier_damage_bonus","captain_damage_bonus",
        "cavalry_flatland_damage_bonus","ranged_forest_damage_bonus",
        "infantry_hill_damage_bonus","attack_clan_building_damage_bonus",
        "attack_map_building_damage_bonus","attack_player_camp_damage_bonus",
        "defend_clan_building_damage_bonus","defend_map_building_damage_bonus",
        "defend_camp_damage_bonus","all_captain_attack_power_bonus",
        "all_captain_defense_power_bonus","all_captain_wit_bonus",
        "army_speed_bonus","rally_speed_bonus","recall_speed_bonus",
        "army_cap_bonus","rally_cap_bonus","camp_garrison_cap_bonus",
        "camp_garrison_from_clam_cap_bonus","rally_summon_time_reduction",
        "food_consumption_reduction","money_consumption_reduction",
        "teleport_cost_reduction"
    };

    /*──────── JSON 解析 ────────*/
    static void LoadTree()
    {
        string json;

        if (JsonFile != null)                          // A) Inspector 注入
            json = JsonFile.text;
        else                                           // B) StreamingAssets
        {
            var path = Path.Combine(Application.streamingAssetsPath, "techtree.json");
#if UNITY_ANDROID && !UNITY_EDITOR
            var req = UnityEngine.Networking.UnityWebRequest.Get(path);
            req.SendWebRequest();
            while (!req.isDone) {}
            json = req.downloadHandler.text;
#else
            json = File.ReadAllText(path);
#endif
        }

        if (string.IsNullOrEmpty(json))
            throw new FileNotFoundException("未能加载 techtree.json。");

        JObject root = JObject.Parse(json);
        _tree = new Dictionary<int, Dictionary<int, TechItem>>();

        foreach (var stageProp in root.Properties())
        {
            if (!int.TryParse(stageProp.Name, out int stageNum)) continue;

            var itemMap  = new Dictionary<int, TechItem>();
            JObject obj  = (JObject)stageProp.Value;

            foreach (var itemProp in obj.Properties())
            {
                if (!int.TryParse(itemProp.Name, out int itemNum)) continue;

                // [statName, maxLvl, [costs], [values]]
                JArray arr = (JArray)itemProp.Value;
                if (arr.Count < 4) continue;

                var tech = new TechItem
                {
                    StatName = arr[0].ToString(),
                    MaxLevel = arr[1].ToObject<int>(),
                    Values   = arr[3].ToObject<float[]>()
                };
                itemMap[itemNum] = tech;
            }
            _tree[stageNum] = itemMap;
        }
    }

    /*──────── 小工具 ────────*/
    static int CharLevel(string progress, int index) =>
        (index >= 0 && index < progress?.Length)
        && char.IsDigit(progress[index]) ? progress[index] - '0' : 0;
}
