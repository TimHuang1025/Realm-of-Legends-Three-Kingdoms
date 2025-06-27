// ────────────────────────────────────────────────────────────────────────────────
// Assets/Scripts/Game/Core/PlayerResourceBank.cs
// ────────────────────────────────────────────────────────────────────────────────
using System;
using System.IO;
using UnityEngine;

/// <summary>统一管理玩家资源（单例 + JSON 持久化）</summary>
public class PlayerResourceBank : MonoBehaviour
{
    public static PlayerResourceBank I { get; private set; }

    [SerializeField] private PlayerResources data;    // Inspector 拖入 ScriptableObject
    private const string SaveFile = "player_bank.json";

    /*──────────────── 生命周期 ─────────────────*/
    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        Load();                                       // 启动时读取本地存档
    }

    /*──────────────── 查询接口 ─────────────────*/
    public long this[ResourceType t] => t switch
    {
        // —— 基础货币 ——
        ResourceType.Gold         => data.gold,
        ResourceType.Silver       => data.silver,
        ResourceType.Copper       => data.copper,
        ResourceType.Food         => data.food,

        // —— 通用令牌 / 经验 ——
        ResourceType.SummonWrit   => data.summonWrit,
        ResourceType.TroopOrder   => data.troopOrder,
        ResourceType.UnionMerit   => data.unionMerit,
        ResourceType.Merit        => data.merit,
        ResourceType.AdvanceScroll=> data.advanceScroll,

        // —— 武将碎片 ——
        ResourceType.HeroCrest_SGeneral => data.heroCrestSGeneral,
        ResourceType.HeroCrest_AGeneral => data.heroCrestAGeneral,
        ResourceType.HeroCrest_BGeneral => data.heroCrestBGeneral,

        // —— 常用材料 ——
        ResourceType.Forage       => data.forage,
        ResourceType.RefinedIron  => data.refinedIron,
        ResourceType.FineWood     => data.fineWood,
        ResourceType.FineSteel    => data.fineSteel,
        ResourceType.TechScroll   => data.techScroll,
        ResourceType.Fame         => data.fame,
        ResourceType.ArtOfWar     => data.artOfWar,
        ResourceType.TrainingToken=> data.trainingToken,
        ResourceType.RespecPotion => data.respecPotion,
        ResourceType.RawIron      => data.rawIron,
        ResourceType.Firmwood     => data.firmwood,
        ResourceType.IronIngot    => data.ironIngot,
        ResourceType.Honor        => data.honor,
        ResourceType.MerchantDeed => data.merchantDeed,
        ResourceType.LandDeed     => data.landDeed,

        _                         => 0
    };

    /*──────────────── 修改接口 ─────────────────*/
    public void Add(ResourceType t, long amount)  => Modify(t,  Math.Max(0, amount));
    public bool Spend(ResourceType t, long amount)
    {
        if (this[t] < amount) return false;
        Modify(t, -amount);
        return true;
    }

    private void Modify(ResourceType t, long delta)
    {
        switch (t)
        {
            // —— 基础货币 ——
            case ResourceType.Gold:   data.gold   += delta; break;
            case ResourceType.Silver: data.silver += delta; break;
            case ResourceType.Copper: data.copper += delta; break;
            case ResourceType.Food:   data.food   += delta; break;

            // —— 通用令牌 / 经验 ——
            case ResourceType.SummonWrit:   data.summonWrit   += (int)delta; break;
            case ResourceType.TroopOrder:   data.troopOrder   += (int)delta; break;
            case ResourceType.UnionMerit:   data.unionMerit   += delta;      break;
            case ResourceType.Merit:        data.merit        += delta;      break;
            case ResourceType.AdvanceScroll:data.advanceScroll+= (int)delta; break;

            // —— 武将碎片 ——
            case ResourceType.HeroCrest_SGeneral: data.heroCrestSGeneral += (int)delta; break;
            case ResourceType.HeroCrest_AGeneral: data.heroCrestAGeneral += (int)delta; break;
            case ResourceType.HeroCrest_BGeneral: data.heroCrestBGeneral += (int)delta; break;

            // —— 常用材料 ——
            case ResourceType.Forage:        data.forage        += delta;      break;
            case ResourceType.RefinedIron:   data.refinedIron   += delta;      break;
            case ResourceType.FineWood:      data.fineWood      += delta;      break;
            case ResourceType.FineSteel:     data.fineSteel     += delta;      break;
            case ResourceType.TechScroll:    data.techScroll    += delta;      break;
            case ResourceType.Fame:          data.fame          += delta;      break;
            case ResourceType.ArtOfWar:      data.artOfWar      += (int)delta; break;
            case ResourceType.TrainingToken: data.trainingToken += (int)delta; break;
            case ResourceType.RespecPotion:  data.respecPotion  += (int)delta; break;
            case ResourceType.RawIron:       data.rawIron       += delta;      break;
            case ResourceType.Firmwood:      data.firmwood      += delta;      break;
            case ResourceType.IronIngot:     data.ironIngot     += delta;      break;
            case ResourceType.Honor:         data.honor         += (int)delta; break;
            case ResourceType.MerchantDeed:  data.merchantDeed  += (int)delta; break;
            case ResourceType.LandDeed:      data.landDeed      += (int)delta; break;
        }

        onBankChanged?.Invoke(t);
        Save();
    }

    /*──────────────── 事件 ─────────────────*/
    public Action<ResourceType> onBankChanged;

    /*──────────────── 存档结构 ─────────────────*/
    [Serializable] private class SaveStruct
    {
        // 基础货币
        public long gold, silver, copper, food;

        // 通用令牌 / 经验
        public int summonWrit, troopOrder;
        public long unionMerit, merit;
        public int advanceScroll;

        // 武将碎片
        public int heroCrestSGeneral, heroCrestAGeneral, heroCrestBGeneral;

        // 常用材料
        public long forage, refinedIron, fineWood, fineSteel, techScroll, fame;
        public int  artOfWar, trainingToken, respecPotion;
        public long rawIron, firmwood, ironIngot;
        public int  honor, merchantDeed, landDeed;
    }

    /*──────────────── 保存 ─────────────────*/
    private void Save()
    {
        var s = new SaveStruct
        {
            // 基础货币
            gold   = data.gold,
            silver = data.silver,
            copper = data.copper,
            food   = data.food,

            // 通用令牌 / 经验
            summonWrit   = data.summonWrit,
            troopOrder   = data.troopOrder,
            unionMerit   = data.unionMerit,
            merit        = data.merit,
            advanceScroll= data.advanceScroll,

            // 武将碎片
            heroCrestSGeneral = data.heroCrestSGeneral,
            heroCrestAGeneral = data.heroCrestAGeneral,
            heroCrestBGeneral = data.heroCrestBGeneral,

            // 常用材料
            forage        = data.forage,
            refinedIron   = data.refinedIron,
            fineWood      = data.fineWood,
            fineSteel     = data.fineSteel,
            techScroll    = data.techScroll,
            fame          = data.fame,
            artOfWar      = data.artOfWar,
            trainingToken = data.trainingToken,
            respecPotion  = data.respecPotion,
            rawIron       = data.rawIron,
            firmwood      = data.firmwood,
            ironIngot     = data.ironIngot,
            honor         = data.honor,
            merchantDeed  = data.merchantDeed,
            landDeed      = data.landDeed
        };

        File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveFile),
                          JsonUtility.ToJson(s));
    }

    /*──────────────── 加载 ─────────────────*/
    private void Load()
    {
        string path = Path.Combine(Application.persistentDataPath, SaveFile);
        if (!File.Exists(path)) return;

        var s = JsonUtility.FromJson<SaveStruct>(File.ReadAllText(path));

        // 基础货币
        data.gold   = s.gold;
        data.silver = s.silver;
        data.copper = s.copper;
        data.food   = s.food;

        // 通用令牌 / 经验
        data.summonWrit   = s.summonWrit;
        data.troopOrder   = s.troopOrder;
        data.unionMerit   = s.unionMerit;
        data.merit        = s.merit;
        data.advanceScroll= s.advanceScroll;

        // 武将碎片
        data.heroCrestSGeneral = s.heroCrestSGeneral;
        data.heroCrestAGeneral = s.heroCrestAGeneral;
        data.heroCrestBGeneral = s.heroCrestBGeneral;

        // 常用材料
        data.forage        = s.forage;
        data.refinedIron   = s.refinedIron;
        data.fineWood      = s.fineWood;
        data.fineSteel     = s.fineSteel;
        data.techScroll    = s.techScroll;
        data.fame          = s.fame;
        data.artOfWar      = s.artOfWar;
        data.trainingToken = s.trainingToken;
        data.respecPotion  = s.respecPotion;
        data.rawIron       = s.rawIron;
        data.firmwood      = s.firmwood;
        data.ironIngot     = s.ironIngot;
        data.honor         = s.honor;
        data.merchantDeed  = s.merchantDeed;
        data.landDeed      = s.landDeed;
    }
}
