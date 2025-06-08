// Assets/Scripts/Game/Data/RuntimeSave/PlayerGearBank.cs
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Data
{
    /// <summary>玩家拥有的装备清单，可本地 / 云端存档</summary>
    [CreateAssetMenu(menuName = "GameDB/PlayerGearBank")]
    public class PlayerGearBank : ScriptableObject
    {
        [SerializeField] private List<PlayerGear> gears = new();

        public IEnumerable<PlayerGear> All => gears;

        /*──────── 增删改查 ────────*/
        public PlayerGear Get(string staticId) => gears.Find(g => g.staticId == staticId);

        public void Add(string staticId)
        {
            if (Get(staticId) != null) return;
            gears.Add(new PlayerGear { staticId = staticId, level = 1, unlocked = true });
        }

        /*──────── 存档 I/O ────────*/
        public void Save()
        {
            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "player_gears.json"), json);
        }

        public void Load()
        {
            string path = Path.Combine(Application.persistentDataPath, "player_gears.json");
            if (!File.Exists(path)) return;
            JsonUtility.FromJsonOverwrite(File.ReadAllText(path), this);
        }
    }
}
