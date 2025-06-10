#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Game.Data;

public static class HorseDatabaseImporter
{
    [MenuItem("Tools/Import Horse JSON…")]
    static void Import()
    {
        string jsonPath = EditorUtility.OpenFilePanel("horse.json", Application.dataPath, "json");
        if (string.IsNullOrEmpty(jsonPath)) return;

        JObject root = JObject.Parse(File.ReadAllText(jsonPath));

        // ① 读公式参数
        var valCoe  = ReadTierDict(root["value_coe"]);
        var valPow  = ReadTierDict(root["value_pow"]);
        var costCoe = ReadTierDict(root["cost_coe"]);
        var costPow = ReadTierDict(root["cost_pow"]);

        // ② 读条目
        var list = new List<HorseStatic>();
        foreach (var p in root.Properties())
        {
            if (!p.Name.StartsWith("horse_")) continue;
            JObject o = (JObject)p.Value;

            list.Add(new HorseStatic
            {
                id              = o["id"].Value<string>(),
                name            = o["name"].Value<string>(),
                tier            = Enum.Parse<HorseTier>(o["tier"].Value<string>()),
                valueMultiplier = o["value_multiplier"].ToObject<float[]>()
            });
        }

        // ③ 写入 / 新建 SO
        string assetPath = "Assets/Resources/GameDB/HorseStaticDB.asset";
        string dirPath   = Path.GetDirectoryName(assetPath);

        // ☆ 若目录不存在先建
        if (!AssetDatabase.IsValidFolder(dirPath))
        {
            Directory.CreateDirectory(dirPath);          // 创建磁盘目录
            AssetDatabase.Refresh();                     // 让 Unity 识别新文件夹
        }

        var db = AssetDatabase.LoadAssetAtPath<HorseDatabaseStatic>(assetPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<HorseDatabaseStatic>();
            AssetDatabase.CreateAsset(db, assetPath);
        }

        CopyToDB(db, list, valCoe, valPow, costCoe, costPow);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log("✅ 马匹数据导入完成");
    }

    static Dictionary<HorseTier,float> ReadTierDict(JToken tok)
    {
        var d = new Dictionary<HorseTier,float>();
        foreach (var p in tok.Children<JProperty>())
            d[Enum.Parse<HorseTier>(p.Name)] = p.Value.Value<float>();
        return d;
    }

    static void CopyToDB(
        HorseDatabaseStatic db,
        List<HorseStatic> list,
        Dictionary<HorseTier,float> valCoe,
        Dictionary<HorseTier,float> valPow,
        Dictionary<HorseTier,float> costCoe,
        Dictionary<HorseTier,float> costPow)
    {
        void Set(string field, object value) =>
            typeof(HorseDatabaseStatic)
            .GetField(field, System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
            .SetValue(db, value);

        HorseDatabaseStatic.TierFloatPair[] ToArr(Dictionary<HorseTier,float> src)
        {
            var a = new HorseDatabaseStatic.TierFloatPair[src.Count];
            int i = 0;
            foreach (var kv in src)
                a[i++] = new HorseDatabaseStatic.TierFloatPair { tier = kv.Key, value = kv.Value };
            return a;
        }

        Set("horses",         list);
        Set("valueCoeArr",    ToArr(valCoe));
        Set("valuePowArr",    ToArr(valPow));
        Set("costCoeArr",     ToArr(costCoe));
        Set("costPowArr",     ToArr(costPow));
    }
}
#endif
