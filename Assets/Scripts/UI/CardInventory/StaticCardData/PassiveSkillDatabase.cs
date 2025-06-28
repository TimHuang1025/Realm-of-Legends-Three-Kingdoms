// ────────────────────────────────────────────────────────────────────────────────
// Assets/Scripts/SkillDB/PassiveSkillDatabase.cs
// 被动技能数据库（实现 ISkillMultiplierSource）
// ────────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;                // ISkillMultiplierSource / Tier 枚举

[CreateAssetMenu(
    fileName = "PassiveSkillDatabase",
    menuName = "SkillDB/Passive Database")]
public class PassiveSkillDatabase : ScriptableObject, ISkillMultiplierSource
{
    /*──────────────── 1. 技能条目 ───────────────────────────*/
    [Header("技能条目")]
    [SerializeField] private List<PassiveSkillInfo> skills = new();

    /*──────────────── 2. 可序列化倍率表 ─────────────────────*/
    [Header("倍率表 (Tier / Lv)")]
    [SerializeField] private List<TierEntry>  tierTable  = new();   // S / A / B …
    [SerializeField] private List<LevelEntry> levelTable = new();   // 1 ~ 4

    /*──────────────── 3. 运行时缓存 ─────────────────────────*/
    Dictionary<string, PassiveSkillInfo> lookup;   // id → 技能
    Dictionary<Tier,   float>            tierDict; // Tier → 倍率
    Dictionary<int,    float>            levelDict;// Lv   → 倍率

    /*──────── 内部序列化结构 ───────────────────────────────*/
    [Serializable] public class TierEntry  { public string key; public float value = 1f; }
    [Serializable] public class LevelEntry { public int    key; public float value = 1f; }

    /*──────────────── 4. 生命周期 ─────────────────────────*/
    void OnEnable()
    {
        RebuildLookup();
        RebuildMultiplierCache();
    }

    /*──────────────── 5. 导入器 Setter（可选） ─────────────*/
    public void SetSkillList(List<PassiveSkillInfo> list)          { skills = list;  RebuildLookup(); }
    public void SetTierTable (Dictionary<string, float> src)       { tierTable .Clear(); foreach (var kv in src) tierTable .Add(new TierEntry  { key = kv.Key, value = kv.Value }); RebuildMultiplierCache(); }
    public void SetLevelTable(Dictionary<int,    float> src)       { levelTable.Clear(); foreach (var kv in src) levelTable.Add(new LevelEntry { key = kv.Key, value = kv.Value }); RebuildMultiplierCache(); }

    /*──────────────── 6. 运行时构建 ───────────────────────*/
    void RebuildLookup()
    {
        lookup = new();
        foreach (var s in skills)
            if (!string.IsNullOrEmpty(s.id))
                lookup[s.id] = s;
    }

    void RebuildMultiplierCache()
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

    /*──────────────── 7. 公共 API ─────────────────────────*/
    public PassiveSkillInfo Get(string id) =>
        !string.IsNullOrEmpty(id) && lookup.TryGetValue(id, out var s) ? s : null;

    public IReadOnlyList<PassiveSkillInfo> All => skills;

    /*──────────────── 8. ISkillMultiplierSource 实现 ──────*/
    public IReadOnlyDictionary<Tier, float> TierMultiplier  => tierDict;
    public IReadOnlyDictionary<int,  float> LevelMultiplier => levelDict;
}

/*───────────────────────── 数据结构 ───────────────────────*/
[Serializable]
public class PassiveSkillInfo
{
    public string id;           // "P11"
    public string name;         // 英文
    public string cnName;       // 中文
    public SkillTiming timing;  // 触发时机
    [TextArea] public string description;
    public float baseValue;     // 基础数值
    public Sprite iconSprite;   // 图标
}

public enum SkillTiming { Create = 0, BattleStart = 1, TurnStart = 2 }
