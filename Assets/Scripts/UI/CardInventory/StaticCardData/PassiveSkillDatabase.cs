using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PassiveSkillDatabase",
    menuName = "SkillDB/Passive Database")]
public class PassiveSkillDatabase : ScriptableObject
{
    [SerializeField] private List<PassiveSkillInfo> skills = new();
    Dictionary<string, PassiveSkillInfo> lookup;

    void OnEnable()
    {
        lookup = new();
        foreach (var s in skills)
            if (!string.IsNullOrEmpty(s.id))
                lookup[s.id] = s;
    }

    public PassiveSkillInfo Get(string id) =>
        lookup != null && lookup.TryGetValue(id, out var s) ? s : null;

    public IReadOnlyList<PassiveSkillInfo> All => skills;
}

[Serializable]
public class PassiveSkillInfo
{
    public string id;              // "P1"
    public string name;            // 英文
    public string cnName;          // 中文
    public SkillTiming timing;     // 0 / 1 / 2
    [TextArea] public string description;   // 带 {X} 占位符
    public float baseValue;        // JSON 原始 value
    public Sprite iconSprite;      // 可选：技能图标
}

/* 0=创建时,1=战斗开始,2=每回合开始 */
public enum SkillTiming { Create = 0, BattleStart = 1, TurnStart = 2 }
