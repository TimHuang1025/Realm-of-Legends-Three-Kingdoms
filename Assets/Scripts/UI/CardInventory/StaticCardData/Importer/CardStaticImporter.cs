// Assets/Editor/CardStaticImporter.cs
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public static class CardStaticImporter
{
    [MenuItem("Tools/CardDB/Import Static JSON")]
    public static void Import()
    {
        /*──── 1. 选 JSON 文件 ────*/
        var path = EditorUtility.OpenFilePanel("选择静态卡牌 JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        var rootDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(path));
        if (rootDict == null) { Debug.LogError("解析失败：检查 JSON"); return; }

        /*──── 2. 取全局 image_path ────*/
        rootDict.TryGetValue("image_path", out var imagePathObj);
        var imagePath = (imagePathObj as string)?.TrimEnd('/', '\\') + "/";   // 保证结尾 /

        /*──── 3. 把卡牌键挑出来再反序列化 ────*/
        var list = new List<CardInfoStatic>();

        foreach (var (key, value) in rootDict)
        {
            // 卡牌 ID 约定以品质首字母开头：S/A/B/C/N
            if (!(key.StartsWith("S") || key.StartsWith("A") ||
                  key.StartsWith("B") || key.StartsWith("C") ||
                  key.StartsWith("N"))) continue;

            // value 先转成 JSON 字符串，再反序列化成 RawCard
            var raw = JsonConvert.DeserializeObject<RawCard>(value.ToString());

            /*── 4. 映射到 CardInfoStatic ──*/
            var card = new CardInfoStatic
            {
                id               = key,
                displayName      = raw.name,
                tier             = Enum.TryParse<Tier>(raw.tier, true, out var t) ? t : Tier.N,
                faction          = Enum.TryParse<Faction>(raw.faction, true, out var f) ? f : Faction.neutral,
                activeSkillId    = raw.ActiveSkill,
                passiveOneId     = raw.PassiveOne,
                passiveTwoId     = raw.PassiveTwo,
                trait            = raw.trait,
                description      = raw.Description,
                base_value_multiplier = raw.base_value_multiplier ?? new float[4]
            };

            /*── 5. 自动加载贴图 ──*/
            if (!string.IsNullOrEmpty(imagePath))
            {
                var iconPath = $"{imagePath}{key}_icon.png";
                var fullPath = $"{imagePath}{key}_full.png";
                card.iconSprite     = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                card.fullBodySprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
            }

            list.Add(card);
        }

        if (list.Count == 0)
        {
            Debug.LogError("⚠️ 未找到任何卡牌条目，确认 JSON 键是 S/A/B/C/N 开头的 ID");
            return;
        }

        /*──── 6. 写入选中的 ScriptableObject ────*/
        if (!(Selection.activeObject is CardDatabaseStatic db))
        {
            Debug.LogError("请先在 Project 面板选中 CardDatabaseStatic.asset 再导入");
            return;
        }

        typeof(CardDatabaseStatic)
            .GetField("cards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(db, list);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ 导入完成：{list.Count} 张卡；贴图目录：{imagePath}");
    }

    /*── JSON 内部字段映射 ──*/
    private class RawCard
    {
        public int      available;
        public string   name;
        public string   tier;
        public string   faction;
        public string   ActiveSkill;
        public string   PassiveOne;
        public string   PassiveTwo;
        public string   trait;
        public string   Description;
        public float[]  base_value_multiplier;
    }
}
