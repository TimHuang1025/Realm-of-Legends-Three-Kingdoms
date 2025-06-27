// ────────────────────────────────────────────────────────────────────────────────
// Assets/Scripts/Game/PlayerLordCard.cs
// 管理主公等级 / 声望 / 技能点分配 + 技能槽(1 主动+2 被动) + 技能等级 + 洗点
// ────────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace Game
{
    public enum AttributeType { Atk, Def, IQ }
    public enum SkillSlot     { Active, Passive1, Passive2 }

    [CreateAssetMenu(fileName = "PlayerLordCard", menuName = "Game/Player Lord Card")]
    public class PlayerLordCard : ScriptableObject
    {
        /*──────── 等级 / 声望 ────────*/
        [Header("等级 / 声望")]
        public int currentLevel = 1;
        public int currentFame  = 0;

        /*──────── 技能槽 ────────*/
        [Header("技能槽（1 主动 + 2 被动）")]
        public string activeSkillId  = "A1";
        public string passiveOneId   = "P11";
        public string passiveTwoId   = "P14";

        /*──────── 技能等级 ────────*/
        [Header("技能等级 (0~4)")]
        [Range(0,4)] public int activeSkillLv  = 0;
        [Range(0,4)] public int passiveOneLv   = 0;
        [Range(0,4)] public int passiveTwoLv   = 0;

        /*──────── 技能点 ────────*/
        [Header("技能点")]
        public int totalSkillPointsEarned = 0;
        public int availableSkillPoints   = 0;

        /*──────── 基础属性 ────────*/
        [Header("基础属性 (不可变)")]
        [SerializeField] private int baseAtk = 100;
        [SerializeField] private int baseDef = 100;
        [SerializeField] private int baseIQ  = 100;

        /* 当前属性（= 基础 + 加点加成） */
        [Header("当前属性值")]
        public int atk;
        public int def;
        public int iq;

        /*──────── 已用技能点 ────────*/
        [Header("各项已用技能点")]
        public int atkPointsUsed = 0;
        public int defPointsUsed = 0;
        public int iqPointsUsed  = 0;

        /*──────── 初始化 ────────*/
        void OnEnable()
        {
            if (atk == 0 && def == 0 && iq == 0)
            {
                atk = baseAtk;
                def = baseDef;
                iq  = baseIQ;
            }
        }

        // ───────────────────── 声望升级 ─────────────────────
        public bool AddFame(int amount, LordCardStaticData staticData)
        {
            if (staticData == null) return false;
            bool lvUp = false;

            currentFame += Mathf.Max(0, amount);

            while (currentLevel < staticData.currentMax)
            {
                var next = staticData.GetLevel(currentLevel + 1);
                if (next == null || currentFame < next.requiredFame) break;

                currentLevel++;
                totalSkillPointsEarned += next.skillPoints;
                availableSkillPoints   += next.skillPoints;
                lvUp = true;
            }
            return lvUp;
        }

        public bool TryLevelUpWithFame(LordCardStaticData staticData)
        {
            if (staticData == null || PlayerResourceBank.I == null) return false;
            if (currentLevel >= staticData.currentMax) return false;

            var next = staticData.GetLevel(currentLevel + 1);
            if (next == null) return false;

            long fameOwned = PlayerResourceBank.I[ResourceType.Fame];
            if (fameOwned < next.requiredFame) return false;

            PlayerResourceBank.I.Spend(ResourceType.Fame, (int)next.requiredFame);
            currentLevel++;
            totalSkillPointsEarned += next.skillPoints;
            availableSkillPoints   += next.skillPoints;
            return true;
        }

        // ───────────────────── 加点 ─────────────────────
        public bool SpendSkillPoints(AttributeType attr, int pts, LordCardStaticData staticData)
        {
            if (staticData == null || pts <= 0 || availableSkillPoints < pts) return false;
            int inc = staticData.valuePerSkillPoint * pts;

            switch (attr)
            {
                case AttributeType.Atk: atk += inc; atkPointsUsed += pts; break;
                case AttributeType.Def: def += inc; defPointsUsed += pts; break;
                case AttributeType.IQ:  iq  += inc; iqPointsUsed  += pts; break;
            }
            availableSkillPoints -= pts;
            return true;
        }

        // ───────────────────── 洗点 ─────────────────────
        public void RefundAllSkillPoints()
        {
            int refunded = atkPointsUsed + defPointsUsed + iqPointsUsed;
            availableSkillPoints += refunded;

            atkPointsUsed = defPointsUsed = iqPointsUsed = 0;

            atk = baseAtk;
            def = baseDef;
            iq  = baseIQ;
        }

        /*──────── 属性便捷访问 ────────*/
        public int GetAttribute(AttributeType t) =>
            t == AttributeType.Atk ? atk :
            t == AttributeType.Def ? def : iq;

        public int GetUsedPoints(AttributeType t) =>
            t == AttributeType.Atk ? atkPointsUsed :
            t == AttributeType.Def ? defPointsUsed : iqPointsUsed;

        /*──────── 技能槽便捷 ────────*/
        public string GetSkillId(SkillSlot s) => s switch
        {
            SkillSlot.Active   => activeSkillId,
            SkillSlot.Passive1 => passiveOneId,
            _                  => passiveTwoId
        };
        public void SetSkillId(SkillSlot s, string id)
        {
            if      (s == SkillSlot.Active)   activeSkillId  = id;
            else if (s == SkillSlot.Passive1) passiveOneId   = id;
            else                              passiveTwoId   = id;
        }

        /*──────── 技能等级便捷 ────────*/
        public int GetSkillLevel(SkillSlot s) => s switch
        {
            SkillSlot.Active   => activeSkillLv,
            SkillSlot.Passive1 => passiveOneLv,
            _                  => passiveTwoLv
        };
        public void SetSkillLevel(SkillSlot s, int lv)
        {
            lv = Mathf.Clamp(lv, 0, 4);
            if      (s == SkillSlot.Active)   activeSkillLv  = lv;
            else if (s == SkillSlot.Passive1) passiveOneLv   = lv;
            else                              passiveTwoLv   = lv;
        }
    }
}
