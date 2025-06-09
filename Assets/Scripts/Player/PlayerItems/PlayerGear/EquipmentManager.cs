using Game.Data;

namespace Game.Core
{
    /// <summary>
    /// 负责「穿 / 卸」装备的一切边界情况处理：
    /// 1) 一件装备只能属于一名武将 → 新主人穿上时自动把旧主人卸下  
    /// 2) 一名武将同一槽位只能有一件 → 换新装备时自动把旧装备解绑  
    /// 3) 更新两个 Bank（Card & Gear），并触发 onCardUpdated
    /// </summary>
    public static class EquipmentManager
    {
        /// <param name="hero">要穿装备的武将</param>
        /// <param name="gear">目标装备（PlayerGear 数据）</param>
        /// <param name="slot">Weapon / Armor / Mount</param>
        public static void Equip(
            PlayerCard hero,
            PlayerGear gear,
            EquipSlotType slot,
            PlayerCardBank cardBank,
            PlayerGearBank gearBank)
        {
            if (hero == null || gear == null) return;

            /*──────── 1) 若这件装备原本在别人身上 → 先卸下 ────────*/
            if (!string.IsNullOrEmpty(gear.equippedById) &&
                gear.equippedById != hero.id)
            {
                var oldHero = cardBank.Get(gear.equippedById);
                if (oldHero != null)
                    ClearSlot(oldHero, slot, gearBank);
            }

            /*──────── 2) 如果该槽已有旧装备 → 卸下 ────────*/
            ClearSlot(hero, slot, gearBank);

            /*──────── 3) 穿新装备 (写入 uuid) ────────*/
            switch (slot)
            {
                case EquipSlotType.Weapon: hero.equip.weaponUuid = gear.uuid; break;
                case EquipSlotType.Armor: hero.equip.armorUuid = gear.uuid; break;
                case EquipSlotType.Mount: hero.equip.accessoryUuid = gear.uuid; break;
            }
            gear.equippedById = hero.id;

            /*──────── 4) 标记脏数据 ────────*/
            cardBank.MarkDirty(hero.id);
            gearBank.MarkDirty(gear.uuid);

            /*──────── 5) 触发刷新事件 ────────*/
            PlayerCardBankMgr.I?.RaiseCardUpdated(hero.id);
            cardBank.Save();   // 把 PlayerCardBank 写到 json
            gearBank.Save();   // 把 PlayerGearBank 写到 json
        }

        /// <summary>把指定槽清空；若 gearBank!=null 同时把旧装备归为闲置并打脏标</summary>
        public static void ClearSlot(
            PlayerCard      hero,
            EquipSlotType   slot,
            PlayerGearBank  gearBank = null)
        {
            if (hero == null) return;

            string oldGearUuid = "";
            switch (slot)
            {
                case EquipSlotType.Weapon:
                    oldGearUuid        = hero.equip.weaponUuid;
                    hero.equip.weaponUuid = "";
                    break;
                case EquipSlotType.Armor:
                    oldGearUuid        = hero.equip.armorUuid;
                    hero.equip.armorUuid  = "";
                    break;
                case EquipSlotType.Mount:
                    oldGearUuid            = hero.equip.accessoryUuid;
                    hero.equip.accessoryUuid = "";
                    break;
            }

            if (gearBank != null && !string.IsNullOrEmpty(oldGearUuid))
            {
                var g = gearBank.Get(oldGearUuid);
                if (g != null)
                {
                    g.equippedById = "";
                    gearBank.MarkDirty(oldGearUuid);   // ← 别忘给旧装备打脏标
                }
            }
        }
    }
}
