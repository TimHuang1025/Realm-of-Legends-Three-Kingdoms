using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 礼物经验 & 等级显示（ProgressBar 版）
/// Lv 1 → 2 需要 10 000 EXP；Lv 2 → 3 需要 30 000 EXP；Lv 3 = 赐骑（顶级）
/// </summary>
public class UnitGiftLevel : MonoBehaviour
{
    /*──────── 运行时数据 ────────*/
    [HideInInspector] public CardInfo data;   // 点击卡片时由外部脚本赋值

    /*──────── UI 引用 ────────*/
    private Label        levelLabel;
    private ProgressBar  expBar;
    private VisualElement weaponslot;  // 武器槽
    private VisualElement armorslot;   // 盔甲槽       
    private VisualElement horseslot;   // 坐骑槽

    /*──────── 常量表 ────────*/
    public static readonly int[] Need = { 0, 10000, 30000, 50000 };          // 1→2, 2→3
    public static readonly string[] LvNames = { "", "拜将", "授甲", "赐骑", "赐骑" };   // 下标 = giftLv
    private const int MaxLv = 4;

    /*──────── 生命周期 ────────*/
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // 下一帧再抓元素，确保树已 attach 到 Panel
        root.schedule.Execute(() =>
        {
            CacheElements();
            RefreshUI();
        }).ExecuteLater(0);
    }

    private void CacheElements()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        levelLabel = root.Q<Label>("HeroGiftLv");
        expBar = root.Q<ProgressBar>("HeroGiftProgress");
        weaponslot = root.Q<VisualElement>("weaponslot");
        armorslot = root.Q<VisualElement>("armorslot");
        horseslot = root.Q<VisualElement>("horseslot");
    }

    /*──────── 主要接口 ────────*/
    /*──────── AddExp 改进版 ────────*/
    public void AddExp(int amount)
    {
        if (data == null || data.giftLv >= MaxLv) return;

        data.giftExp += amount;

        while (data.giftLv < MaxLv && data.giftExp >= Need[data.giftLv])
        {
            data.giftExp -= Need[data.giftLv];
            data.giftLv++;

            /* 升到每一级都触发一次 */
            PlayUpgradeFX(data.giftLv);
        }

        RefreshUI();
    }

    /*──────── 按等级播放特效 ────────*/
void PlayUpgradeFX(int newLv)
    {
        if (data == null) return;               // 双保险

        switch (newLv)
        {
            case 2:   // 拜将 → 授甲：解锁武器槽
                data.equip.weaponUnlocked = true;          // ① 改数据
                BindEquipSlot(weaponslot, true);           // ② 刷 UI
                break;

            case 3:   // 授甲 → 赐骑：解锁盔甲槽
                data.equip.armorUnlocked  = true;
                BindEquipSlot(armorslot, true);
                break;

            case 4:   // 赐骑 → 封侯：解锁坐骑槽
                data.equip.mountUnlocked  = true;
                BindEquipSlot(horseslot, true);
                break;
        }
    }

    /* 把之前的通用函数放在脚本里，供任何地方复用 */
    public void RefreshEquipSlots()
    {
        if (data == null) return;

        BindEquipSlot(weaponslot, data.equip.weaponUnlocked);
        BindEquipSlot(armorslot, data.equip.armorUnlocked);
        BindEquipSlot(horseslot, data.equip.mountUnlocked);
    }
// 复用同名工具
    void BindEquipSlot(VisualElement slot, bool unlocked)
    {
        if (slot == null) return;
        slot.RemoveFromClassList("equipmentlocked");
        slot.RemoveFromClassList("equipmentunlocked");
        slot.AddToClassList(unlocked ? "equipmentunlocked" : "equipmentlocked");
    }

    public void RefreshUI()
    {
        if (data == null || levelLabel == null || expBar == null)
            return;

        /*── 1. 等级文字 ──*/
        levelLabel.text = $"封赏等级：{LvNames[data.giftLv]}";
        levelLabel.MarkDirtyRepaint();

        /*── 2. 经验条 ──*/
        float pct = data.giftLv >= MaxLv
                  ? 1f
                  : (float)data.giftExp / Need[data.giftLv];

        expBar.lowValue  = 0;
        expBar.highValue = 100;
        expBar.value     = pct * 100f;
        expBar.title     = data.giftLv >= MaxLv ? "MAX"
                           : $"{(int)(pct * 100)}%";
        expBar.MarkDirtyRepaint();
    }

    /*──────── 提供给其他脚本调用的只读接口 ────────*/
    public string GetLvText()     => data == null ? "封赏等级：" : $"封赏等级：{LvNames[data.giftLv]}";
    public float  GetExpPercent() => data == null ? 0 :
                                     data.giftLv >= MaxLv ? 100f :
                                     100f * data.giftExp / Need[data.giftLv];
}
