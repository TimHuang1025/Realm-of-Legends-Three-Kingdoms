// Assets/Scripts/SkillDB/PassiveSkillDatabase.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;   // ISkillMultiplierProvider、Tier

[CreateAssetMenu(
    fileName = "PassiveSkillDatabase",
    menuName = "SkillDB/Passive Database")]
public class PassiveSkillDatabase : ScriptableObject, ISkillMultiplierProvider
{
    /*──── 1. 技能条目 ────*/
    [Header("技能条目")]
    [SerializeField] private List<PassiveSkillInfo> skills = new();

    /*──── 2. 可序列化倍率表 ────*/
    [Header("乘区倍率表")]
    [SerializeField] private List<TierEntry>  tierTable  = new();   // S,A,B…
    [SerializeField] private List<LevelEntry> levelTable = new();   // 1~4

    /*──── 运行时缓存 ────*/
    Dictionary<string, PassiveSkillInfo> lookup;      // id→技能
    Dictionary<Tier,   float>            tierDict;    // Tier→倍率
    Dictionary<int,    float>            levelDict;   // 1~4→倍率

    /*─── 序列化结构 ───*/
    [Serializable] public class TierEntry  { public string key; public float value = 1f; }
    [Serializable] public class LevelEntry { public int    key; public float value = 1f; }

    /*──── 生命周期 ────*/
    void OnEnable()
    {
        RebuildLookup();
        RebuildMultiplierCache();
    }

    /*──── Setter：给导入器调用 ────*/
    public void SetSkillList(List<PassiveSkillInfo> list)
    {
        skills = list;
        RebuildLookup();
    }
    public void SetTierTable(Dictionary<string,float> src)
    {
        tierTable.Clear();
        foreach (var kv in src)
            tierTable.Add(new TierEntry { key = kv.Key, value = kv.Value });
        RebuildMultiplierCache();
    }
    public void SetLevelTable(Dictionary<int,float> src)
    {
        levelTable.Clear();
        foreach (var kv in src)
            levelTable.Add(new LevelEntry { key = kv.Key, value = kv.Value });
        RebuildMultiplierCache();
    }

    /*──── 私有构建 ────*/
    void RebuildLookup()
    {
        lookup = new();
        foreach (var s in skills)
            if (!string.IsNullOrEmpty(s.id))
                lookup[s.id] = s;
    }
    void RebuildMultiplierCache()
    {
        tierDict  = new();
        foreach (var e in tierTable)
            if (Enum.TryParse(e.key, true, out Tier t))
                tierDict[t] = e.value;

        levelDict = new();
        foreach (var e in levelTable)
            levelDict[e.key] = e.value;
    }

    /*──── 公共 API ────*/
    public PassiveSkillInfo Get(string id) =>
        id != null && lookup.TryGetValue(id, out var s) ? s : null;
    public IReadOnlyList<PassiveSkillInfo> All => skills;

    /*──── ISkillMultiplierProvider ────*/
    IReadOnlyDictionary<Tier, float> ISkillMultiplierProvider.TierMultiplier
        => tierDict;
    IReadOnlyDictionary<int,  float> ISkillMultiplierProvider.LevelMultiplier
        => levelDict;
}

/*──────── 技能条目 ────────*/
[Serializable]
public class PassiveSkillInfo
{
    public string id;
    public string name;
    public string cnName;
    public SkillTiming timing;
    [TextArea] public string description;
    public float baseValue;
    public Sprite iconSprite;
}

public enum SkillTiming { Create = 0, BattleStart = 1, TurnStart = 2 }
