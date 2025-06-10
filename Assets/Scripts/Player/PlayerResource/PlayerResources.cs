using UnityEngine;

public enum ResourceType
{
    Gold,
    Silver,
    Copper,
    HeroExp,
    HeroMat2,
    GachaTicket
    // 需要更多资源直接往下加
}


[CreateAssetMenu(fileName = "PlayerResources", menuName = "Game/Player Resources")]
public class PlayerResources : ScriptableObject
{
    [Header("玩家资源")]
    public long gold         = 0;
    public long silver       = 0;
    public long copper       = 0;
    public long heroexp      = 0;
    public long heromat2     = 0;
    public int gachaTicket  = 0;
}
