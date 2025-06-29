// Assets/Scripts/TechTree/Editor/TechProgressIO.cs
//------------------------------------------------
/*
    
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class TechProgressIO
{
    private const string DefaultJsonPath = "Assets/TechProgress.json";

    /* ---------- 从 JSON 创建 / 更新 ScriptableObject ---------- *//***
    [MenuItem("Tools/TechTree/Import Progress JSON")]
    private static void ImportJson()
    {
        string path = EditorUtility.OpenFilePanel("选择 progress.json", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        JObject jo  = JObject.Parse(json);

        TechProgressData asset = ScriptableObject.CreateInstance<TechProgressData>();

        asset.currentStage = jo["current_stage"]?.Value<int>() ?? 1;
        asset.total        = jo["total"]?.Value<int>()        ?? 0;
        asset.complete     = jo["complete"]?.Value<int>()     ?? 0;
        asset.progress     = jo["progress"]?.Value<string>()  ?? "";

        JObject bonusObj = jo["player_tech_bonus"] as JObject;
        if (bonusObj != null)
            JsonUtility.FromJsonOverwrite(bonusObj.ToString(), asset.bonus);

        string savePath = EditorUtility.SaveFilePanelInProject("保存 Progress Asset",
                                                               "TechProgressData.asset",
                                                               "asset",
                                                               "选择保存路径");
        if (!string.IsNullOrEmpty(savePath))
        {
            AssetDatabase.CreateAsset(asset, savePath);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
    }

    /* ---------- 将 ScriptableObject 写回 JSON ---------- * 这里有备注！！！！！！看这里！！！
    [MenuItem("Tools/TechTree/Export Progress JSON")]
    private static void ExportJson()
    {
        var asset = Selection.activeObject as TechProgressData;
        if (asset == null)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 视图选中一个 TechProgressData 资产", "OK");
            return;
        }

        JObject jo = new JObject
        {
            ["current_stage"] = asset.currentStage,
            ["total"]         = asset.total,
            ["complete"]      = asset.complete,
            ["progress"]      = asset.progress,
            ["player_tech_bonus"] = JObject.Parse(JsonUtility.ToJson(asset.bonus))
        };

        string path = EditorUtility.SaveFilePanel("导出为 JSON", Application.dataPath, "TechProgress", "json");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, jo.ToString());
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
        }
    }
}
***/