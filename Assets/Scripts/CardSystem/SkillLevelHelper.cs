using Game.Data;

namespace Game.Core
{
    public static class SkillLevelHelper
    {
        public static int GetSkillLevel(int star, int idx)
        {
            if (star <= 0) return 1;
            var r = CardDBInstance.Instance.GetStar(star);
            return (r != null && idx >= 0 && idx < r.skillLvGain.Length)
                   ? r.skillLvGain[idx] : 1;
        }
    }

    internal static class CardDBInstance
    {
        static CardDatabaseStatic _inst;
        internal static CardDatabaseStatic Instance =>
            _inst ??= UnityEngine.Resources.Load<CardDatabaseStatic>("CardDatabaseStatic");
    }
}
