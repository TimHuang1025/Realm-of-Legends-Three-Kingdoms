using System.Collections.Generic;
using UnityEngine;
using System.Linq;    

/*───────────────────────────────────────*
 *  兵种枚举 & 比例
 *───────────────────────────────────────*/

public enum TroopType
{
    Infantry,
    Cavalry,
    Archer
}

[System.Serializable]
public struct TroopRatio
{
    [Range(0, 100)] public int infantry;
    [Range(0, 100)] public int cavalry;
    [Range(0, 100)] public int archer;

    public bool IsValid() => infantry + cavalry + archer == 100;
}

/*───────────────────────────────────────*
 *  单条阵容（只存武将 id）
 *───────────────────────────────────────*/

[System.Serializable]
public class LineupInfo
{
    [Header("武将 id（对应 CardInfoStatic.id）")]
    public string mainId;
    public string subId1;
    public string subId2;
    public string strategistId;

    [Header("兵种比例（总和 100）")]
    public TroopRatio ratio;

    /// <summary>把 id 解析为静态卡；找不到则返回 null</summary>
    public ResolvedLineup Resolve(CardDatabaseStatic db)
    {
        return new ResolvedLineup
        {
            mainGeneral = db?.Get(mainId),
            subGeneral1 = db?.Get(subId1),
            subGeneral2 = db?.Get(subId2),
            strategist  = db?.Get(strategistId),
            ratio       = ratio
        };
    }
}

/*───────────────────────────────────────*
 *  解析后的结构（方便直接用静态卡）
 *───────────────────────────────────────*/

public struct ResolvedLineup
{
    public CardInfoStatic mainGeneral;
    public CardInfoStatic subGeneral1;
    public CardInfoStatic subGeneral2;
    public CardInfoStatic strategist;
    public TroopRatio     ratio;
}

/*───────────────────────────────────────*
 *  阵容数据库 ScriptableObject
 *───────────────────────────────────────*/

[CreateAssetMenu(fileName = "LineupDB",
                 menuName = "Lineup/Database",
                 order = 0)]
public class LineupDatabase : ScriptableObject, ISerializationCallbackReceiver
{
    [Tooltip("最多五套阵容；可在 Inspector 中自行增删")]
    public List<LineupInfo> lineups = new();

#if UNITY_EDITOR
    /* 反序列化后在编辑器里简单校验一次比例是否合法 */
    public void OnAfterDeserialize()
    {
        foreach (var l in lineups)
        {
            int sum = l.ratio.infantry + l.ratio.cavalry + l.ratio.archer;
            if (sum == 100) continue;

            // 把三个值按比例缩放到 100
            if (sum == 0) { l.ratio.infantry = 34; l.ratio.cavalry = 33; l.ratio.archer = 33; }
            else
            {
                l.ratio.infantry = Mathf.RoundToInt(l.ratio.infantry * 100f / sum);
                l.ratio.cavalry  = Mathf.RoundToInt(l.ratio.cavalry  * 100f / sum);
                l.ratio.archer   = 100 - l.ratio.infantry - l.ratio.cavalry;
            }
            Debug.Log($"[LineupDB] 自动归一化比例为 {l.ratio.infantry}/{l.ratio.cavalry}/{l.ratio.archer}", this);
        }
    }
#else
    public void OnAfterDeserialize() { }
#endif

    /* 目前不需要序列化前处理 */
    public void OnBeforeSerialize() { }
}

/*───────────────────────────────────────*
 *  简易扩展：按 id 获取静态卡
 *───────────────────────────────────────*/

public static class CardDatabaseStaticExt
{
    /// <summary>按 id 查找；找不到返回 null</summary>
    public static CardInfoStatic Get(this CardDatabaseStatic db, string id)
    {
        if (db == null || string.IsNullOrEmpty(id))
            return null;

        // IReadOnlyList 上用 LINQ
        return db.All.FirstOrDefault(c => c.id == id);
    }
}
