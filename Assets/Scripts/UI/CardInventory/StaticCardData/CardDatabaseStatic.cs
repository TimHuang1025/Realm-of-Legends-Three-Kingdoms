// Assets/Scripts/CardDB/CardDatabaseStatic.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CardDatabaseStatic",
    menuName = "CardDB/Static Database")]
public class CardDatabaseStatic : ScriptableObject
{
    public static CardDatabaseStatic Instance =>
        _inst ??= Resources.Load<CardDatabaseStatic>("CardDatabaseStatic");
    static CardDatabaseStatic _inst;
    /*─────────── ① 卡牌静态表 ───────────*/
    [SerializeField] private List<CardInfoStatic> cards = new();
    public IReadOnlyList<CardInfoStatic> AllCards => cards;

    /*─────────── ② 升星 / 品阶表 ───────────*/
    [Header("升星规则 (1‒15)")]
    [SerializeField] private List<StarUpgradeRule> starTable = new();     // 序列化
    [Header("品阶倍率 (S/A/B/…)")]
    [SerializeField] private List<TierEntry> tierTable = new();           // 序列化

    /*─────────── ③ 运行时缓存 ───────────*/
    Dictionary<string, CardInfoStatic> cardLookup;
    Dictionary<int,    StarUpgradeRule> starLookup;
    Dictionary<Tier,   float> tierLookup;

    /*─────────── ④ 生命周期 ───────────*/
    void OnEnable()
    {
        /* 卡牌索引 */
        cardLookup = new();
        foreach (var c in cards)
            if (!string.IsNullOrEmpty(c.id))
                cardLookup[c.id] = c;

        /* 星级索引 */
        starLookup = new();
        foreach (var r in starTable)
            starLookup[r.starLevel] = r;

        /* 品阶倍率索引 */
        tierLookup = new();
        foreach (var e in tierTable)
            if (Enum.TryParse(e.key, true, out Tier t))
                tierLookup[t] = e.value;
    }

    /*─────────── ⑤ 公共 API ───────────*/
    public CardInfoStatic   GetCard(string id)   => cardLookup.TryGetValue(id,   out var c) ? c : null;
    public StarUpgradeRule  GetStar(int level)   => starLookup.TryGetValue(level, out var r) ? r : null;
    public float            GetTierMultiplier(Tier t) => tierLookup.TryGetValue(t, out var v) ? v : 1f;

    public IReadOnlyList<StarUpgradeRule> AllStars => starTable;
    public IReadOnlyDictionary<Tier,float> TierMultiplier => tierLookup;
}

/*─────────── 附属数据结构 ───────────*/

[Serializable]
public class CardInfoStatic
{
    public string id;
    public string displayName;
    public Tier tier;
    public Faction faction;
    public Sprite iconSprite;
    public Sprite fullBodySprite;
    public string activeSkillId;
    public string passiveOneId;
    public string passiveTwoId;
    public string trait;
    [TextArea] public string description;
    public float[] base_value_multiplier = {1,1,1,1};
}

[Serializable]
public class StarUpgradeRule
{
    public int starLevel;          // 目标星级 1~15
    public int shardsRequired;     // 所需碎片
    public int battlePowerAdd;     // 战力加成
    public int[] skillLvGain;      // 长度=3 (主动/被动1/被动2)
    public string frameColor;      // blue / purple / gold
    public int starsInFrame;       // UI 第几颗星
}

[Serializable] public class TierEntry { public string key; public float value = 1f; }

public enum Tier    { S, A, B, C, N }
public enum Faction { wei, shu, wu, qun, neutral }
