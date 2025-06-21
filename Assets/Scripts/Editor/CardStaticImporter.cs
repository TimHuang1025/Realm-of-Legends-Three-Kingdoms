// Assets/Editor/CardStaticImporter.cs
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public static class CardStaticImporter
{
    [MenuItem("Tools/CardDB/Import Static JSON")]
    public static void Import()
    {
        /*── 1. 选 JSON 文件 ───*/
        var path = EditorUtility.OpenFilePanel("选择 card.json", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;
        var root = JObject.Parse(File.ReadAllText(path));

        /*── 2. 找目标 ScriptableObject ───*/
        if (!(Selection.activeObject is CardDatabaseStatic db))
        {
            EditorUtility.DisplayDialog("未选中 CardDatabaseStatic",
                "请先在 Project 面板选中 CardDatabaseStatic.asset 再导入", "知道了");
            return;
        }

        /*── 3. 提取公共字段 ───*/
        string imgDir = root["image_path"]?.Value<string>()?.TrimEnd('/', '\\') + "/";
        var tierMult = root["base_value_tier_multiplier"]?.ToObject<Dictionary<string,float>>();

        /*── 4. 解析升星表 ───*/
        var starTable = new List<StarUpgradeRule>();
        var starObj = root["star"] as JObject;
        if (starObj != null)
        {
            foreach (var prop in starObj.Properties())
            {
                if (!int.TryParse(prop.Name, out int lv)) continue;
                var arr = (JArray)prop.Value;

                starTable.Add(new StarUpgradeRule
                {
                    starLevel      = lv,
                    shardsRequired = (int)arr[0],
                    battlePowerAdd = (int)arr[1],
                    skillLvGain    = new []{
                        (int)arr[2][0], (int)arr[2][1], (int)arr[2][2]},
                    frameColor     = (string)arr[3],
                    starsInFrame   = (int)arr[4]
                });
            }
        }

        /*── 5. 解析卡牌条目 ───*/
        var cards = new List<CardInfoStatic>();
        foreach (var prop in root.Properties())
        {
            if (!(prop.Name.StartsWith("S") || prop.Name.StartsWith("A") ||
                  prop.Name.StartsWith("B") || prop.Name.StartsWith("C") ||
                  prop.Name.StartsWith("N"))) continue;

            var raw = prop.Value.ToObject<RawCard>();

            var card = new CardInfoStatic
            {
                id          = prop.Name,
                displayName = raw.name,
                tier        = Enum.TryParse<Tier>(raw.tier, true, out var t) ? t : Tier.N,
                faction     = Enum.TryParse<Faction>(raw.faction, true, out var f) ? f : Faction.neutral,
                activeSkillId = raw.ActiveSkill,
                passiveOneId  = raw.PassiveOne,
                passiveTwoId  = raw.PassiveTwo,
                trait       = raw.trait,
                description = raw.Description,
                base_value_multiplier = raw.base_value_multiplier ?? new float[4]
            };

            /* 自动加载贴图 */
            if (!string.IsNullOrEmpty(imgDir))
            {
                string icon = $"{imgDir}{prop.Name}_icon.png";
                string full = $"{imgDir}{prop.Name}_full.jpg";
                card.iconSprite     = AssetDatabase.LoadAssetAtPath<Sprite>(icon);
                card.fullBodySprite = AssetDatabase.LoadAssetAtPath<Sprite>(full);
            }
            cards.Add(card);
        }

        if (cards.Count == 0) { Debug.LogError("未找到任何卡牌条目"); return; }

        /*── 6. 写入 SO 私有字段 ───*/
        var so = new SerializedObject(db);

        /* 6-1 卡牌列表 */
        var cardArr = so.FindProperty("cards");
        WriteList(cardArr, cards, (dst, src) =>
        {
            dst.FindPropertyRelative("id").stringValue               = src.id;
            dst.FindPropertyRelative("displayName").stringValue      = src.displayName;
            dst.FindPropertyRelative("tier").enumValueIndex          = (int)src.tier;
            dst.FindPropertyRelative("faction").enumValueIndex       = (int)src.faction;
            dst.FindPropertyRelative("iconSprite").objectReferenceValue = src.iconSprite;
            dst.FindPropertyRelative("fullBodySprite").objectReferenceValue = src.fullBodySprite;
            dst.FindPropertyRelative("activeSkillId").stringValue    = src.activeSkillId;
            dst.FindPropertyRelative("passiveOneId").stringValue     = src.passiveOneId;
            dst.FindPropertyRelative("passiveTwoId").stringValue     = src.passiveTwoId;
            dst.FindPropertyRelative("trait").stringValue            = src.trait;
            dst.FindPropertyRelative("description").stringValue      = src.description;
            var mul = dst.FindPropertyRelative("base_value_multiplier");
            mul.arraySize = 4;
            for (int i = 0; i < 4; i++)
                mul.GetArrayElementAtIndex(i).floatValue = i < src.base_value_multiplier.Length
                                                           ? src.base_value_multiplier[i] : 1f;
        });

        /* 6-2 升星表 */
        var starArr = so.FindProperty("starTable");
        WriteList(starArr, starTable, (dst, src) =>
        {
            dst.FindPropertyRelative("starLevel").intValue      = src.starLevel;
            dst.FindPropertyRelative("shardsRequired").intValue = src.shardsRequired;
            dst.FindPropertyRelative("battlePowerAdd").intValue = src.battlePowerAdd;
            var skl = dst.FindPropertyRelative("skillLvGain");
            skl.arraySize = 3;
            for (int i = 0; i < 3; i++)
                skl.GetArrayElementAtIndex(i).intValue = src.skillLvGain[i];
            dst.FindPropertyRelative("frameColor").stringValue  = src.frameColor;
            dst.FindPropertyRelative("starsInFrame").intValue   = src.starsInFrame;
        });

        /* 6-3 品阶倍率表 */
        var tierArr = so.FindProperty("tierTable");
        tierArr.arraySize = 0;
        if (tierMult != null)
        {
            foreach (var (k,v) in tierMult)
            {
                int idx = tierArr.arraySize++;
                var elem = tierArr.GetArrayElementAtIndex(idx);
                elem.FindPropertyRelative("key").stringValue   = k;
                elem.FindPropertyRelative("value").floatValue  = v;
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ 已导入 {cards.Count} 张卡，{starTable.Count} 条星阶规则");
    }

    /*──────── 私有工具 ───────*/
    static void WriteList<T>(SerializedProperty arr, List<T> src, Action<SerializedProperty,T> assign)
    {
        arr.arraySize = src.Count;
        for (int i = 0; i < src.Count; i++)
            assign(arr.GetArrayElementAtIndex(i), src[i]);
    }

    /*──────── JSON 映射 ───────*/
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
