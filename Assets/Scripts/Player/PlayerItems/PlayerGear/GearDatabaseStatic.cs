// Assets/Scripts/Game/Data/GearDatabaseStatic.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 装备静态数据库（ScriptableObject）。
    /// 由 GearDatabaseImporter 在 Editor 中导入 JSON 生成。
    /// </summary>
    [CreateAssetMenu(menuName = "GameDB/GearStaticDB")]
    public class GearDatabaseStatic : ScriptableObject
    {
        /*───────── 1. 序列化字段 ─────────*/
        [Header("所有装备条目")]
        [SerializeField] private List<GearStatic> gears = new();

        [Header("公式系数表（按 Tier）")]
        [SerializeField] private TierFloatPair[] valueCoeArr;       // 攻防系数
        [SerializeField] private TierFloatPair[] valuePowArr;

        [SerializeField] private TierFloatPair[] ironCostCoeArr;    // 基础铁
        [SerializeField] private TierFloatPair[] ironCostPowArr;

        [SerializeField] private TierFloatPair[] fineWoodCoeArr;    // 精木
        [SerializeField] private TierFloatPair[] fineWoodPowArr;

        [SerializeField] private TierFloatPair[] steelCoeArr;       // 精钢
        [SerializeField] private TierFloatPair[] steelPowArr;

        /*───────── 2. 运行时字典 ─────────*/
        private Dictionary<string, GearStatic> id2Gear;

        private Dictionary<Tier, float> valueCoe,  valuePow,
                                        ironCostCoe,  ironCostPow,
                                        fineWoodCoe,  fineWoodPow,
                                        steelCoe,     steelPow;

        /*───────── 3. 单例加载 ─────────*/
        private static GearDatabaseStatic _instance;
        public  static GearDatabaseStatic Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = Resources.Load<GearDatabaseStatic>("GearStaticDB");
                if (_instance == null)
                {
                    Debug.LogError("【GearDB】未找到 GearStaticDB.asset！" +
                                   "请在菜单 Tools → Import Gear JSON… 生成。");
                    return null;
                }

                _instance.BuildLookup();
                return _instance;
            }
        }

        /*───────── 4. 构建所有查表字典 ─────────*/
        private void BuildLookup()
        {
            if (id2Gear != null) return;      // 只做一次

            id2Gear     = new();
            valueCoe    = new();  valuePow    = new();
            ironCostCoe = new();  ironCostPow = new();
            fineWoodCoe = new();  fineWoodPow = new();
            steelCoe    = new();  steelPow    = new();

            foreach (var g in  gears)           id2Gear[g.id]           = g;
            foreach (var p in valueCoeArr)      valueCoe[p.tier]        = p.value;
            foreach (var p in valuePowArr)      valuePow[p.tier]        = p.value;
            foreach (var p in ironCostCoeArr)   ironCostCoe[p.tier]     = p.value;
            foreach (var p in ironCostPowArr)   ironCostPow[p.tier]     = p.value;
            foreach (var p in fineWoodCoeArr)   fineWoodCoe[p.tier]     = p.value;
            foreach (var p in fineWoodPowArr)   fineWoodPow[p.tier]     = p.value;
            foreach (var p in steelCoeArr)      steelCoe[p.tier]        = p.value;
            foreach (var p in steelPowArr)      steelPow[p.tier]        = p.value;
        }

        /*───────── 5. 外部查询接口 ─────────*/

        // 找静态条目
        public GearStatic Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id2Gear == null) BuildLookup();      // 确保已初始化
            id2Gear.TryGetValue(id, out var g);
            return g;
        }

        // 攻防基数
        public float GetValueCoe  (Tier t) => valueCoe[t];
        public float GetValuePow  (Tier t) => valuePow[t];

        // 升级材料·铁
        public float GetIronCostCoe(Tier t) => ironCostCoe[t];
        public float GetIronCostPow(Tier t) => ironCostPow[t];

        // 升级材料·精木
        public float GetFineWoodCoe(Tier t) => fineWoodCoe[t];
        public float GetFineWoodPow(Tier t) => fineWoodPow[t];

        // 升级材料·精钢
        public float GetSteelCoe   (Tier t) => steelCoe[t];
        public float GetSteelPow   (Tier t) => steelPow[t];

        /*───────── 6. util 结构体 ─────────*/
        [Serializable] public struct TierFloatPair
        {
            public Tier  tier;
            public float value;
        }
    }
}
