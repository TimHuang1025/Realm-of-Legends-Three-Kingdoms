using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 整个游戏所有武将 / 卡牌的数据库：
/// - 在 Project 视图中选中后，可一次性编辑所有卡牌数据
/// - 运行时由 CardInventory 等系统读取
/// </summary>
[CreateAssetMenu(fileName = "CardDatabase", menuName = "CardDB/Database")]
public class CardDatabase : ScriptableObject
{
    /// <summary>所有卡片条目</summary>
    public List<CardInfo> cards = new();
}

/*───────────────────────────────────────────────*/

/// <summary>
/// 每一张武将卡片的数据结构（可序列化进 .asset / 存档）
/// </summary>
[System.Serializable]
public class CardInfo
{
    [Header("基础资料")]
    public string  cardName;
    public Sprite  iconSprite;      // 方形头像
    public Sprite  fullBodySprite;  // 立绘，大图

    [Header("品质 / 星级 / 等级")]
    public string  quality;         // "S" / "A" / "B"
    [Range(0,5)]  public int rank;  // 星数 0~5
    [Range(1,200)]public int level; // 等级

    [Header("礼物好感")]
    public int giftLv  = 1;         // 1~4，对应“拜将 / 授甲 / 赐骑 / 封侯”
    public int giftExp = 0;         // 当前累计 EXP

    [Header("装备槽解锁状态")]
    public EquipState equip = new EquipState();
}

/*───────────────────────────────────────────────*/

/// <summary>
/// 一张卡片 3 个装备槽的解锁布尔值
/// </summary>
[System.Serializable]
public class EquipState
{
    public bool weaponUnlocked = false;   // 武器槽
    public bool armorUnlocked  = false;   // 盔甲槽
    public bool mountUnlocked  = false;   // 坐骑槽

    // 后期想加新槽只需再添字段：
    // public bool accessoryUnlocked = false;
}
