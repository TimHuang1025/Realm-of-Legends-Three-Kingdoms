// Assets/Editor/PassiveSkillImporter.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Game.Core;
#endif

public static class PassiveSkillImporter
{
#if UNITY_EDITOR
    [MenuItem("Tools/Import PassiveSkill JSON")]
    public static void Import()
    {
        /*── A. 让用户选 JSON ─────────────────────*/
        string jsonPath = EditorUtility.OpenFilePanel("选择被动技能 JSON", "Assets", "json");
        if (string.IsNullOrEmpty(jsonPath)) return;
        string jsonText = File.ReadAllText(jsonPath);
        var root = JObject.Parse(jsonText);

        /*── B. 找目标 PassiveSkillDatabase ───────*/
        PassiveSkillDatabase db = Selection.activeObject as PassiveSkillDatabase;
        if (db == null)
        {
            // 没有选中：询问是否创建
            if (EditorUtility.DisplayDialog("未选中 Database",
                "你没有在 Project 面板选中 PassiveSkillDatabase 资源。\n\n" +
                "要在 Resources 目录自动创建新的 DB 吗？",
                "创建新的", "取消") == false)
                return;

            const string dirPath   = "Assets/Resources";
            const string assetPath = "Assets/Resources/PassiveSkillDB.asset";

            if (!AssetDatabase.IsValidFolder(dirPath))
                AssetDatabase.CreateFolder("Assets", "Resources");

            db = AssetDatabase.LoadAssetAtPath<PassiveSkillDatabase>(assetPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<PassiveSkillDatabase>();
                AssetDatabase.CreateAsset(db, assetPath);
            }
        }

        /*── C. 解析倍率表 ────────────────────────*/
        var tierDict  = ParseFloatDict(root["tier_multiplier"]  as JObject);
        var levelDict = ParseIntFloatDict(root["level_multiplier"] as JObject);

        /*── D. 解析技能条目 ──────────────────────*/
        string imgPrefix = root["image_path"]?.Value<string>() ?? "";
        var skills = new List<PassiveSkillInfo>();
        foreach (var prop in root.Properties())
        {
            if (!prop.Name.StartsWith("P")) continue;
            var s = (JObject)prop.Value;

            skills.Add(new PassiveSkillInfo
            {
                id          = prop.Name,
                name        = s["name"]?.Value<string>(),
                cnName      = s["cn_name"]?.Value<string>(),
                timing      = (SkillTiming)(s["type"]?.Value<int>() ?? 0),
                description = s["Description"]?.Value<string>(),
                baseValue   = s["value"]?.Value<float>() ?? 0f,
                iconSprite  = LoadSprite(imgPrefix, prop.Name)
            });
        }

        /*── E. 写入并保存 ───────────────────────*/
        db.SetTierTable(tierDict);
        db.SetLevelTable(levelDict);
        db.SetSkillList(skills);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"已导入 {skills.Count} 条技能到 {AssetDatabase.GetAssetPath(db)}");
    }

    /*──────── 辅助函数 ────────*/
    static Dictionary<string,float> ParseFloatDict(JObject obj)
    {
        var dict = new Dictionary<string,float>();
        if (obj != null)
            foreach (var kv in obj)
                dict[kv.Key] = kv.Value.Value<float>();
        return dict;
    }
    static Dictionary<int,float> ParseIntFloatDict(JObject obj)
    {
        var dict = new Dictionary<int,float>();
        if (obj != null)
            foreach (var kv in obj)
                dict[int.Parse(kv.Key)] = kv.Value.Value<float>();
        return dict;
    }
    static Sprite LoadSprite(string dir, string id)
    {
        if (string.IsNullOrEmpty(dir)) return null;
        string assetPath = Path.Combine(dir, $"{id}.png")
                             .Replace(Application.dataPath, "Assets")
                             .Replace("\\", "/");
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        return tex ? Sprite.Create(tex,
                new Rect(0,0,tex.width,tex.height),
                new Vector2(0.5f,0.5f), 100) : null;
    }
#endif
}
