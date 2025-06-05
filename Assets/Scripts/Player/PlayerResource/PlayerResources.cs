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
    public int gold         = 0;
    public int silver       = 0;
    public int copper       = 0;
    public int heroexp      = 0;
    public int heromat2     = 0;
    public int gachaTicket  = 0;
}
