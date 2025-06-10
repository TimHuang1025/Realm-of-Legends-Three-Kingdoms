using Game.Data;

namespace Game.Core
{
    /// <summary>
    /// 穿 / 卸 任何装备（武器、防具、战马）的统一入口。<br />
    /// 约定：Mount 槽既可装 Horse，也可装其它 Accessory —— 这里只演示战马。
    /// </summary>
    public static class EquipmentManager
    {
        /*───────────────────────────────────────────────────────────*
         *  ① 原有「武器/防具」方法 (PlayerGear) —— 保持不动
         *───────────────────────────────────────────────────────────*/
        public static void Equip(
            PlayerCard    hero,
            PlayerGear    gear,
            EquipSlotType slot,
            PlayerCardBank cardBank,
            PlayerGearBank gearBank)
        {
            if (hero == null || gear == null) return;

            /* 1) 这件装备原本在别人身上 → 先卸下 */
            if (!string.IsNullOrEmpty(gear.equippedById) &&
                gear.equippedById != hero.id)
            {
                var oldHero = cardBank.Get(gear.equippedById);
                if (oldHero != null)
                    ClearSlot(oldHero, slot, gearBank);
            }

            /* 2) 如果该槽已有旧装备 → 卸下 */
            ClearSlot(hero, slot, gearBank);

            /* 3) 穿上新装备 */
            switch (slot)
            {
                case EquipSlotType.Weapon: hero.equip.weaponUuid     = gear.uuid; break;
                case EquipSlotType.Armor:  hero.equip.armorUuid      = gear.uuid; break;
                case EquipSlotType.Mount:  hero.equip.accessoryUuid  = gear.uuid; break;
            }
            gear.equippedById = hero.id;

            /* 4) 标记脏并存档 */
            cardBank.MarkDirty(hero.id);
            gearBank.MarkDirty(gear.uuid);
            cardBank.Save();
            gearBank.Save();

            /* 5) 通知 UI */
            PlayerCardBankMgr.I?.RaiseCardUpdated(hero.id);
        }

        /*───────────────────────────────────────────────────────────*
         *  ② 新增「战马」重载 (PlayerHorse) —— 仅 Mount 槽
         *───────────────────────────────────────────────────────────*/
        public static void Equip(
            PlayerCard       hero,
            PlayerHorse      horse,
            EquipSlotType    slot,        // 必须传 Mount
            PlayerCardBank   cardBank,
            PlayerHorseBank  horseBank)
        {
            if (hero == null || horse == null || slot != EquipSlotType.Mount) return;

            /* 1) 把这匹马原先的主人卸下 */
            if (!string.IsNullOrEmpty(horse.equippedById) &&
                horse.equippedById != hero.id)
            {
                var oldHero = cardBank.Get(horse.equippedById);
                if (oldHero != null)
                    ClearMount(oldHero, horseBank);
            }

            /* 2) 卸下英雄当前坐骑 */
            ClearMount(hero, horseBank);

            /* 3) 穿上新马 */
            hero.equip.accessoryUuid = horse.uuid;
            horse.equippedById       = hero.id;

            /* 4) 标记脏 & 存档 */
            cardBank.MarkDirty(hero.id);
            horseBank.MarkDirty(horse.uuid);
            cardBank.Save();
            horseBank.Save();

            /* 5) 通知 UI */
            PlayerCardBankMgr.I?.RaiseCardUpdated(hero.id);
        }

        /*───────────────────────────────────────────────────────────*
         *  ③ 通用卸下（旧实现，只处理 Gear）
         *───────────────────────────────────────────────────────────*/
        public static void ClearSlot(
            PlayerCard      hero,
            EquipSlotType   slot,
            PlayerGearBank  gearBank = null)
        {
            if (hero == null) return;

            string oldUuid = "";
            switch (slot)
            {
                case EquipSlotType.Weapon:
                    oldUuid = hero.equip.weaponUuid;
                    hero.equip.weaponUuid = "";
                    break;
                case EquipSlotType.Armor:
                    oldUuid = hero.equip.armorUuid;
                    hero.equip.armorUuid = "";
                    break;
                case EquipSlotType.Mount:
                    oldUuid = hero.equip.accessoryUuid;
                    hero.equip.accessoryUuid = "";
                    break;
            }

            if (gearBank != null && !string.IsNullOrEmpty(oldUuid))
            {
                var g = gearBank.Get(oldUuid);
                if (g != null)
                {
                    g.equippedById = "";
                    gearBank.MarkDirty(oldUuid);
                }
            }
        }

        /*───────────────────────────────────────────────────────────*
         *  ④ 专用卸马（只处理 HorseBank）
         *───────────────────────────────────────────────────────────*/
        static void ClearMount(PlayerCard hero, PlayerHorseBank horseBank)
        {
            if (hero == null || horseBank == null) return;

            string uuid = hero.equip.accessoryUuid;
            if (string.IsNullOrEmpty(uuid)) return;

            var horse = horseBank.Get(uuid);
            if (horse != null)
            {
                horse.equippedById = "";
                horseBank.MarkDirty(uuid);
            }
            hero.equip.accessoryUuid = "";
        }
    }
}
