using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/*───────────────────────────────────────────────*
 *  CardDatabase 保持不变
 *───────────────────────────────────────────────*/
[CreateAssetMenu(fileName = "CardDatabase", menuName = "CardDB/Database")]
public class CardDatabase : ScriptableObject
{
    public List<CardInfo> cards = new();
}

/*───────────────────────────────────────────────*
 *  CardInfo：保留字段名 level，但加事件 / 封装方法
 *───────────────────────────────────────────────*/
[Serializable]
public class CardInfo
{
    /*── 基础资料 ──*/
    public string cardId;
    public string cardName;
    public Sprite iconSprite;
    public Sprite fullBodySprite;

    /*── 品质 / 星级 ──*/
    public string quality;           // "S" / "A" / "B"
    [Range(0, 5)] public int rank;   // 星数 0~5

    /*── 等级 (仍叫 level) ──*/
    [Range(1, 999999)] public int level = 1;

    /*── 礼物好感 ──*/
    public int giftLv = 1;
    public int giftExp = 0;

    /*── 装备槽 ──*/
    public EquipState equip = new();

    /*── 新增：属性变化事件 ──*/
    public event Action<CardInfo> OnStatsChanged;

    /*── 升级统一走这个接口 ──*/
    public void AddLevel(int delta)
    {
        level = Mathf.Clamp(level + delta, 1, 999999);
        OnStatsChanged?.Invoke(this);
    }

    /*── 如果要改星级，同理 ──*/
    public void SetRank(int newRank)
    {
        rank = Mathf.Clamp(newRank, 0, 5);
        OnStatsChanged?.Invoke(this);
    }
    public VideoClip videoClip;
}

/*───────────────────────────────────────────────*/
[Serializable]
public class EquipState
{
    public bool weaponUnlocked = false;
    public bool armorUnlocked  = false;
    public bool mountUnlocked  = false;
}
