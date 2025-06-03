// GachaPoolDatabase.cs  (放 Editor/Runtime 均可)
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GachaPoolDB", menuName = "Gacha/Pool Database")]
public class GachaPoolDatabase : ScriptableObject
{
    public List<GachaPoolInfo> pools = new();
}

[System.Serializable]
public class GachaPoolInfo
{
    public string poolId;        // "standard_01"
    public string title;         // UI 上显示的标题
    public Sprite banner;       // 按钮上的图片 / 竖幅
    public int costx1;      // 单抽价格
    public int costx10;     // 十连价格
    public List<DropEntry> drops;
    public bool showCountdown = false;
    public int expireSeconds;
    
}
[System.Serializable]
public class DropEntry
{
    public string rewardId;            // 卡牌 ID 或物品 ID
    [Range(1, 10000)]
    public int weight = 1;             // 权重
}