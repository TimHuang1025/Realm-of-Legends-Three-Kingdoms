using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Kamgam.UIToolkitScrollViewPro;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GiftPanelController : MonoBehaviour
{
    [SerializeField] private CardInventoryUI inventoryUI;   // 取当前武将
    [SerializeField] private UnitGiftLevel giftLevelUI;   // 同一个组件实例

    [SerializeField] private VhSizer vhSizer;

    [Header("Gift 資料")]
    [SerializeField] private GiftDatabase giftDatabase;
    [SerializeField] private VisualTreeAsset giftOptionTpl;

    [Header("ScrollViewPro 名稱")]
    [SerializeField] private string optionContainerName = "GiftList";

    /*──────── 排序 ────────*/
    private enum SortMode { StockDesc, ValueDesc }
    private SortMode currentSort = SortMode.StockDesc;

    /*──────── 运行时字段 ────────*/
    private UIDocument doc;
    private ScrollViewPro optionContainer;

    private readonly List<Entry> entries = new();
    private Entry selectedEntry;

    private class Entry
    {
        public GiftData data;
        public VisualElement root;
        public VisualElement mask;
        public Color origBorder;
    }
    /*──────── 礼物 ────────*/
    private int sendCount = 1;       // 送几个，默认 1          // “–”
    private Button confirmBtn;           // “赠送”

    private Label heroGiftLvLbl;
    private ProgressBar heroGiftExpBar;


    /*──────── 生命周期 ────────*/
    private void Awake()
    {
        doc = GetComponent<UIDocument>();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        var root = doc.rootVisualElement;

        heroGiftLvLbl = root.Q<Label>("HeroGiftLevel");
        heroGiftExpBar = root.Q<ProgressBar>("HeroGiftLvBar");
        RefreshHeroGiftUI();

        /* 關閉按鈕 */
        Button b = root.Q<Button>("ClosePanel");
        if (b != null) b.clicked += Close;
        b = root.Q<Button>("CloseBtn2");
        if (b != null) b.clicked += Close;

        VisualElement blk = root.Q<VisualElement>("BlackSpace");
        if (blk != null)
            blk.RegisterCallback<ClickEvent>(_ => Close());

        /* 容器 */
        optionContainer = root.Q<ScrollViewPro>(optionContainerName);

        /* 排序按鈕 */
        Button sortBtn = root.Q<Button>("GiftOrderSort");
        if (sortBtn != null)
        {
            sortBtn.text = "库存↓";
            sortBtn.clicked += () =>
            {
                currentSort = currentSort == SortMode.StockDesc
                            ? SortMode.ValueDesc
                            : SortMode.StockDesc;

                sortBtn.text = currentSort == SortMode.StockDesc ? "库存↓" : "赏赐↓";
                BuildGiftOptions();
            };
        }

        confirmBtn = root.Q<Button>("SendGiftBtn");

        if (confirmBtn != null) confirmBtn.clicked += OnConfirmGift;

    }

    /*──────── 外部接口 ────────*/
    public void Open()
    {
        gameObject.SetActive(true);
        StartCoroutine(AfterEnableRoutine());
    }
    public void Close() => gameObject.SetActive(false);

    private IEnumerator AfterEnableRoutine()
    {
        yield return null;
        BuildGiftOptions();
        if (vhSizer != null) vhSizer.Apply();
    }

    /*──────── 生成列表 ────────*/
    private void BuildGiftOptions()
    {
        if (giftDatabase == null || giftOptionTpl == null || optionContainer == null)
        {
            Debug.LogWarning("[GiftPanel] 资源缺失"); return;
        }

        optionContainer.Clear();
        entries.Clear();
        selectedEntry = null;

        IEnumerable<GiftData> ordered =
            currentSort == SortMode.StockDesc
            ? giftDatabase.gifts.OrderByDescending(g => g.stock)
            : giftDatabase.gifts.OrderByDescending(g => g.value);

        int idx = 0;
        foreach (GiftData g in ordered)
        {
            VisualElement ve = giftOptionTpl.Instantiate();
            ve.name = $"GiftOption_{idx++}";
            ve.style.position = Position.Relative;

            /* 文本 */
            ve.Q<Label>("GiftItem").SetTextSafe(g.name);
            ve.Q<Label>("GiftAddNum").SetTextSafe(g.value.ToString());
            ve.Q<Label>("GiftStockNum").SetTextSafe(g.stock.ToString());

            /* 圖標 */
            VisualElement icon = ve.Q<VisualElement>("GiftIcon");
            if (icon != null && g.icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(g.icon);
                icon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }

            /* DimMask */
            VisualElement mask = ve.Q<VisualElement>("DimMask");
            if (mask == null) { Debug.LogError("缺少 DimMask"); continue; }
            mask.style.display = DisplayStyle.Flex;
            SetMaskAlpha(mask, g.stock == 0 ? 0.70f : 0f);

            /* 邊框原色 */
            Color orig = ve.resolvedStyle.borderLeftColor;

            /* 建立條目 */
            Entry entry = new Entry { data = g, root = ve, mask = mask, origBorder = orig };
            entries.Add(entry);

            /* 點擊 / 禁用 */
            VisualElement clickTarget = ve.Q<Button>("GiftOption") ?? ve;
            if (g.stock == 0)
            {
                clickTarget.pickingMode = PickingMode.Ignore;
                clickTarget.focusable = false;
            }
            else
            {
                clickTarget.RegisterCallback<ClickEvent>(_ =>
                {
                    selectedEntry = entry;
                    UpdateMasks();
                    Debug.Log($"[GiftPanel] 選中 {g.name} (stock {g.stock})");
                });
            }

            optionContainer.Add(ve);
        }

        optionContainer.RefreshAfterHierarchyChange();
        if (selectedEntry != null)
        {
            // 找到同名条目（库存被扣了但还是它）
            VisualElement sameVe = optionContainer.Q<VisualElement>(selectedEntry.root.name);
            if (sameVe != null)
            {
                sameVe.Focus();         // 把键盘/游戏手柄焦点回到此条
                UpdateMasks();          // 重新高亮边框
            }
        }
    }

    /*──────── 更新遮罩 & 邊框 ────────*/
    private void UpdateMasks()
    {
        foreach (Entry e in entries)
        {
            bool sel = e == selectedEntry;
            bool oos = e.data.stock == 0;
            float a = sel ? (oos ? 0.80f : 0f) : 0.35f;

            SetMaskAlpha(e.mask, a);

            Color col = a > 0f ? Color.black : e.origBorder;
            e.root.style.borderTopColor =
            e.root.style.borderRightColor =
            e.root.style.borderBottomColor =
            e.root.style.borderLeftColor = col;
        }
    }

    /*──────── 工具 ────────*/
    private static void SetMaskAlpha(VisualElement mask, float alpha)
    {
        Color c = mask.resolvedStyle.backgroundColor;
        mask.style.backgroundColor = new Color(c.r, c.g, c.b, alpha);
    }

    void OnConfirmGift()
    {
        if (selectedEntry == null) return;
        GiftData gift = selectedEntry.data;

        // 1) 库存校验
        if (gift.stock < sendCount) return;

        // 2) 扣库存
        gift.stock -= sendCount;

        // 3) 计算总经验
        int totalExp = gift.value * sendCount;

        // 4) 给当前武将加经验（自动升级）
        giftLevelUI.AddExp(totalExp);
        RefreshHeroGiftUI();

        // 5) 刷礼物列表数字 & 重置选择
        selectedEntry.root.Q<Label>("GiftStockNum")
                  .text = gift.stock.ToString();
        

        if (gift.stock == 0)
        {
            selectedEntry.mask.style.display = DisplayStyle.Flex;
            SetMaskAlpha(selectedEntry.mask, 0.70f);
            selectedEntry.root.pickingMode = PickingMode.Ignore;
        }
    }
    void RefreshHeroGiftUI()
    {
        if (heroGiftLvLbl == null || heroGiftExpBar == null) return;

        heroGiftLvLbl.text = giftLevelUI.GetLvText();        // ← 直接用文字接口
        heroGiftExpBar.lowValue  = 0;
        heroGiftExpBar.highValue = 100;
        heroGiftExpBar.value     = giftLevelUI.GetExpPercent();
        heroGiftExpBar.title     = giftLevelUI.GetExpPercent() >= 100 ? "MAX"
                                : $"{(int)giftLevelUI.GetExpPercent()}%";
        heroGiftExpBar.MarkDirtyRepaint();
    }


}

/*──────── Label 擴展 ────────*/
public static class UITKExt
{
    public static void SetTextSafe(this Label lbl, string txt)
    {
        if (lbl != null) lbl.text = txt;
    }
}
