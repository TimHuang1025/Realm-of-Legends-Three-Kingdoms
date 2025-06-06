using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存放所有玩家卡牌动态状态的 SO（可作为资源存档）
/// </summary>
[CreateAssetMenu(menuName = "Player/Card Bank")]
public class PlayerCardBank : ScriptableObject
{
    public List<PlayerCard> cards = new();

    // 运行期快速查询
    private Dictionary<string, PlayerCard> lookup;

    void OnEnable()
    {
        lookup = new Dictionary<string, PlayerCard>();
        foreach (var c in cards)
            if (!string.IsNullOrEmpty(c.id))
                lookup[c.id] = c;
    }

    public PlayerCard Get(string id) =>
        lookup != null && lookup.TryGetValue(id, out var c) ? c : null;
}
