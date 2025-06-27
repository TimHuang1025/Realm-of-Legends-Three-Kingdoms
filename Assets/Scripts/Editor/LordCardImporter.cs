#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Game;           // 引入上面的命名空间

public static class LordCardImporter
{
    [MenuItem("Tools/Import/Lord Card JSON")]
    private static void Import()
    {
        // 1. 选择文件
        string path = EditorUtility.OpenFilePanel("Select lord_card.json",
                                                  Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        // 2. 读取并解析
        var json = File.ReadAllText(path);
        var jo   = JObject.Parse(json);

        // 3. 创建 ScriptableObject
        var asset = ScriptableObject.CreateInstance<LordCardStaticData>();
        asset.currentMax         = jo["current_max"]?.Value<int>()        ?? 0;
        asset.valuePerSkillPoint = jo["value_per_skillPoint"]?.Value<int>() ?? 0;

        foreach (var kv in jo)
        {
            if (!int.TryParse(kv.Key, out int level)) continue; // 跳过 "format" 等键

            var arr = kv.Value as JArray;
            if (arr == null || arr.Count < 8) continue;

            var row = new LordCardLevel
            {
                level             = level,
                title             = arr[0].ToString(),
                requiredFame      = arr[1].Value<int>(),
                copperIncome      = arr[2].Value<int>(),
                addSoldierSymbol  = arr[3].Value<int>() == 1,
                baseSoldierCount  = arr[4].Value<int>(),
                rewardSoldier     = arr[5].Value<int>(),
                skillPoints       = arr[6].Value<int>(),
            };

            // 奖励列表：[ [code, qty], ... ]
            var rewardsArr = (JArray)arr[7];
            foreach (JArray r in rewardsArr)
            {
                row.rewards.Add(new LordCardReward
                {
                    itemCode = r[0].ToString(),
                    amount   = r[1].Value<int>()
                });
            }
            asset.levels.Add(row);
        }

        // 4. 保存 .asset
        const string savePath = "Assets/GameData/LordCardStaticData.asset";
        Directory.CreateDirectory(Path.GetDirectoryName(savePath));
        AssetDatabase.CreateAsset(asset, savePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ 导入成功，等级条目：{asset.levels.Count}");
        Selection.activeObject = asset;
    }
}
#endif
