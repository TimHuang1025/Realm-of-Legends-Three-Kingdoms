using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public static class PassiveSkillImporter
{
    [MenuItem("Tools/SkillDB/Import Passive Skill JSON")]
    public static void Import()
    {
        var path = EditorUtility.OpenFilePanel("选择 passive_skill.json", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        // 1) 反序列化为字典
        var root = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(path));
        if (root == null) { Debug.LogError("解析失败"); return; }

        // 2) 全局字段
        root.TryGetValue("image_path", out var imgObj);
        var imagePath = ((string)imgObj)?.TrimEnd('/', '\\') + "/";

        // 3) 技能条目解析
        var list = new List<PassiveSkillInfo>();
        foreach (var (key, val) in root)
        {
            if (!key.StartsWith("P")) continue;           // 只处理编号 P1~Pn

            var raw = JsonConvert.DeserializeObject<RawSkill>(val.ToString());

            var info = new PassiveSkillInfo
            {
                id          = key,
                name        = raw.name,
                cnName      = raw.cn_name,
                timing      = (SkillTiming)int.Parse(raw.type),
                description = raw.Description,
                baseValue   = raw.value
            };

            // 4) 自动装载图标（可选）
            var iconPath = $"{imagePath}{key}.png";       // 建议命名：P1.png …
            info.iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

            list.Add(info);
        }

        if (list.Count == 0)
        {
            Debug.LogError("⚠️ 未找到任何 P 编号技能，检查 JSON");
            return;
        }

        // 5) 写入 ScriptableObject
        if (!(Selection.activeObject is PassiveSkillDatabase db))
        {
            Debug.LogError("请先选中 PassiveSkillDatabase.asset 再导入");
            return;
        }

        typeof(PassiveSkillDatabase)
            .GetField("skills", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(db, list);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ 被动技能导入完成：{list.Count} 条；图标目录：{imagePath}");
    }

    private class RawSkill
    {
        public string name;
        public string cn_name;
        public string type;
        public string Description;
        public float  value;
    }
}
