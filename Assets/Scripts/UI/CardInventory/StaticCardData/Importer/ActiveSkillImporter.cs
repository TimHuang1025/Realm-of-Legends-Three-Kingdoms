// Assets/Editor/ActiveSkillImporter.cs
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public static class ActiveSkillImporter
{
    [MenuItem("Tools/SkillDB/Import Active Skill JSON")]
    public static void Import()
    {
        /*── 1. 选 JSON ───────────────────────────*/
        var path = EditorUtility.OpenFilePanel(
            "选择 active_skill.json", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        var root = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                      File.ReadAllText(path));
        if (root == null) { Debug.LogError("解析失败"); return; }

        /*── 2. 全局字段 ───────────────────────────*/
        root.TryGetValue("image_path", out var imgObj);
        var imagePath = ((string?)imgObj)?.TrimEnd('/', '\\') + "/";

        var tierMult  = ReadDictStringFloat(root, "tier_multiplier"); // S/A/B…
        var levelMult = ReadDictIntFloat   (root, "level_multiplier"); // 1‒5

        /*── 3. 技能条目列表 ───────────────────────*/
        var list = new List<ActiveSkillInfo>();
        foreach (var (key, val) in root)
        {
            if (!key.StartsWith("A")) continue;           // 只处理 A1~An

            var raw = JsonConvert.DeserializeObject<RawSkill>(val.ToString());
            if (raw == null) continue;

            var info = new ActiveSkillInfo
            {
                id          = key,
                name        = raw.name,
                cnName      = raw.cn_name,
                description = raw.Description,
                coefficient = raw.coefficient,
                iconSprite  = AssetDatabase.LoadAssetAtPath<Sprite>($"{imagePath}{key}.png")
            };
            list.Add(info);
        }

        if (list.Count == 0)
        {
            Debug.LogError("⚠️ 未找到任何 A 编号技能，检查 JSON");
            return;
        }

        /*── 4. 选中目标 asset ─────────────────────*/
        if (!(Selection.activeObject is ActiveSkillDatabase db))
        {
            Debug.LogError("请先在 Project 选中 ActiveSkillDatabase.asset 再导入");
            return;
        }

        /*── 5. 写入技能条目 (私有 List) ─────────────*/
        typeof(ActiveSkillDatabase)
            .GetField("skills",
                      System.Reflection.BindingFlags.NonPublic |
                      System.Reflection.BindingFlags.Instance)!
            .SetValue(db, list);

        /*── 6. 写入倍率表 (Tier / Level) ───────────*/
        db.tierTable.Clear();
        foreach (var (k, v) in tierMult)
            db.tierTable.Add(new ActiveSkillDatabase.TierEntry { key = k, value = v });

        db.levelTable.Clear();
        foreach (var (k, v) in levelMult)
            db.levelTable.Add(new ActiveSkillDatabase.LevelEntry { key = k, value = v });

        /*── 7. 重建运行时缓存 ─────────────────────*/
        db.RebuildMultiplierCache();

        /*── 8. 保存 ─────────────────────────────*/
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ 主动技能导入完成：{list.Count} 条；图标目录：{imagePath}");
    }

    /*──────── 私有工具 ──────────────────────────*/
    static Dictionary<string,float> ReadDictStringFloat(
        Dictionary<string,object> root,string key)
    {
        if (!root.TryGetValue(key, out var obj)) return new();
        return JsonConvert.DeserializeObject<Dictionary<string,float>>(obj.ToString()) ?? new();
    }

    static Dictionary<int,float> ReadDictIntFloat(
        Dictionary<string,object> root,string key)
    {
        var dict = new Dictionary<int,float>();
        if (root.TryGetValue(key, out var obj))
        {
            var tmp = JsonConvert.DeserializeObject<Dictionary<string,float>>(obj.ToString());
            if (tmp != null)
                foreach (var (k,v) in tmp) dict[int.Parse(k)] = v;
        }
        return dict;
    }

    /*──────── JSON 内部字段 ─────────────────────*/
    private class RawSkill
    {
        public string name;
        public string cn_name;
        public string Description;
        public float  coefficient;
    }
}
