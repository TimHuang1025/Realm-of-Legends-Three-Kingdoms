// Assets/Editor/GearDatabaseImporter.cs
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Game.Data;

// 别名：始终指向 Game.Data 下的枚举，避免重名冲突
using GDTier = Game.Data.Tier;
using GDKind = Game.Data.GearKind;

public static class GearDatabaseImporter
{
    // ─────────────────────────────────────────────────────────────
    // ① 菜单栏：Tools → Import Gear JSON…（优先导入到当前选中资产）
    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Import Gear JSON…")]
    private static void ImportGearJson_Global()
    {
        // 若 Project 面板已选中 GearStaticDB.asset，就直接用它
        var target = Selection.activeObject as GearDatabaseStatic;

        // 让用户选择 JSON 文件
        string jsonPath = EditorUtility.OpenFilePanel("选择 gear.json", Application.dataPath, "json");
        if (string.IsNullOrEmpty(jsonPath)) return;

        // 解析 JSON → 得到临时数据库
        if (!TryBuildFromJson(jsonPath, out var tempDB)) return;

        // 若未选中资产，则 fallback 到 Resources/GearStaticDB.asset
        if (target == null)
        {
            string dir = "Assets/Resources";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string assetPath = $"{dir}/GearStaticDB.asset";
            target = AssetDatabase.LoadAssetAtPath<GearDatabaseStatic>(assetPath);
            if (target == null)
            {
                target = ScriptableObject.CreateInstance<GearDatabaseStatic>();
                AssetDatabase.CreateAsset(target, assetPath);
                Debug.Log($"[GearImporter] 未选中资产，已在 {assetPath} 新建默认文件。");
            }
        }

        // 把临时数据库内容复制进目标 asset
        CopyDatabase(tempDB, target);
        target.Get(""); 
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ JSON 已导入 → {AssetDatabase.GetAssetPath(target)}");
    }

    // ─────────────────────────────────────────────────────────────
    // ② 右键：Assets → Import Gear JSON to Selected…（仅导入到选中的）
    // ─────────────────────────────────────────────────────────────
    [MenuItem("Assets/Import Gear JSON to Selected…", false, 1000)]
    private static void ImportGearJson_ToSelected()
    {
        var selected = Selection.activeObject as GearDatabaseStatic;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("No GearStaticDB Selected",
                "请先在 Project 面板选中一个 GearStaticDB.asset，再执行导入。", "OK");
            return;
        }

        string jsonPath = EditorUtility.OpenFilePanel("选择 gear.json", Application.dataPath, "json");
        if (string.IsNullOrEmpty(jsonPath)) return;

        if (!TryBuildFromJson(jsonPath, out var tempDB)) return;

        CopyDatabase(tempDB, selected);
        EditorUtility.SetDirty(selected);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ JSON 已导入 → {AssetDatabase.GetAssetPath(selected)}");
    }

    // ─────────────────────────────────────────────────────────────
    // 共享：从 JSON 构建临时 GearDatabaseStatic
    // ─────────────────────────────────────────────────────────────
    private static bool TryBuildFromJson(string jsonPath, out GearDatabaseStatic result)
    {
        result = null;

        string json = File.ReadAllText(jsonPath);
        JObject root;
        try { root = JObject.Parse(json); }
        catch (Exception e)
        {
            Debug.LogError($"[GearImporter] 解析 JSON 失败：{e.Message}");
            return false;
        }

        // 1) 解析公式字典
        var valCoe  = ReadTierDict(root["value_coe"]);
        var valPow  = ReadTierDict(root["value_pow"]);
        var costCoe = ReadTierDict(root["iron_cost_coe"]);
        var costPow = ReadTierDict(root["iron_cost_pow"]);

        // 2) 解析各条目
        var gears = new List<GearStatic>();
        foreach (var kv in root.Properties())
        {
            if (!kv.Name.StartsWith("weapon_") && !kv.Name.StartsWith("armor_")) continue;

            JObject o = (JObject)kv.Value;
            try
            {
                gears.Add(new GearStatic
                {
                    id              = o["id"].Value<string>(),
                    name            = o["name"].Value<string>(),
                    kind            = (GDKind)Enum.Parse(typeof(GDKind),  o["type"].Value<string>(), true),
                    tier            = (GDTier)Enum.Parse(typeof(GDTier), o["tier"].Value<string>(), true),
                    valueMultiplier = o["value_multiplier"].Select(v => v.Value<float>()).ToArray()
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GearImporter] 条目解析失败 ({kv.Name})：{ex.Message}");
            }
        }

        // 3) 生成临时 ScriptableObject
        var temp = ScriptableObject.CreateInstance<GearDatabaseStatic>();
        CopyDatabaseFields(temp, gears, valCoe, valPow, costCoe, costPow);
        result = temp;
        return true;
    }

    // ─────────────────────────────────────────────────────────────
    // 复制字段：src → dst  (私有序列化字段用反射赋值)
    // ─────────────────────────────────────────────────────────────
    private static void CopyDatabase(GearDatabaseStatic src, GearDatabaseStatic dst)
    {
        void Copy(string field)
        {
            var f = typeof(GearDatabaseStatic).GetField(field,
                        BindingFlags.NonPublic | BindingFlags.Instance);
            f.SetValue(dst, f.GetValue(src));
        }
        Copy("gears");
        Copy("valueCoeArr");
        Copy("valuePowArr");
        Copy("ironCostCoeArr");
        Copy("ironCostPowArr");
    }

    private static void CopyDatabaseFields(
        GearDatabaseStatic db,
        List<GearStatic> gears,
        Dictionary<GDTier,float> valCoe,
        Dictionary<GDTier,float> valPow,
        Dictionary<GDTier,float> costCoe,
        Dictionary<GDTier,float> costPow)
    {
        void Set(string field, object value) =>
            typeof(GearDatabaseStatic).GetField(field,
                BindingFlags.NonPublic | BindingFlags.Instance).SetValue(db, value);

        Set("gears",           gears);
        Set("valueCoeArr",     ToPairArray(valCoe));
        Set("valuePowArr",     ToPairArray(valPow));
        Set("ironCostCoeArr",  ToPairArray(costCoe));
        Set("ironCostPowArr",  ToPairArray(costPow));
    }

    // ─────────────────────────────────────────────────────────────
    // 辅助：解析 Tier→float 字典 / 转为 TierFloatPair[]
    // ─────────────────────────────────────────────────────────────
    private static Dictionary<GDTier, float> ReadTierDict(JToken token)
    {
        var dict = new Dictionary<GDTier, float>();
        if (token == null) return dict;

        foreach (var p in token.Children<JProperty>())
        {
            if (!Enum.TryParse<GDTier>(p.Name, true, out var tier))
            {
                Debug.LogWarning($"[GearImporter] 未识别的 Tier \"{p.Name}\"，已跳过。");
                continue;
            }
            dict[tier] = p.Value.Value<float>();
        }
        return dict;
    }

    private static Array ToPairArray(Dictionary<GDTier, float> src)
    {
        var pairType = typeof(GearDatabaseStatic.TierFloatPair);
        Array arr = Array.CreateInstance(pairType, src.Count);
        int i = 0;
        foreach (var kv in src)
        {
            var inst = Activator.CreateInstance(pairType);
            pairType.GetField("tier").SetValue(inst, kv.Key);
            pairType.GetField("value").SetValue(inst, kv.Value);
            arr.SetValue(inst, i++);
        }
        return arr;
    }
}
