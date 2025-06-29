// Assets/Scripts/Game/Core/ResourceType.cs
using UnityEngine;

/// <summary>
/// 游戏中所有可计数资源的枚举。枚举名即资源代码（区分大小写）。
/// 新资源直接往下加，不影响已发布版本的序号（除非你在存档里用 int 保存枚举值）
/// </summary>
public enum ResourceType
{
    // 货币
    Gold,                 // 金锭
    Silver,               // 银锭
    Copper,               // 铜钱
    Food,                 // 粮草

    // 通用令牌 / 货币
    SummonWrit,           // 征才令（抽卡）
    TroopOrder,           // 募兵帖
    UnionMerit,           // 联盟军功
    Merit,                // 战功值（经验）
    AdvanceScroll,        // 进修册

    // 武将碎片
    HeroCrest_SGeneral,   // S 将印（通用）
    HeroCrest_AGeneral,   // A 将印（通用）
    HeroCrest_BGeneral,   // B 将印（通用）

    // —— 以下为常用材料，按需增删 ——  
    Forage,               // 马粮
    RefinedIron,          // 炼铁
    FineWood,             // 精木
    FineSteel,            // 精钢
    TechScroll,           // 科技卷轴
    Fame,                 // 声望
    ArtOfWar,             // 兵法
    TrainingToken,        // 练兵券
    RespecPotion,         // 重修药水
    RawIron,              // 粗铁
    Firmwood,             // 坚木
    IronIngot,            // 铁锭
    Honor,                // 封赏系统物品（Honor_xxx）
    MerchantDeed,         // 商契
    LandDeed              // 田契

    // 需要更多资源直接往下加
}

[CreateAssetMenu(fileName = "PlayerResources", menuName = "Game/Player Resources")]
public class PlayerResources : ScriptableObject
{
    // ───────── 基础货币 ─────────
    public long gold = 0;
    public long silver = 0;
    public long copper = 0;
    public long food = 0;

    // ───────── 通用令牌 / 经验 ─────────
    public int summonWrit = 0;   // 征才令
    public int troopOrder = 0;   // 募兵帖
    public long unionMerit = 0;   // 联盟军功
    public long merit = 0;   // 战功值（经验）
    public int advanceScroll = 0;  // 进修册

    // ───────── 武将碎片 ─────────
    public int heroCrestSGeneral = 0;  // S 将印（通用）
    public int heroCrestAGeneral = 0;  // A 将印（通用）
    public int heroCrestBGeneral = 0;  // B 将印（通用）

    // ───────── 常用材料（可按需裁剪）─────────
    public long forage = 0;  // 马粮
    public long refinedIron = 0;  // 炼铁
    public long fineWood = 0;  // 精木
    public long fineSteel = 0;  // 精钢
    public long techScroll = 0;  // 科技卷轴
    public long fame = 0;  // 声望
    public int artOfWar = 0;  // 兵法
    public int trainingToken = 0;  // 练兵券
    public int respecPotion = 0;  // 重修药水
    public long rawIron = 0;  // 粗铁
    public long firmwood = 0;  // 坚木
    public long ironIngot = 0;  // 铁锭
    public int honor = 0;  // Honor_xxx（如果按 ID 细分，可改成 Dictionary）
    public int merchantDeed = 0;  // 商契
    public int landDeed = 0;  // 田契

    // 需要更多资源继续加字段，名字保持和 ResourceType 枚举一致即可
    
}
