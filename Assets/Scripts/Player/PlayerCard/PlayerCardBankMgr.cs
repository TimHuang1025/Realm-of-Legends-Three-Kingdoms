using System;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PlayerCardBankMgr : MonoBehaviour
{
    /*──────── 单例 ────────*/
    public static PlayerCardBankMgr I { get; private set; }

    /*──────── 数据资产 ───────*/
    [SerializeField] private PlayerCardBank data;         // 拖 PlayerCardBank.asset
    public PlayerCardBank Data => data;

    const string SaveFile = "player_cardbank.json";

    /*──────── 事件 ────────*/
    public event Action<string> onCardChanged;   // 仅卡牌新增 / 删除时触发（沿用旧逻辑）
    public event Action<string> onCardUpdated;   // 任何动态字段变动都触发（新）

    /*──────── 生命周期 ─────*/
    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        Load();
        Debug.Log("Awake PlayerCardBankMgr: " + data.cards.Count + " cards loaded.");
    }

    /*─────────────────────────────────────────────
     * 公开 API
     *────────────────────────────────────────────*/

    /*—— 等级 ——*/
    public void AddLevel(string id, int delta)
    {
        var pc = data.Get(id);
        if (pc == null) return;

        pc.level = Mathf.Clamp(pc.level + delta, 1, 999);
        NotifyUpdated(id);
    }

    /*—— 星级 ——*/
    public void AddStar(string id, int delta)
    {
        var pc = data.Get(id);
        if (pc == null) return;

        pc.star = Mathf.Clamp(pc.star + delta, 0, 5);
        NotifyUpdated(id);
    }

    /*—— 装备 ——*/
    public void EquipWeapon(string id, string weaponId)
    {
        var pc = data.Get(id);
        if (pc == null) return;

        pc.equip.weaponUuid = weaponId;
        pc.equip.weaponUnlocked = !string.IsNullOrEmpty(weaponId);
        NotifyUpdated(id);
    }

    public void EquipArmor(string id, string armorId)
    {
        var pc = data.Get(id);
        if (pc == null) return;

        pc.equip.armorUuid = armorId;
        pc.equip.armorUnlocked = !string.IsNullOrEmpty(armorId);
        NotifyUpdated(id);
    }

    public void EquipMount(string id, string mountId)
    {
        var pc = data.Get(id);
        if (pc == null) return;

        pc.equip.accessoryUuid = mountId;
        pc.equip.mountUnlocked = !string.IsNullOrEmpty(mountId);
        NotifyUpdated(id);
    }

    /*—— 礼物经验 ——*/
    public void AddGiftExp(string id, int exp)
    {
        var pc = data.Get(id);
        if (pc == null || exp <= 0) return;

        const int MaxLv = 4;
        int[] Need = UnitGiftLevel.Need;      // 直接引用静态表

        pc.giftExp += exp;

        while (pc.giftLv < MaxLv && pc.giftExp >= Need[pc.giftLv])
        {
            pc.giftExp -= Need[pc.giftLv];
            pc.giftLv++;
            if (pc.giftLv == 2) pc.equip.weaponUnlocked = true;
            if (pc.giftLv == 3) pc.equip.armorUnlocked = true;
            if (pc.giftLv == 4) pc.equip.mountUnlocked = true;
        }

        NotifyUpdated(id);   // 广播 onCardUpdated + Save()
    }

    /*—— 新抽到卡牌 ——*/
    public void AddNewCard(string id)
    {
        if (data.Get(id) != null) return;          // 已拥有
        data.cards.Add(new PlayerCard { id = id });
        onCardChanged?.Invoke(id);                 // 保持与旧逻辑一致
        Save();
    }

    /*—— 删除卡牌（可能用不到，示例） ——*/
    public void RemoveCard(string id)
    {
        int idx = data.cards.FindIndex(c => c.id == id);
        if (idx < 0) return;

        data.cards.RemoveAt(idx);
        onCardChanged?.Invoke(id);
        Save();
    }

    /*──────────────── 内部辅助 ───────────────*/

    void NotifyUpdated(string id)
    {
        onCardUpdated?.Invoke(id);
        Save();
    }

    /*──────── 存取硬盘 ────────*/
    void Save()
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveFile), json);
    }

    void Load()
    {
        string path = Path.Combine(Application.persistentDataPath, SaveFile);
        if (File.Exists(path))
            JsonUtility.FromJsonOverwrite(File.ReadAllText(path), data);
    }
    public void BroadcastCardUpdated(string id)
    {
        onCardUpdated?.Invoke(id);
    }
    public void RaiseCardUpdated(string id)
    {
        onCardUpdated?.Invoke(id);
    }
    // 加在 PlayerCardBankMgr 类里，位置随意（比如其他 public 方法下方）
    public void MarkDirty(string id)
    {
        NotifyUpdated(id);  // 复用内部私有方法，顺带 Save()
    }

}
