using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class GachaSystem
{
    /* 抽 1 个 */
    public static string RollOne(GachaPoolInfo pool)
    {
        int total = pool.drops.Sum(d => d.weight);
        int r = Random.Range(1, total + 1);

        int cum = 0;
        foreach (var d in pool.drops)
        {
            cum += d.weight;
            if (r <= cum) return d.rewardId;
        }
        return null;
    }

    /* 抽 n 个，返回结果列表 */
    public static List<string> Roll(GachaPoolInfo pool, int n)
    {
        var res = new List<string>(n);
        for (int i = 0; i < n; i++)
            res.Add(RollOne(pool));
        return res;
    }
}
