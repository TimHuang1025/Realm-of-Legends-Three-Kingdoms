// Assets/Scripts/Game/Core/StarUpgradeSystem.cs
using Game.Data;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 升星系统：判断碎片→扣碎片→★ +1 → 同步技能等级
    /// </summary>
    public static class StarUpgradeSystem
    {
        /*──────── 单例缓存 ────*/
        static CardDatabaseStatic DB =>
            _db ??= Resources.Load<CardDatabaseStatic>("CardDatabaseStatic");
        static CardDatabaseStatic _db;

        static PlayerResources RES =>
            _res ??= Resources.Load<PlayerResources>("PlayerResources");
        static PlayerResources _res;

        /*──────── API ─────────*/

        /// <summary>能否升到下一星？返回对应规则</summary>
        public static bool CanUpgrade(PlayerCard pc, out StarUpgradeRule rule)
        {
            rule = DB.GetStar(pc.star + 1);            // ★1‒15 规则
            if (rule == null) return false;            // 已满星
            return RES.upTierMaterial >= rule.shardsRequired;
            
        }

        /// <summary>执行升星；成功返回 true</summary>
        public static bool TryUpgrade(PlayerCard pc)
        {
            if (!CanUpgrade(pc, out var rule)) return false;

            /* 1) 扣统一碎片 & 星级 +1 */
            RES.upTierMaterial -= rule.shardsRequired;
            pc.star += 1;
            PlayerCardBankMgr.I?.MarkDirty(pc.id);

            return true;
        }
    }
}
