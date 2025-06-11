using System;
using UnityEngine;

/// <summary>
/// 玩家拥有的单张武将卡的动态状态
/// </summary>
[Serializable]                       // 仅数据，不是 ScriptableObject
public class PlayerCard
{
    /*──── 基础 ────*/
    public string id;                // 对应静态库里的卡牌 ID（如 "B6"）
    public int    level  = 1;        // 角色等级
    public int    star   = 1;        // 星级（Rank）
    public int    copies = 0;        // 额外碎片 / 重复抽到

    /*──── 礼物系统 ────*/
    public int giftLv  = 1;          // 1: 拜将, 2: 授甲, 3: 赐骑, 4: 封侯
    public int giftExp = 0;          // 当前等级已累计经验

    /*──── 装备槽 ────*/
    public EquipStatus equip = new(); // 各槽当前穿戴 + 解锁状态
}

/// <summary>
/// 该卡当前穿戴的装备槽位（空串 = 未装备）
/// 并记录槽位是否已解锁
/// </summary>
[Serializable]
public class EquipStatus
{
    public string weaponUuid     = "";
    public string armorUuid      = "";
    public string accessoryUuid  = "";

    public bool   weaponUnlocked = false;
    public bool   armorUnlocked  = false;
    public bool   mountUnlocked  = false;   // 坐骑槽（accessory 对应）
}
