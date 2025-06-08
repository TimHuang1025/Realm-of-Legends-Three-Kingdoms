// Assets/Scripts/Game/UI/GearSelectionPanel.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Core;                      // EquipSystem, EquipSlotType
using Game.Data;                      // GearDatabaseStatic, PlayerGearBank
using Kamgam.UIToolkitScrollViewPro;  // ScrollViewPro
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GearSelectionPanel : MonoBehaviour
{
    /*───────── ① Inspector ─────────*/
    [Header("模板 & 数据源")]
    [SerializeField] private VisualTreeAsset    gearItemTpl;     // 单条装备模板
    [SerializeField] private PlayerGearBank     playerGearBank;  // 玩家背包
    [SerializeField] private GearDatabaseStatic gearDB;          // 可选：直接拖 GearStaticDB

    [Header("ScrollViewPro 名称")]
    [SerializeField] private string optionContainerName = "ItemList";

    [Header("网格布局 (可选)")]
    [SerializeField] private int   itemsPerRow = 2;   // <=1 时退回单列列表
    [SerializeField] private float gearSize    = 240; // 单张宽高
    [SerializeField] private float colGap      = 8f;  // 列间距
    [SerializeField] private float rowGap      = 12f; // 行间距

    /*───────── ② 运行时字段 ─────────*/
    private UIDocument     doc;
    private ScrollViewPro  optionContainer;
    private Button         closeBtn, cancelBtn, equipBtn, sortBtn;

    private EquipSlotType  currentSlot;     // Weapon / Armor
    private PlayerCard     currentCard;     // 选中武将
    private string         selectedGearId;  // 当前选择的装备 id

    private readonly string[] orderModes = { "品阶↓", "稀有度↓" };
    private int orderIdx = 0;

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

        currentCard    = card;
        currentSlot    = slot;
        selectedGearId = string.Empty;

        gameObject.SetActive(true);
        StartCoroutine(AfterEnableRoutine());
    }

    public void Close() => gameObject.SetActive(false);

    private IEnumerator AfterEnableRoutine()
    {
        yield return null;   // 第 1 帧：元素实例化
        yield return null;   // 第 2 帧：布局完成，才能拿到宽度
        BuildGearOptions();
    }

    /*───────── ⑤ 生成网格 ─────────*/
    /*───────────────────────────── 新版 BuildGearOptions ─────────────────────────────*/
    private void BuildGearOptions()
    {
        if (gearItemTpl == null || optionContainer == null || playerGearBank == null)
        {
            Debug.LogWarning("[GearPanel] 资源未绑定完整");
            return;
        }

        optionContainer.Clear();
        equipBtn.SetEnabled(false);

        var db = gearDB != null ? gearDB : GearDatabaseStatic.Instance;
        if (db == null)
        {
            Debug.LogError("[GearPanel] GearStaticDB 缺失！");
            return;
        }

        /*── 过滤 ─*/
        var source = playerGearBank.All
                    .Select(pg => (pg, st: db.Get(pg.staticId)))
                    .Where(t => t.st != null &&
                                (currentSlot == EquipSlotType.Weapon
                                    ? t.st.kind == GearKind.Weapon
                                    : t.st.kind == GearKind.Armor));

        /*── 排序 ─*/
        var ordered = orderIdx == 1
            ? source.OrderBy(t => t.st.tier)            // 稀有度
            : source.OrderBy(t => (int)t.st.tier);      // 品阶

        var pairList = ordered.ToList();
        if (pairList.Count == 0)
        {
            optionContainer.Add(new Label("（暂无符合条件的装备）")
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    height         = 60,
                    color          = Color.gray
                }
            });
            optionContainer.RefreshAfterHierarchyChange();
            return;
        }

        /*── 计算列数：itemsPerRow = 0 时自动计算 ─*/
        int cols = itemsPerRow > 0 ? itemsPerRow : CalcAutoColumns();
        cols = Mathf.Max(1, cols);

        /*── 按行生成 ─*/
        int idx = 0;
        while (idx < pairList.Count)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom  = rowGap,
                    flexShrink    = 0,
                    flexGrow      = 0,
                    alignItems      = Align.Center,
                    justifyContent  = Justify.SpaceEvenly,
                }
            };

            for (int c = 0; c < cols && idx < pairList.Count; c++, idx++)
            {
                var pg = pairList[idx].pg;
                var st = pairList[idx].st;

                var ve = gearItemTpl.Instantiate();
                ve.name         = $"Gear_{st.id}";
                ve.style.width  = gearSize;
                ve.style.height = 140;
                if (c > 0) ve.style.marginLeft = colGap;

                /* 图标 */
                var icon = ve.Q<VisualElement>("GearIcon");
                if (icon != null && st.iconSprite != null)
                {
                    icon.style.backgroundImage        = new StyleBackground(st.iconSprite);
                    icon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                }
                var (atk, def) = st.CalcStats(pg.level);
                /* 文本 */
                ve.Q<Label>("GearName")   ?.SetTextSafe(st.name);
                ve.Q<Label>("GearAtkStat")?.SetTextSafe($"攻击 +{atk:N0}");
                ve.Q<Label>("GearDefStat")?.SetTextSafe($"防御 +{def:N0}");

                /* 已装备 */
                bool isEquipped = currentCard != null &&
                                ((currentSlot == EquipSlotType.Weapon && currentCard.equip.weaponId == st.id) ||
                                (currentSlot == EquipSlotType.Armor  && currentCard.equip.armorId  == st.id));
                ve.Q<VisualElement>("IfEquiped")?.SetDisplay(isEquipped);

                /* 点击选中 —— 两行写法 */
                var clickTarget = (VisualElement)(ve.Q<Button>("GearOption") ?? (VisualElement)ve);
                clickTarget.RegisterCallback<ClickEvent>(_ =>
                {
                    selectedGearId = st.id;
                    equipBtn.SetEnabled(true);
                    Highlight(ve);
                });

                row.Add(ve);
            }

            optionContainer.Add(row);
        }

        optionContainer.RefreshAfterHierarchyChange();
    }

