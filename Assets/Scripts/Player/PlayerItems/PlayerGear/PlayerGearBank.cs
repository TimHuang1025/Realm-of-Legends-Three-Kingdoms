// Assets/Scripts/Game/Data/RuntimeSave/PlayerGearBank.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Game.Data
{
    /// <summary>玩家拥有的装备清单，可本地 / 云端存档</summary>
    [CreateAssetMenu(menuName = "GameDB/PlayerGearBank")]
    public class PlayerGearBank : ScriptableObject
    {
        /*──────────────── 序列化字段 ─────────────────*/
        [SerializeField] private List<PlayerGear> gears = new();

        /*──────────────── 运行时字典 ─────────────────*/
        private Dictionary<string, PlayerGear> uuid2Gear;   // uuid → Gear

        /*─────────────── 生命周期 ───────────────*/
        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            uuid2Gear = new Dictionary<string, PlayerGear>();

            foreach (var g in gears)
            {
                g.EnsureUuid();            // 兼容旧存档，自动补 uuid
                uuid2Gear[g.uuid] = g;     // 以 uuid 为唯一键
            }
        }

        /*──────────────── 公共访问器 ─────────────────*/
        /// <summary>遍历所有装备（按 uuid 索引）</summary>
        public IEnumerable<PlayerGear> All =>
            uuid2Gear != null ? (IEnumerable<PlayerGear>)uuid2Gear.Values
                            : gears;

        /// <summary>按 uuid 精确查找一件装备</summary>
        public PlayerGear Get(string uuid) =>
            uuid2Gear != null && uuid2Gear.TryGetValue(uuid, out var g) ? g : null;

        /// <summary>过渡接口：仍按 staticId 查（若旧代码尚未迁移）</summary>
        public PlayerGear GetByStatic(string staticId) =>
            All.FirstOrDefault(g => g.staticId == staticId);

        /*──────────────── 增删改 ─────────────────*/
        /// <summary>新增一件装备（同种类型可有多件）</summary>
        public PlayerGear Add(string staticId, int level = 1, bool unlocked = true)
        {
            var g = new PlayerGear { staticId = staticId, level = level, unlocked = unlocked };
            g.EnsureUuid();

            gears.Add(g);
            uuid2Gear[g.uuid] = g;

            MarkDirty(g.uuid);
            return g;
        }

        /*──────────────── 存档 I/O ─────────────────*/
        private const string SaveFile = "player_gears.json";

        public void Save()
        {
            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveFile), json);
            ClearDirty();                       // 已写盘 → 清脏标
        }

        public void Load()
        {
            string path = Path.Combine(Application.persistentDataPath, SaveFile);
            if (!File.Exists(path)) return;

            JsonUtility.FromJsonOverwrite(File.ReadAllText(path), this);
            BuildLookup();                      // 重新填充 uuid2Gear
        }

        /*──────────────── Dirty 集 ─────────────────*/
        private readonly HashSet<string> dirtySet = new();

        public void MarkDirty(string uuid)
        {
            if (!string.IsNullOrEmpty(uuid))
                dirtySet.Add(uuid);
        }

        public IEnumerable<string> GetDirtyIds() => dirtySet;
        public void ClearDirty()                => dirtySet.Clear();
    }
}
