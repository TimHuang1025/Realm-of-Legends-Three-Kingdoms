using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 统一管理玩家资源（单例 + JSON 存档）
/// </summary>
public class PlayerBank : MonoBehaviour
{
    public static PlayerBank I { get; private set; }

    [SerializeField] private PlayerResources data;   // Inspector 拖 ScriptableObject
    private const string SaveFile = "player_bank.json";

    /* ───── 生命周期 ───── */
    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        Load();                    // 读取本地存档
    }

    /* ───── 查询接口 ───── */
    public int this[ResourceType type] => type switch
    {
        ResourceType.Gold        => data.gold,
        ResourceType.Silver      => data.silver,
        ResourceType.Copper      => data.copper,
        ResourceType.HeroExp     => data.heroexp,
        ResourceType.HeroMat2    => data.heromat2,
        ResourceType.GachaTicket => data.gachaTicket,
        _                        => 0
    };

    /* ───── 修改接口 ───── */
    public void Add(ResourceType type, int amount)
    {
        amount = Mathf.Max(amount, 0);
        Modify(type, +amount);
    }

    /// <summary>扣除资源；余额不足时返回 false</summary>
    public bool Spend(ResourceType type, int amount)
    {
        if (this[type] < amount) return false;
        Modify(type, -amount);
        return true;
    }

    private void Modify(ResourceType type, int delta)
    {
        switch (type)
        {
            case ResourceType.Gold:        data.gold        += delta; break;
            case ResourceType.Silver:      data.silver      += delta; break;
            case ResourceType.Copper:      data.copper      += delta; break;
            case ResourceType.HeroExp:     data.heroexp     += delta; break;
            case ResourceType.HeroMat2:    data.heromat2    += delta; break;
            case ResourceType.GachaTicket: data.gachaTicket += delta; break;
        }

        onBankChanged?.Invoke(type);
        Save();
    }

    /* ───── 事件 ───── */
    public Action<ResourceType> onBankChanged;   // UI 可订阅此事件刷新显示

    /* ───── 存档 (JSON) ───── */
    [Serializable] class SaveStruct
    {
        public int gold;
        public int silver;
        public int copper;
        public int heroexp;
        public int heromat2;
        public int gachaTicket;
    }

    private void Save()
    {
        var s = new SaveStruct
        {
            gold        = data.gold,
            silver      = data.silver,
            copper      = data.copper,
            heroexp     = data.heroexp,
            heromat2    = data.heromat2,
            gachaTicket = data.gachaTicket
        };

        string path = Path.Combine(Application.persistentDataPath, SaveFile);
        File.WriteAllText(path, JsonUtility.ToJson(s));
    }

    private void Load()
    {
        string path = Path.Combine(Application.persistentDataPath, SaveFile);
        if (!File.Exists(path)) return;

        var s = JsonUtility.FromJson<SaveStruct>(File.ReadAllText(path));
        data.gold        = s.gold;
        data.silver      = s.silver;
        data.copper      = s.copper;
        data.heroexp     = s.heroexp;
        data.heromat2    = s.heromat2;
        data.gachaTicket = s.gachaTicket;
    }
}
