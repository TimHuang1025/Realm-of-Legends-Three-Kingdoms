// Assets/Scripts/Game/Core/StarUpgradeSystem.cs
using Game.Data;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 升星系统：先用自身碎片，允许（可选）用通用碎片补足，再升★
    /// </summary>
    public static class StarUpgradeSystem
    {
        /*──────── 静态表缓存 ────*/
        private static CardDatabaseStatic DB =>
            _db ??= Resources.Load<CardDatabaseStatic>("CardDatabaseStatic");
        private static CardDatabaseStatic _db;

        private static PlayerResources RES =>
            _res ??= Resources.Load<PlayerResources>("PlayerResources");
        private static PlayerResources _res;

        /*──────── 公共 API ──────*/

        public static bool CanUpgrade(PlayerCard pc,
                                      CardInfoStatic info,
                                      bool useGeneral,
                                      out StarUpgradeRule rule)
        {
            rule = DB.GetStar(pc.star + 1);
            if (rule == null) return false;                 // 已满星

            int need = rule.shardsRequired;
            int havePersonal = pc.copies;

            if (havePersonal >= need) return true;          // 自身够

            if (!useGeneral) return false;                  // 未勾选

            int shortage = need - havePersonal;
            int haveGeneral = GetGeneralShardCount(info.tier);

            return haveGeneral >= shortage;                 // 通用能补足
        }

        public static bool TryUpgrade(PlayerCard pc,
                                      CardInfoStatic info,
                                      bool useGeneral)
        {
            if (!CanUpgrade(pc, info, useGeneral, out var rule)) return false;

            int need         = rule.shardsRequired;
            int personalUsed = Mathf.Min(pc.copies, need);
            int shortage     = need - personalUsed;

            /* 1) 扣自身碎片 */
            pc.copies -= personalUsed;

            /* 2) 如有缺口且允许，用通用碎片补齐 */
            if (shortage > 0)
                ConsumeGeneralShards(info.tier, shortage);

            /* 3) ★ +1 & 通知刷新 */
            pc.star += 1;
            PlayerCardBankMgr.I?.MarkDirty(pc.id);

            return true;
        }

        /*──────── 私有工具 ──────*/

        private static int GetGeneralShardCount(Tier t) => t switch      // Tier 枚举定义在静态表 :contentReference[oaicite:0]{index=0}
        {
            Tier.S => RES.heroCrestSGeneral,
            Tier.A => RES.heroCrestAGeneral,
            Tier.B => RES.heroCrestBGeneral,
            _      => 0
        };

        private static void ConsumeGeneralShards(Tier t, int amount)
        {
            switch (t)
            {
                case Tier.S:
                    RES.heroCrestSGeneral -= amount;
                    break;
                case Tier.A:
                    RES.heroCrestAGeneral -= amount;
                    break;
                case Tier.B:
                    RES.heroCrestBGeneral -= amount;
                    break;
            }
        }
    }
}