/*──────────────────────── 计算自动列数 ────────────────────────*/
private int CalcAutoColumns()
{
    // 有时 resolvedStyle.width 在第一帧是 0，保险起见也试 worldBound
    float width = optionContainer.resolvedStyle.width;
    if (width < 1f) width = optionContainer.worldBound.width;

    if (width < 1f) return 1; // 依然拿不到宽度就退回 1 列

    // (总宽 + 间距) / (卡宽 + 间距) → 能放多少个
    return Mathf.Max(1,
        Mathf.FloorToInt((width + colGap) / (gearSize + colGap)));
}

/*──────── 修改 AfterEnableRoutine：多等待 1 帧布局 ────────*/



    /*───────── ⑥ 装备按钮 ─────────*/
    private void OnEquipClicked()
    {
        if (string.IsNullOrEmpty(selectedGearId)) return;

        var db   = gearDB != null ? gearDB : GearDatabaseStatic.Instance;
        var gear = db.Get(selectedGearId);

        if (EquipSystem.EquipGear(currentCard, gear))
        {
            PlayerCardBankMgr.I.BroadcastCardUpdated(currentCard.id);
            Close();
        }
        else
        {
            PopupManager.Show("提示", "槽位未解锁或装备失败");
        }
    }

    /*───────── ⑦ 高亮条目 ─────────*/
    private void Highlight(VisualElement ve)
    {
        foreach (var child in optionContainer.Query<VisualElement>().ToList())
            child.RemoveFromClassList("selected");
        ve.AddToClassList("selected");
    }
}

/*──────── 工具扩展 ────────*/
static class UILabelGearExt
{
    public static void SetTextSafe(this Label lbl, string txt)
    {
        if (lbl != null) lbl.text = txt;
    }

    public static void SetDisplay(this VisualElement ve, bool show)
    {
        if (ve != null)
            ve.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
