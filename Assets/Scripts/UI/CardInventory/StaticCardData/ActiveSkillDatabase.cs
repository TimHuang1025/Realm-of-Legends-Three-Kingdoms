// Assets/Scripts/SkillDB/ActiveSkillDatabase.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;                         // ISkillMultiplierProvider, Tier

[CreateAssetMenu(
    fileName = "ActiveSkillDatabase",
    menuName = "SkillDB/Active Database")]
public class ActiveSkillDatabase : ScriptableObject, ISkillMultiplierProvider
{
    /*──────── 1. 技能条目 ─────────────────────────*/
    [Header("技能条目")]
    [SerializeField] private List<ActiveSkillInfo> skills = new();
    Dictionary<string, ActiveSkillInfo> lookup;          // 运行时索引

    /*──────── 2. 序列化倍率表（在 Inspector 可见） ─────────*/
    [Header("倍率表")]
    [SerializeField] public List<TierEntry>  tierTable  = new();
    [SerializeField] public List<LevelEntry> levelTable = new();

    /*──────── 3. 运行时缓存（永不为 null） ───────────────*/
    Dictionary<Tier, float> tierDict;   // Tier  → 倍率
    Dictionary<int,  float> levelDict;  // Lv(1‒5) → 倍率

    /*──────── 4. 生命周期 ───────────────────────*/
    void OnEnable()
    {
        BuildLookup();
        RebuildMultiplierCache();        // 列表 → 字典
    }

    /*──────── 5. 导入器可调用的刷新入口 ───────────*/
    public void RebuildMultiplierCache()
    {
        // Tier
        tierDict = new();
        foreach (var e in tierTable)
            if (Enum.TryParse(e.key, true, out Tier t))
                tierDict[t] = e.value;

        // Level
        levelDict = new();
        foreach (var e in levelTable)
            levelDict[e.key] = e.value;
    }

    void BuildLookup()
    {
        lookup = new();
        foreach (var s in skills)
            if (!string.IsNullOrEmpty(s.id))
                lookup[s.id] = s;
    }

    /*──────── 6. 公共 API ───────────────────────*/
    public ActiveSkillInfo Get(string id) =>
        !string.IsNullOrEmpty(id) && lookup.TryGetValue(id, out var s) ? s : null;

    public IReadOnlyList<ActiveSkillInfo> All => skills;

    /*──────── 7. ISkillMultiplierProvider 实现 ────*/
    public IReadOnlyDictionary<Tier, float> TierMultiplier  => tierDict;
    public IReadOnlyDictionary<int,  float> LevelMultiplier => levelDict;

    /*──────── 8. 序列化结构 ─────────────────────*/
    [Serializable] public class TierEntry  { public string key; public float value = 1f; }
    [Serializable] public class LevelEntry { public int    key; public float value = 1f; }
}

/*──────── 技能条目 ─────────────────────────────*/
[Serializable]
public class ActiveSkillInfo
{
    public string id;               // "A1"
    public string name;             // 英文
    public string cnName;           // 中文
    [TextArea] public string description;
    public float coefficient;       // 基础系数
    public Sprite iconSprite;       // 技能图标
}
