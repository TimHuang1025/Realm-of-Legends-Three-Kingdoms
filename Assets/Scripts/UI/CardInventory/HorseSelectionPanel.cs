// Assets/Scripts/Game/UI/HorseSelectionPanel.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Core;                      // EquipmentManager, EquipSlotType
using Game.Data;                     // HorseDatabaseStatic, PlayerHorseBank
using Kamgam.UIToolkitScrollViewPro;
using UnityEngine;
using UIExt;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HorseSelectionPanel : MonoBehaviour
{
    /*──────── ① Inspector ────────*/
    [Header("模板 & 数据源")]
    [SerializeField] private VisualTreeAsset    horseItemTpl;        // ← 用原 Gear 模板
    [SerializeField] private PlayerHorseBank    playerHorseBank;     // ★ 动态背包
    [SerializeField] private HorseDatabaseStatic horseDB;            // ★ 静态库
    [SerializeField] private CardDatabaseStatic cardDatabase;

    [Header("ScrollViewPro 名称")]
    [SerializeField] private string optionContainerName = "ItemList";

    [Header("网格布局 (可选)")]
    [SerializeField] private int   itemsPerRow = 2;
    [SerializeField] private float cellSize    = 240;
    [SerializeField] private float colGap      = 8f;
    [SerializeField] private float rowGap      = 12f;

    /*──────── ② 运行时字段 ────────*/
    private UIDocument doc;
    private ScrollViewPro optionContainer;
    private Button closeBtn, equipBtn, sortBtn;
    private Button lastShownEquipBtn;

    private PlayerCard currentCard;           // 当前武将
    private string     selectedHorseUuid;     // 选中的战马

    readonly string[] orderModes = { "品阶↓", "稀有度↓" };
    int orderIdx = 0;

    class Entry { public VisualElement root; public VisualElement mask; }
    private readonly List<Entry> entries = new();

    /*──────── ③ 生命周期 ────────*/
    void Awake()
    {
        doc = GetComponent<UIDocument>();
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        var root = doc.rootVisualElement;

        optionContainer = root.Q<ScrollViewPro>(optionContainerName);
        closeBtn = root.Q<Button>("ClosePanel");
        equipBtn = root.Q<Button>("EquipBtn");
        sortBtn  = root.Q<Button>("ItemOrderSort");

        optionContainer.mode = ScrollViewMode.Vertical;
        optionContainer.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        optionContainer.verticalScrollerVisibility   = ScrollerVisibility.Hidden;

        closeBtn?.RegisterCallback<ClickEvent>(_ => Close());
        root.Q<VisualElement>("BlackSpace")?.RegisterCallback<ClickEvent>(_ => Close());

        if (sortBtn != null)
        {
            sortBtn.text = orderModes[orderIdx];
            sortBtn.clicked += () =>
            {
                orderIdx = (orderIdx + 1) % orderModes.Length;
                sortBtn.text = orderModes[orderIdx];
                BuildOptions();
            };
        }

        equipBtn?.RegisterCallback<ClickEvent>(_ => OnEquipClicked());
    }

    /*──────── ④ 对外接口 ────────*/
    public void Open(PlayerCard card)
    {
        currentCard        = card;
        selectedHorseUuid  = string.Empty;

        gameObject.SetActive(true);
        StartCoroutine(AfterEnableRoutine());
    }

    public void Close() => gameObject.SetActive(false);

    IEnumerator AfterEnableRoutine()
    {
        yield return null;
        yield return null;          // 等布局
        BuildOptions();
    }

    /*──────── ⑤ 生成网格 ────────*/
    void BuildOptions()
    {
        if (horseItemTpl == null || optionContainer == null || playerHorseBank == null) return;

        optionContainer.Clear();
        entries.Clear();

        var list = playerHorseBank.All
                  .Select(ph => (ph, st: horseDB.Get(ph.staticId)))
                  .Where(t => t.st != null)
                  .ToList();

        list = orderIdx == 1
             ? list.OrderBy(t => t.st.tier).ToList()
             : list.OrderBy(t => (int)t.st.tier).ToList();

        if (list.Count == 0)
        {
            optionContainer.Add(new Label("（暂无战马）") { style = { unityTextAlign = TextAnchor.MiddleCenter, height = 60, color = Color.gray } });
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
                var ph = list[idx].ph;
                var st = list[idx].st;

                var ve = horseItemTpl.Instantiate();
                ve.name = $"Horse_{ph.uuid}";
                ve.style.width  = cellSize;
                ve.style.height = 170;
                if (c > 0) ve.style.marginLeft = colGap;

                /* icon 与文本 --------------------------------------------------*/
                var icon = ve.Q<VisualElement>("GearIcon");
                if (icon != null && st.iconSprite != null)
                {
                    icon.style.backgroundImage = new StyleBackground(st.iconSprite);
                    icon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                }
                var (atk, def, intel) = st.CalcStats(ph.level);

                ve.Q<Label>("GearName")   ?.SetTextSafe(st.name);
                ve.Q<Label>("GearLv")     ?.SetTextSafe($"{ph.level}");
                ve.Q<Label>("GearAtkStat")?.SetTextSafe($"攻击 +{atk:P3}");
                ve.Q<Label>("GearDefStat")?.SetTextSafe($"防御 +{def:P3}");

                // ★ 显示智力
                var intLbl = ve.Q<Label>("GearIntStat");
                if (intLbl != null)
                {
                    intLbl.text = $"智力 +{intel:P3}";
                    intLbl.style.display = DisplayStyle.Flex;
                }

                /* 已装备角标 ----------------------------------------------------*/
                var equipFlag = ve.Q<VisualElement>("IfEquiped");
                if (!string.IsNullOrEmpty(ph.equippedById))
                {
                    var heroStatic = cardDatabase.Get(ph.equippedById);
                    if (heroStatic?.iconSprite != null)
                    {
                        equipFlag.style.display = DisplayStyle.Flex;
                        equipFlag.style.backgroundImage = new StyleBackground(heroStatic.iconSprite);
                        equipFlag.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                    }
                }

                /* DimMask & 按钮 ----------------------------------------------*/
                var mask = ve.Q<VisualElement>("DimMask");
                bool equippedByOther =
                    !string.IsNullOrEmpty(ph.equippedById) && ph.equippedById != currentCard.id;
                if (mask != null) mask.style.display = equippedByOther ? DisplayStyle.Flex : DisplayStyle.None;

                entries.Add(new Entry { root = ve, mask = mask });

                var rowEquipBtn = ve.Q<Button>("EquipBtn");
                rowEquipBtn.style.display = DisplayStyle.None;
                rowEquipBtn.clicked += () =>
                {
                    selectedHorseUuid = ph.uuid;
                    OnEquipClicked();
                };

                /* 点击选中 -------------------------------------------------------*/
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
                    selectedHorseUuid = ph.uuid;
                    Highlight(ve);          // 让它只加选中样式；别再管 mask
                });

                row.Add(ve);
            }
            optionContainer.Add(row);
        }
        optionContainer.RefreshAfterHierarchyChange();
    }

    /*──────── 自动列数 ────────*/
    int CalcAutoColumns()
    {
        float w = optionContainer.resolvedStyle.width;
        if (w < 1f) w = optionContainer.worldBound.width;
        if (w < 1f) return 1;
        return Mathf.Max(1, Mathf.FloorToInt((w + colGap) / (cellSize + colGap)));
    }

    /*──────── 装备按钮 ────────*/
    void OnEquipClicked()
    {
        if (string.IsNullOrEmpty(selectedHorseUuid)) return;

        var pHorse = playerHorseBank.Get(selectedHorseUuid);
        if (pHorse == null) { PopupManager.Show("提示", "找不到战马数据"); return; }

        EquipmentManager.Equip(currentCard, pHorse, EquipSlotType.Mount,
                               PlayerCardBankMgr.I.Data, playerHorseBank);

        BuildOptions();
        Close();
    }

    /*──────── 高亮 ────────*/
    void Highlight(VisualElement selected)
    {
        foreach (var e in entries)
        {
            if (e.root == selected) e.root.AddToClassList("selected");
            else                    e.root.RemoveFromClassList("selected");
        }
    }
}

/*──────── 扩展辅助 ────────*/

