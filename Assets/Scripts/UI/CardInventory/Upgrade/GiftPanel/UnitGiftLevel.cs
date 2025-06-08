using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 玩家礼物经验 & 等级显示（ProgressBar 版）
/// giftLv: 1→2 需要 10 000 EXP，2→3 需要 30 000 EXP，3→4 需要 50 000 EXP
/// 静态信息来自 CardInfoStatic，动态进度来自 PlayerCard
/// </summary>
public class UnitGiftLevel : MonoBehaviour
{
    /*──────── 传入数据 ────────*/
    private CardInfoStatic info;  // 静态：名字、品质等
    private PlayerCard     dyn;   // 动态：giftLv / giftExp / equip

    public void SetData(CardInfoStatic staticInfo, PlayerCard dynamicInfo)
    {
        info = staticInfo;
        dyn  = dynamicInfo;
    }

    /*──────── UI 元素 ────────*/
    Label         levelLabel;
    ProgressBar   expBar;
    VisualElement weaponslot, armorslot, horseslot;

    /*──────── 常量表 ────────*/
    public static readonly int[] Need    = { 0, 10000, 30000, 50000 };    // idx = 当前 giftLv
    public static readonly string[] LvTxt= { "", "拜将", "授甲", "赐骑", "封侯" };
    const int MaxLv = 4;

    /*──────── 生命周期 ────────*/
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        levelLabel = root.Q<Label>("HeroGiftLv");
        expBar = root.Q<ProgressBar>("HeroGiftProgress");
        weaponslot = root.Q<Button>("weaponslot");
        armorslot = root.Q<Button>("armorslot");
        horseslot = root.Q<Button>("horseslot");
    }



    /*──────── 对外接口 ────────*/
    public void AddExp(int amount)
    {
        if (dyn == null || amount <= 0) return;

        // ① 记录升级前的等级
        int oldLv = dyn.giftLv;

        // ② 交给 BankMgr 统一加经验（会自动升级、存档、广播）
        PlayerCardBankMgr.I.AddGiftExp(dyn.id, amount);

        // ③ 取回最新动态数据，确保本地同步
        dyn = PlayerCardBankMgr.I.Data.Get(dyn.id);

        // ④ 前后等级差 > 0 时，依次播放 FX
        for (int lv = oldLv + 1; lv <= dyn.giftLv; lv++)
            PlayUpgradeFX(lv);

        // ⑤ 刷新本面板 UI（经验条、槽位）
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (info == null || levelLabel == null || expBar == null) return;

        int lv   = dyn?.giftLv  ?? 1;
        int exp  = dyn?.giftExp ?? 0;

        /* 等级文字 */
        levelLabel.text = $"封赏等级：{LvTxt[lv]}";
        levelLabel.MarkDirtyRepaint();

        /* 经验条 */
        float pct = lv >= MaxLv ? 1f : (float)exp / Need[lv];
        expBar.lowValue  = 0;
        expBar.highValue = 100;
        expBar.value     = pct * 100f;
        expBar.title     = lv >= MaxLv ? "MAX" : $"{(int)(pct * 100)}%";
        expBar.MarkDirtyRepaint();

        RefreshEquipSlots();
    }

    /*──────── 装备槽刷新 ────────*/
    public void RefreshEquipSlots()
    {
        if (dyn == null) return;
        var eq = dyn.equip;

        bool w = eq != null && (eq.weaponUnlocked  || !string.IsNullOrEmpty(eq.weaponId));
        bool a = eq != null && (eq.armorUnlocked   || !string.IsNullOrEmpty(eq.armorId));
        bool h = eq != null && (eq.mountUnlocked   || !string.IsNullOrEmpty(eq.accessoryId));

        BindEquipSlot(weaponslot, w);
        BindEquipSlot(armorslot,  a);
        BindEquipSlot(horseslot,  h);
    }

    static void BindEquipSlot(VisualElement slot, bool unlocked)
    {
        if (slot == null) return;
        slot.RemoveFromClassList("equipmentlocked");
        slot.RemoveFromClassList("equipmentunlocked");
        slot.AddToClassList(unlocked ? "equipmentunlocked" : "equipmentlocked");
    }

    /*──────── 晋级解锁 FX + 自动刷新 ────────*/
    void PlayUpgradeFX(int newLv)
    {
        if (dyn == null) return;

        switch (newLv)
        {
            case 2:
                PopupManager.Show("恭喜！", "完成拜将仪式，解锁武器槽！");
                dyn.equip.weaponUnlocked = true;    // 标记已解锁
                break;
            case 3:
                PopupManager.Show("恭喜！", "完成授甲仪式，解锁盔甲槽！");
                dyn.equip.armorUnlocked = true;
                break;
            case 4:
                PopupManager.Show("恭喜！", "完成赐骑仪式，解锁坐骑槽！");
                dyn.equip.mountUnlocked = true;
                break;
        }

        // 升阶弹窗后立即刷新 3 个槽位视觉
        RefreshEquipSlots();
    }

    /*──────── 只读辅助接口 ────────*/
    public string GetLvText()     => dyn == null ? "封赏等级：" : $"封赏等级：{LvTxt[dyn.giftLv]}";
    public float GetExpPercent()
    {
        if (dyn == null)               return 0f;    // 未拥有
        if (dyn.giftLv >= MaxLv)       return 100f;  // 满级

        int need = Need[dyn.giftLv];

        // 防止分母为 0 或负；同时把异常值钳到 0-100
        if (need <= 0) return 0f;

        float pct = 100f * dyn.giftExp / need;
        return Mathf.Clamp(pct, 0f, 100f);
    }
}
