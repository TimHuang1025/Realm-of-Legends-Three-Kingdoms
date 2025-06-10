// ────────────────────────────────────────────────────────────────────────────────
// HorseDatabaseStatic.cs   （仅数据库本体，不含 HorseStatic 定义）
// ────────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "GameDB/HorseStaticDB")]
    public class HorseDatabaseStatic : ScriptableObject
    {
        /*───────────── 静态列表 ─────────────*/
        [SerializeField] private List<HorseStatic> horses = new();

        /*───────────── Importer 会写入的四组系数 ─────────────*/
        [Serializable] public struct TierFloatPair
        {
            public HorseTier tier;
            public float     value;
        }

        [Header("属性公式参数")]
        [SerializeField] private TierFloatPair[] valueCoeArr;
        [SerializeField] private TierFloatPair[] valuePowArr;

        [Header("升级花费参数")]
        [SerializeField] private TierFloatPair[] costCoeArr;
        [SerializeField] private TierFloatPair[] costPowArr;

        /*───────────── 运行时字典 ─────────────*/
        private Dictionary<string, HorseStatic> id2Horse;
        private Dictionary<HorseTier, float>    valueCoe, valuePow, costCoe, costPow;

        /*───────────── 单例 ─────────────*/
        public static HorseDatabaseStatic Instance { get; private set; }

        void OnEnable()
        {
            Instance = this;

            // 1) 静态条目查表
            id2Horse = new Dictionary<string, HorseStatic>(horses.Count);
            foreach (var h in horses)
            {
                if (string.IsNullOrEmpty(h.id)) continue;
                id2Horse[h.id] = h;
            }

            // 2) 参数字典
            valueCoe = ToDict(valueCoeArr);
            valuePow = ToDict(valuePowArr);
            costCoe  = ToDict(costCoeArr);
            costPow  = ToDict(costPowArr);
        }

        /*──────────────── 对外接口 ────────────────*/
        public HorseStatic Get(string id) =>
            string.IsNullOrEmpty(id) ? null :
            (id2Horse != null && id2Horse.TryGetValue(id, out var h) ? h : null);

        public IReadOnlyList<HorseStatic> All => horses;

        /*── 供 HorseStatic 调用的四个方法 ──*/
        public float GetValueCoe(HorseTier t) => valueCoe[t];
        public float GetValuePow(HorseTier t) => valuePow[t];
        public float GetCostCoe (HorseTier t) => costCoe[t];
        public float GetCostPow (HorseTier t) => costPow[t];

        /*──────────────── 私有助手 ────────────────*/
        static Dictionary<HorseTier,float> ToDict(TierFloatPair[] src)
        {
            var d = new Dictionary<HorseTier, float>(src?.Length ?? 0);
            if (src == null) return d;
            foreach (var p in src) d[p.tier] = p.value;
            return d;
        }
    }
}
