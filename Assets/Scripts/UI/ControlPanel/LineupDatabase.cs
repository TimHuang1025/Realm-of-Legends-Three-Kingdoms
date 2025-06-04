using System.Collections.Generic;
using UnityEngine;

/* ───────── 兵种与比例 ───────── */

public enum TroopType { Infantry, Cavalry, Archer }

[System.Serializable]
public struct TroopRatio
{
    [Range(0, 100)] public int infantry;
    [Range(0, 100)] public int cavalry;
    [Range(0, 100)] public int archer;

    public bool IsValid() => infantry + cavalry + archer == 100;
}

/* ───────── 单条阵容（仅存名字） ───────── */

[System.Serializable]
public class LineupInfo
{
    [Header("武将名字（与 CardInfo.cardName 匹配）")]
    public string mainGeneral;
    public string subGeneral1;
    public string subGeneral2;
    public string strategist;

    [Header("兵种比例 (总和 100)")]
    public TroopRatio ratio;

    /// <summary>把名字解析成 CardInfo；解析失败返回 null</summary>
    public ResolvedLineup Resolve(CardDatabase cardDB)
    {
        return new ResolvedLineup
        {
            mainGeneral = cardDB.FindByName(mainGeneral),
            subGeneral1 = cardDB.FindByName(subGeneral1),
            subGeneral2 = cardDB.FindByName(subGeneral2),
            strategist  = cardDB.FindByName(strategist),
            ratio       = ratio
        };
    }
}

/* ───────── 解析后的数据结构 ───────── */

public struct ResolvedLineup
{
    public CardInfo  mainGeneral;
    public CardInfo  subGeneral1;
    public CardInfo  subGeneral2;
    public CardInfo  strategist;
    public TroopRatio ratio;
}

/* ───────── 阵容数据库 ───────── */

[CreateAssetMenu(fileName = "LineupDB", menuName = "Lineup/Database")]
public class LineupDatabase : ScriptableObject, ISerializationCallbackReceiver
{
    public List<LineupInfo> lineups = new();

#if UNITY_EDITOR
    /* 反序列化后做一次简单校验（编辑器下才跑） */
    public void OnAfterDeserialize()
    {
        foreach (var l in lineups)
        {
            if (!l.ratio.IsValid())
                Debug.LogWarning($"[LineupDB] 阵容「{l.mainGeneral}」兵种比例不等于 100。", this);
        }
    }
#else
    public void OnAfterDeserialize() { }
#endif
    public void OnBeforeSerialize() { }
}

/* ───────── CardDatabase 扩展辅助 ───────── */
/* 如果你的 CardDatabase 已经有查找方法，可以删掉这段 */

public static class CardDatabaseExtensions
{
    /// <summary>按 cardName 查找；不存在则返回 null</summary>
    public static CardInfo FindByName(this CardDatabase db, string name)
    {
        return db.cards.Find(c => c.cardName == name);
    }
}
