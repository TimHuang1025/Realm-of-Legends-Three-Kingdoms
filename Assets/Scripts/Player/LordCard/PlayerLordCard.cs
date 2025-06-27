// ────────────────────────────────────────────────────────────────────────────────
// Assets/Scripts/Game/PlayerLordCard.cs
// 管理主公等级 / 声望 / 技能点分配（Atk Def IQ）+ 洗点功能
// ────────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace Game
{
    public enum AttributeType { Atk, Def, IQ }

    [CreateAssetMenu(fileName = "PlayerLordCard", menuName = "Game/Player Lord Card")]
    public class PlayerLordCard : ScriptableObject
    {
        /*──────── 等级 / 声望 ────────*/
        [Header("等级 / 声望")]
        public int currentLevel = 1;
        public int currentFame  = 0;

        /*──────── 技能点 ────────*/
        [Header("技能点")]
        public int totalSkillPointsEarned = 0;
        public int availableSkillPoints   = 0;

        /*──────── 基础属性 (起始 100) ────────*/
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
        private void OnEnable()
        {
            // 确保第一次创建时属性等于基础值
            if (atk == 0 && def == 0 && iq == 0)
            {
                atk = baseAtk;
                def = baseDef;
                iq  = baseIQ;
            }
        }

        // ───────────────────── 升级方法 ①：自然累积声望 ─────────────────────
        public bool AddFame(int amount, LordCardStaticData staticData)
        {
            if (staticData == null) return false;
            bool leveledUp = false;

            currentFame += Mathf.Max(0, amount);

            while (currentLevel < staticData.currentMax)
            {
                var next = staticData.GetLevel(currentLevel + 1);
                if (next == null || currentFame < next.requiredFame) break;

                currentLevel++;
                totalSkillPointsEarned += next.skillPoints;
                availableSkillPoints   += next.skillPoints;
                leveledUp = true;
            }
            return leveledUp;
        }

        // ───────────────────── 升级方法 ②：直接扣 Fame ─────────────────────
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
        public bool SpendSkillPoints(AttributeType attr, int points, LordCardStaticData staticData)
        {
            if (staticData == null || points <= 0 || availableSkillPoints < points) return false;
            int inc = staticData.valuePerSkillPoint * points;

            switch (attr)
            {
                case AttributeType.Atk:
                    atk += inc; atkPointsUsed += points; break;
                case AttributeType.Def:
                    def += inc; defPointsUsed += points; break;
                case AttributeType.IQ:
                    iq  += inc; iqPointsUsed  += points; break;
            }
            availableSkillPoints -= points;
            return true;
        }

        // ───────────────────── 洗点 ─────────────────────
        /// <summary>返还所有已投入点数 & 还原属性到基础值</summary>
        public void RefundAllSkillPoints()
        {
            int refunded = atkPointsUsed + defPointsUsed + iqPointsUsed;
            availableSkillPoints += refunded;

            atkPointsUsed = defPointsUsed = iqPointsUsed = 0;

            atk = baseAtk;
            def = baseDef;
            iq  = baseIQ;
        }

        /*──────── 便捷 getter ────────*/
        public int GetAttribute(AttributeType t) =>
            t == AttributeType.Atk ? atk :
            t == AttributeType.Def ? def : iq;

        public int GetUsedPoints(AttributeType t) =>
            t == AttributeType.Atk ? atkPointsUsed :
            t == AttributeType.Def ? defPointsUsed : iqPointsUsed;
    }
}
