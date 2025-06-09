// Assets/Scripts/Game/UI/GearSelectionPanel.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Core;                      // EquipmentManager, EquipSlotType
using Game.Data;                     // GearDatabaseStatic, PlayerGearBank
using Kamgam.UIToolkitScrollViewPro; // ScrollViewPro
using UnityEngine;
using UIExt;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GearSelectionPanel : MonoBehaviour
{
    /*───────── ① Inspector ─────────*/
    [Header("模板 & 数据源")]
    [SerializeField] private VisualTreeAsset    gearItemTpl;     // 单条装备模板
    [SerializeField] private PlayerGearBank     playerGearBank;  // 玩家背包
    [SerializeField] private GearDatabaseStatic gearDB;          // 可选：直接拖 GearStaticDB
    [SerializeField] private CardDatabaseStatic cardDatabase;    // 武将静态表，用于取头像

    [Header("ScrollViewPro 名称")]
    [SerializeField] private string optionContainerName = "ItemList";

    [Header("网格布局 (可选)")]
    [SerializeField] private int   itemsPerRow = 2;   // <=1 时退回单列列表
    [SerializeField] private float gearSize    = 240; // 单张宽高
    [SerializeField] private float colGap      = 8f;  // 列间距
    [SerializeField] private float rowGap      = 12f; // 行间距

    /*───────── ② 运行时字段 ─────────*/
    private VisualElement currentItem;
    private Button lastShownEquipBtn;
    private UIDocument doc;
    private ScrollViewPro optionContainer;
    private Button        closeBtn, cancelBtn, equipBtn, sortBtn;

    private EquipSlotType currentSlot;          // Weapon / Armor
    private PlayerCard    currentCard;          // 选中武将
    private string        selectedGearUuid;     // 当前选择的装备 uuid

    private readonly string[] orderModes = { "品阶↓", "稀有度↓" };
    private int orderIdx = 0;

    /* ★ 保存条目信息（根节点 + DimMask） */
    class Entry { public VisualElement root; public VisualElement mask; }
    private readonly List<Entry> entries = new();

    /*───────── ③ 生命周期 ─────────*/
    private void Awake()
    {
        doc = GetComponent<UIDocument>();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        var root = doc.rootVisualElement;

        optionContainer = root.Q<ScrollViewPro>(optionContainerName);
        closeBtn   = root.Q<Button>("ClosePanel");
        cancelBtn  = root.Q<Button>("CloseBtn2");
        equipBtn   = root.Q<Button>("EquipBtn");
        sortBtn    = root.Q<Button>("ItemOrderSort");

        optionContainer.mode = ScrollViewMode.Vertical;
        optionContainer.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        optionContainer.verticalScrollerVisibility   = ScrollerVisibility.Hidden;

        closeBtn?.RegisterCallback<ClickEvent>(_ => Close());
        cancelBtn?.RegisterCallback<ClickEvent>(_ => Close());

        if (sortBtn != null)
        {
            sortBtn.text = orderModes[orderIdx];
            sortBtn.clicked += () =>
            {
                orderIdx = (orderIdx + 1) % orderModes.Length;
                sortBtn.text = orderModes[orderIdx];
                BuildGearOptions();
            };
        }

        equipBtn?.RegisterCallback<ClickEvent>(_ => OnEquipClicked());
    }

    /*───────── ④ 对外接口 ─────────*/
    public void Open(PlayerCard card, EquipSlotType slot)
    {
        if (slot == EquipSlotType.Mount)
        {
            Debug.LogWarning("当前版本未实现坐骑槽，忽略打开请求");
            return;
        }

        currentCard      = card;
        currentSlot      = slot;
        selectedGearUuid = string.Empty;

        gameObject.SetActive(true);
        StartCoroutine(AfterEnableRoutine());
    }

    public void Close() => gameObject.SetActive(false);

    private IEnumerator AfterEnableRoutine()
    {
        yield return null;
        yield return null;   // 第 2 帧：布局完成
        BuildGearOptions();
    }

    /*───────── ⑤ 生成网格 ─────────*/
    private void BuildGearOptions()
    {
        if (gearItemTpl == null || optionContainer == null || playerGearBank == null) return;

        optionContainer.Clear();
        entries.Clear();

        var db = gearDB != null ? gearDB : GearDatabaseStatic.Instance;
        if (db == null) { Debug.LogError("[GearPanel] GearStaticDB 缺失！"); return; }

        var list = playerGearBank.All
                  .Select(pg => (pg, st: db.Get(pg.staticId)))
                  .Where(t => t.st != null &&
                              (currentSlot == EquipSlotType.Weapon
                                 ? t.st.kind == GearKind.Weapon
                                 : t.st.kind == GearKind.Armor))
                  .ToList();

        list = orderIdx == 1
             ? list.OrderBy(t => t.st.tier).ToList()
             : list.OrderBy(t => (int)t.st.tier).ToList();

        if (list.Count == 0)
        {
            optionContainer.Add(new Label("（暂无符合条件的装备）") { style = { unityTextAlign = TextAnchor.MiddleCenter,height = 60,color = Color.gray } });
            optionContainer.RefreshAfterHierarchyChange();
            return;
        }

        int cols = itemsPerRow > 0 ? itemsPerRow : CalcAutoColumns();
        cols = Mathf.Max(1, cols);

        int idx = 0;
        while (idx < list.Count)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = rowGap, flexShrink = 0 } };

            for (int c = 0; c < cols && idx < list.Count; c++, idx++)
            {
                var pg = list[idx].pg;
                var st = list[idx].st;

                var ve = gearItemTpl.Instantiate();
                ve.name = $"Gear_{pg.uuid}";
                ve.style.width  = gearSize;
                ve.style.height = 140;
                if (c > 0) ve.style.marginLeft = colGap;

                /* icon 与文本 */
                var icon = ve.Q<VisualElement>("GearIcon");
                if (icon != null && st.iconSprite != null)
                {
                    icon.style.backgroundImage = new StyleBackground(st.iconSprite);
                    icon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                }
                var (atk, def) = st.CalcStats(pg.level);
                ve.Q<Label>("GearName")   ?.SetTextSafe($"{st.name}");
                ve.Q<Label>("GearLv")     ?.SetTextSafe($"{pg.level}");
                ve.Q<Label>("GearAtkStat")?.SetTextSafe($"攻击 +{atk:N0}");
                ve.Q<Label>("GearDefStat")?.SetTextSafe($"防御 +{def:N0}");

                /* 已装备角标 */
                var equipFlag = ve.Q<VisualElement>("IfEquiped");
                if (!string.IsNullOrEmpty(pg.equippedById))
                {
                    var heroStatic = cardDatabase.Get(pg.equippedById);
                    if (heroStatic?.iconSprite != null)
                    {
                        equipFlag.style.display = DisplayStyle.Flex;
                        equipFlag.style.backgroundImage = new StyleBackground(heroStatic.iconSprite);
                        equipFlag.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                    }
                }

                /* 记录 DimMask */
                var mask = ve.Q<VisualElement>("DimMask");
                if (mask == null) Debug.LogError("模板缺少 DimMask");

                bool equippedByOther =
                    !string.IsNullOrEmpty(pg.equippedById) && pg.equippedById != currentCard.id;
                mask.style.display = equippedByOther ? DisplayStyle.Flex : DisplayStyle.None;

                entries.Add(new Entry { root = ve, mask = mask });
                var rowEquipBtn = ve.Q<Button>("EquipBtn");
                rowEquipBtn.style.display = DisplayStyle.None;      // 默认隐藏
                rowEquipBtn.clicked += () =>
                {
                    selectedGearUuid = pg.uuid;   // 先记录
                    OnEquipClicked();             
                };

                /* 点击选中 —— 两行保持原写法 */
                var clickTarget = (VisualElement)(ve.Q<Button>("GearOption") 
                                      ?? (VisualElement)ve);
                clickTarget.Bounce(ve, 0.9f, 0.08f);
                clickTarget.RegisterCallback<ClickEvent>(_ =>
                {
                    // 隐旧按钮
                    if (lastShownEquipBtn != null)
                        lastShownEquipBtn.style.display = DisplayStyle.None;

                    // 显新按钮
                    rowEquipBtn.style.display = DisplayStyle.Flex;
                    lastShownEquipBtn = rowEquipBtn;

                    // 选中逻辑（高亮类名可留）
                    selectedGearUuid = pg.uuid;
                    Highlight(ve);          // 让它只加选中样式；别再管 mask
                });

                row.Add(ve);
            }
            optionContainer.Add(row);
        }
        optionContainer.RefreshAfterHierarchyChange();
    }

    /*──────── 自动列数 ────────*/
    private int CalcAutoColumns()
    {
        float w = optionContainer.resolvedStyle.width;
        if (w < 1f) w = optionContainer.worldBound.width;
        if (w < 1f) return 1;
        return Mathf.Max(1, Mathf.FloorToInt((w + colGap) / (gearSize + colGap)));
    }

    /*──────── 装备按钮 ────────*/
    private void OnEquipClicked()
    {
        if (string.IsNullOrEmpty(selectedGearUuid)) return;

        var pGear = playerGearBank.Get(selectedGearUuid);
        if (pGear == null) { PopupManager.Show("提示", "找不到装备数据"); return; }

        EquipmentManager.Equip(currentCard, pGear, currentSlot,
                               PlayerCardBankMgr.I.Data, playerGearBank);

        BuildGearOptions();
        Close();
    }

    /*──────── 高亮条目 + DimMask ────────*/
    private void Highlight(VisualElement selected)
    {
        foreach (var e in entries)
        {
            bool sel = e.root == selected;

            // ★ 只处理选中样式，不再隐藏 / 显示 DimMask
            if (sel)   e.root.AddToClassList("selected");
            else       e.root.RemoveFromClassList("selected");
        }
    }

}

/*──────── Label 扩展 ────────*/
static class UILabelGearExt
{
    public static void SetTextSafe(this Label lbl, string txt)
    {
        if (lbl != null) lbl.text = txt;
    }
}
