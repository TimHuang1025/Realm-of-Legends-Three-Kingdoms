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
        /*── 1. 选文件 ──*/
        var path = EditorUtility.OpenFilePanel("选择 active_skill.json", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;
        var json = File.ReadAllText(path);

        /*── 2. 先反序列化为通用字典 ──*/
        var root = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        if (root == null) { Debug.LogError("解析失败"); return; }

        /*── 3. 读取全局字段 ──*/
        root.TryGetValue("image_path", out var imgObj);
        var imagePath = ((string?)imgObj)?.TrimEnd('/', '\\') + "/";

        var tierMult  = ReadDictStringFloat(root, "tier_multiplier");
        var levelMult = ReadDictIntFloat(root, "level_multiplier");

        /*── 4. 解析技能条目 ──*/
        var list = new List<ActiveSkillInfo>();
        foreach (var (key, val) in root)
        {
            if (!key.StartsWith("A")) continue;          // 只处理 A1~An

            var raw = JsonConvert.DeserializeObject<RawSkill>(val.ToString());

            var info = new ActiveSkillInfo
            {
                id          = key,
                name        = raw.name,
                cnName      = raw.cn_name,
                description = raw.Description,
                coefficient = raw.coefficient
            };

            /* 自动挂图标：Assets/.../Skills/A1.png */
            var iconPath = $"{imagePath}{key}.png";
            info.iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

            list.Add(info);
        }

        if (list.Count == 0)
        {
            Debug.LogError("⚠️ 未找到任何 A 编号技能，检查 JSON");
            return;
        }

        /*── 5. 写入 ScriptableObject ──*/
        if (!(Selection.activeObject is ActiveSkillDatabase db))
        {
            Debug.LogError("请选中 ActiveSkillDatabase.asset 再导入");
            return;
        }

        typeof(ActiveSkillDatabase)
            .GetField("skills", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!
            .SetValue(db, list);
        db.tierMultiplier  = tierMult;
        db.levelMultiplier = levelMult;

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ 主动技能导入完成：{list.Count} 条；图标目录：{imagePath}");
    }

    /*──── 私有帮助函数 ────*/
    static Dictionary<string,float> ReadDictStringFloat(Dictionary<string,object> root,string key)
    {
        var dict = new Dictionary<string,float>();
        if (root.TryGetValue(key, out var obj))
        {
            var tmp = JsonConvert.DeserializeObject<Dictionary<string,float>>(obj.ToString());
            if (tmp != null) dict = tmp;
        }
        return dict;
    }
    static Dictionary<int,float> ReadDictIntFloat(Dictionary<string,object> root,string key)
    {
        var dict = new Dictionary<int,float>();
        if (root.TryGetValue(key, out var obj))
        {
            var tmp = JsonConvert.DeserializeObject<Dictionary<string,float>>(obj.ToString());
            if (tmp != null)
                foreach (var (k,v) in tmp)
                    dict[int.Parse(k)] = v;
        }
        return dict;
    }

    /*──── JSON 内部字段 ────*/
    private class RawSkill
    {
        public string name;
        public string cn_name;
        public string Description;
        public float  coefficient;
    }
}
