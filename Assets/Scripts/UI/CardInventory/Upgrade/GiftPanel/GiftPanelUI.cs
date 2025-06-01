using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Kamgam.UIToolkitScrollViewPro;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GiftPanelController : MonoBehaviour
{
    /*──────── Inspector ────────*/
    [Header("可選：自適應腳本")]
    [SerializeField] private VhSizer          vhSizer;

    [Header("Gift 資料")]
    [SerializeField] private GiftDatabase     giftDatabase;
    [SerializeField] private VisualTreeAsset  giftOptionTpl;

    [Header("ScrollViewPro 名稱")]
    [SerializeField] private string optionContainerName = "GiftList";

    /*──────── 排序 ────────*/
    private enum SortMode { StockDesc, ValueDesc }
    private SortMode currentSort = SortMode.StockDesc;

    /*──────── 运行时字段 ────────*/
    private UIDocument    doc;
    private ScrollViewPro optionContainer;

    private readonly List<Entry> entries = new();
    private Entry selectedEntry;

    private class Entry
    {
        public GiftData      data;
        public VisualElement root;
        public VisualElement mask;
        public Color         origBorder;
    }

    /*──────── 生命周期 ────────*/
    private void Awake()
    {
        doc = GetComponent<UIDocument>();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        var root = doc.rootVisualElement;

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
            ve.Q<Label>("GiftItem")    .SetTextSafe(g.name);
            ve.Q<Label>("GiftAddNum")  .SetTextSafe(g.value.ToString());
            ve.Q<Label>("GiftStockNum").SetTextSafe(g.stock.ToString());

            /* 圖標 */
            VisualElement icon = ve.Q<VisualElement>("GiftIcon");
            if (icon != null && g.icon != null)
            {
                icon.style.backgroundImage          = new StyleBackground(g.icon);
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
                clickTarget.focusable   = false;
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
    }

    /*──────── 更新遮罩 & 邊框 ────────*/
    private void UpdateMasks()
    {
        foreach (Entry e in entries)
        {
            bool sel = e == selectedEntry;
            bool oos = e.data.stock == 0;
            float a  = sel ? (oos ? 0.80f : 0f) : 0.35f;

            SetMaskAlpha(e.mask, a);

            Color col = a > 0f ? Color.black : e.origBorder;
            e.root.style.borderTopColor    =
            e.root.style.borderRightColor  =
            e.root.style.borderBottomColor =
            e.root.style.borderLeftColor   = col;
        }
    }

    /*──────── 工具 ────────*/
    private static void SetMaskAlpha(VisualElement mask, float alpha)
    {
        Color c = mask.resolvedStyle.backgroundColor;
        mask.style.backgroundColor = new Color(c.r, c.g, c.b, alpha);
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
