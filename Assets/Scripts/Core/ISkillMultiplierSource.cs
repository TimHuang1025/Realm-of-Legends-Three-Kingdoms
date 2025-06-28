// ────────────────────────────────────────────────────────────────────────────────
// Assets/Scripts/Game/Core/ISkillMultiplierProvider.cs
// 技能倍率统一读取接口
// ────────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;

// Assets/Scripts/Game/Core/ISkillMultiplierSource.cs
namespace Game.Core
{
    public interface ISkillMultiplierSource
    {
        /// <summary>品阶倍率表 &lt;S/A/B… → 倍率&gt;</summary>
        IReadOnlyDictionary<Tier, float> TierMultiplier  { get; }

        /// <summary>等级倍率表 &lt;Lv(1-4) → 倍率&gt;</summary>
        IReadOnlyDictionary<int,  float> LevelMultiplier { get; }
    }
}
