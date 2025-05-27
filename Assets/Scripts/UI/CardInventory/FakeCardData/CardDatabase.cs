using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "CardDB/Database")]
public class CardDatabase : ScriptableObject
{
    public List<CardInfo> cards = new();
}

[System.Serializable]
public class CardInfo
{
    public string cardName;

    // ⬇⬇ 新增：方形头像 + 全身图
    public Sprite iconSprite;      //  _Icon
    public Sprite fullBodySprite;  // 点选后显示在 SelectedCardImage 上

    // 质量 / 阶级 / 等级
    public string quality;         // "S" / "A" / "B"…
    [Header("阶级")] public int rank;   // 星数 0~5
    [Header("等级")] public int level;  // 1~200
}
