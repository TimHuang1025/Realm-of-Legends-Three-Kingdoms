using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ActiveSkillDatabase",
    menuName = "SkillDB/Active Database")]
public class ActiveSkillDatabase : ScriptableObject
{
    /*──── 1. 技能条目 ────*/
    [SerializeField] private List<ActiveSkillInfo> skills = new();
    Dictionary<string, ActiveSkillInfo> lookup;

    /*──── 2. 全局倍率表 ────*/
    public Dictionary<string, float> tierMultiplier  = new();
    public Dictionary<int, float>    levelMultiplier = new();

    void OnEnable()
    {
        /* 建索引 */
        lookup = new();
        foreach (var s in skills)
            if (!string.IsNullOrEmpty(s.id))
                lookup[s.id] = s;
    }

    public ActiveSkillInfo Get(string id) =>
        lookup != null && lookup.TryGetValue(id, out var s) ? s : null;

    public IReadOnlyList<ActiveSkillInfo> All => skills;
}

/*──────── 技能条目 ────────*/
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
