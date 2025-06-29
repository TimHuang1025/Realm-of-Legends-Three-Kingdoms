/******************************************************
 * TechTreeCalculator.cs
 *  ─────────────────────────────────────────
 *  用途：根据服务器下发
 *        { "current_stage": n, "progress": "abc..." }
 *        计算玩家的所有 player_tech_bonus。
 *
 *  方案 B：优先使用 Inspector 拖入的 TextAsset，
 *          若为空则退回读取 StreamingAssets/techtree.json
 *****************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;

public static class TechTreeCalculator
{
    /*──────── 通过 Inspector 注入 ────────*/
    public static TextAsset JsonFile;   // ← FancySkillTree 在 OnEnable 里赋值

    /*──────── 公共调用入口 ────────*/
    public static Dictionary<string, float> Calc(int currentStage, string progress)
    {
        if (_tree == null) LoadTree();

        // 初始化结果字典
        var result = new Dictionary<string, float>();
        foreach (string key in AllStatKeys) result[key] = 0f;

        // 累加各阶段
        for (int stage = 1; stage <= currentStage; stage++)
        {
            if (!_tree.TryGetValue(stage, out var items)) continue;

            bool isCurrent = stage == currentStage;
            int  idx       = 0;                              // progress 位序

            foreach (var kv in items.OrderBy(k => k.Key))
            {
                var item     = kv.Value;
                string stat  = item.StatName;
                int maxLevel = item.MaxLevel;

                int level = isCurrent ? CharLevel(progress, idx) : maxLevel;

                if (level > 0 && level <= maxLevel)
                    result[stat] += item.Values[level - 1]; // 取“本级数值”

                idx++;
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
        "all_army_attack_bonus","damage_taken_reduction","captain_damage_bonus",
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
        string json = null;

        // A) 优先使用 Inspector 拖进来的 TextAsset
        if (JsonFile != null)
        {
            json = JsonFile.text;
        }
        else
        {
            // B) 退回 StreamingAssets
            string path = Path.Combine(Application.streamingAssetsPath, "techtree.json");
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android 下需要用 UnityWebRequest
            var req = UnityEngine.Networking.UnityWebRequest.Get(path);
            req.SendWebRequest();
            while (!req.isDone) { }
            json = req.downloadHandler.text;
#else
            json = File.ReadAllText(path);
#endif
        }

        if (string.IsNullOrEmpty(json))
            throw new FileNotFoundException("无法加载 techtree.json：既未拖入 TextAsset，也未在 StreamingAssets 找到。");

        JObject root = JObject.Parse(json);
        _tree = new Dictionary<int, Dictionary<int, TechItem>>();

        foreach (var stageProp in root.Properties())
        {
            if (!int.TryParse(stageProp.Name, out int stageNum)) continue;

            var itemMap = new Dictionary<int, TechItem>();
            JObject stageObj = (JObject)stageProp.Value;

            foreach (var itemProp in stageObj.Properties())
            {
                if (!int.TryParse(itemProp.Name, out int itemNum)) continue;

                JArray arr = (JArray)itemProp.Value; // [statName, max, [costs], [values]]
                if (arr.Count < 4) continue;

                string statName = arr[0].ToString();
                int maxLvl      = arr[1].ToObject<int>();
                float[] values  = arr[3].ToObject<float[]>();

                itemMap[itemNum] = new TechItem
                {
                    StatName = statName,
                    MaxLevel = maxLvl,
                    Values   = values
                };
            }
            _tree[stageNum] = itemMap;
        }
    }

    /*──────── 工具函数 ────────*/
    static int CharLevel(string progress, int index)
    {
        if (index < 0 || index >= progress.Length) return 0;
        char c = progress[index];
        return (c >= '0' && c <= '9') ? c - '0' : 0;
    }
}
