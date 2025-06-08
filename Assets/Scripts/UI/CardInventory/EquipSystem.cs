// Assets/Scripts/Game/Core/EquipSystem.cs
using Game.Data;   // GearStatic
using UnityEngine;

namespace Game.Core
{
    /// <summary>装备槽类型</summary>
    public enum EquipSlotType { Weapon, Armor, Mount }

    /// <summary>穿戴 / 卸下 / 解锁 的统一入口</summary>
    public static class EquipSystem
    {
        public static bool EquipGear(PlayerCard card, GearStatic gear)
        {
            if (gear.kind == GearKind.Weapon && card.equip.weaponUnlocked)
            {
                card.equip.weaponId = gear.id;
                return true;
            }
            if (gear.kind == GearKind.Armor && card.equip.armorUnlocked)
            {
                card.equip.armorId = gear.id;
                return true;
            }
            return false;
        }

        // 以后加 Mount 相关时再扩展
    }
}
