// ────────────────────────────────────────────────────────────────────────────────
// PlayerHorseBank.cs
// 玩家全部战马的动态背包（ScriptableObject 存档 + 运行时单例）
// ────────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 战马动态条目（已在 HorseDefinitions.cs 定义）
    /// public class PlayerHorse { string uuid; string staticId; int level; … }
    /// </summary>

    /// <summary>
    /// 玩家战马背包：增删改查 + JSON 存档
    /// </summary>
    [CreateAssetMenu(menuName = "GameDB/PlayerHorseBank")]
    public class PlayerHorseBank : ScriptableObject
    {
        /*───────── 序列化字段 ────────*/
        [SerializeField] private List<PlayerHorse> horses = new();

        /*───────── 运行时字典 ────────*/
        private Dictionary<string, PlayerHorse> uuid2Horse;

        /*───────── 单例访问 ────────*/
        public static PlayerHorseBank I { get; private set; }

        /*───────── 事件 ────────*/
        public event Action<string> onHorseChanged;   // 新增 / 删除
        public event Action<string> onHorseUpdated;   // 任何字段变更

        /*───────── 存档文件名 ────────*/
        private const string SaveFile = "player_horsebank.json";

        /*──────────────── 生命周期 ────────────────*/
        void OnEnable()
        {
            I = this;
            BuildLookup();
        }

        /*──────────────── 私有辅助 ────────────────*/
        void BuildLookup()
        {
            uuid2Horse = new Dictionary<string, PlayerHorse>(horses.Count);
            foreach (var h in horses)
            {
                h.EnsureUuid();
                uuid2Horse[h.uuid] = h;
            }
        }

        /*──────────────── 公开 API ────────────────*/

        /// <summary>通过 uuid 获取战马；不存在返回 null</summary>
        public PlayerHorse Get(string uuid) =>
            uuid2Horse != null && uuid2Horse.TryGetValue(uuid, out var h) ? h : null;

        /// <summary>全部战马（只读枚举）</summary>
        public IEnumerable<PlayerHorse> All => uuid2Horse.Values;

        /// <summary>添加战马（uuid 不重复时生效）</summary>
        public void Add(PlayerHorse ph)
        {
            ph.EnsureUuid();
            if (uuid2Horse.ContainsKey(ph.uuid)) return;

            horses.Add(ph);
            uuid2Horse[ph.uuid] = ph;

            onHorseChanged?.Invoke(ph.uuid);
            Save();
        }

        /// <summary>删除战马</summary>
        public void Remove(string uuid)
        {
            if (!uuid2Horse.TryGetValue(uuid, out var ph)) return;

            horses.Remove(ph);
            uuid2Horse.Remove(uuid);

            onHorseChanged?.Invoke(uuid);
            Save();
        }

        /// <summary>数据变动后调用，触发 UI 刷新并存档</summary>
        public void MarkDirty(string uuid)
        {
            onHorseUpdated?.Invoke(uuid);
            Save();
        }

        /*──────────────── 存档 (JSON) ────────────────*/
        public void Save()
        {
            string json = JsonUtility.ToJson(this);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveFile), json);
        }

        public void Load()
        {
            string path = Path.Combine(Application.persistentDataPath, SaveFile);
            if (!File.Exists(path)) return;

            JsonUtility.FromJsonOverwrite(File.ReadAllText(path), this);
            BuildLookup();
        }
    }
}
