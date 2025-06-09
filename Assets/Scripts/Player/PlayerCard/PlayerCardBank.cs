using System.IO;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Player/Card Bank")]
public class PlayerCardBank : ScriptableObject
{
    public List<PlayerCard> cards = new();

    /*──────── 运行期查询表 ────────*/
    Dictionary<string, PlayerCard> lookup;

    void OnEnable()
    {
        BuildLookup();
    }

    void BuildLookup()
    {
        lookup = new Dictionary<string, PlayerCard>();
        foreach (var c in cards)
            if (!string.IsNullOrEmpty(c.id))
                lookup[c.id] = c;
    }

    public PlayerCard Get(string id) =>
        lookup != null && lookup.TryGetValue(id, out var c) ? c : null;

    /*──────── 打脏集合 ────────*/
    readonly HashSet<string> dirtySet = new();
    public void MarkDirty(string id)
    {
        if (!string.IsNullOrEmpty(id))
            dirtySet.Add(id);
    }
    public IEnumerable<string> GetDirtyIds() => dirtySet;
    public void ClearDirty() => dirtySet.Clear();

    /*──────── 持久化接口 ────────*/
        private const string SaveFile = "player_cardbank.json";

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
}
