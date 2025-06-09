// Assets/Scripts/Game/UI/HeroSelectionPanel.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Kamgam.UIToolkitScrollViewPro;

/*──────── 外部枚举 ─────────*/
public enum LineupSlot { Main, Sub1, Sub2, Strategist }

[RequireComponent(typeof(UIDocument))]
public class HeroSelectionPanel : MonoBehaviour
{
    /*───────── ① Inspector ─────────*/
    [Header("工厂 & 数据库")]
    [SerializeField] private CardInventory      cardFactory;    // 生成单张卡 UI
    [SerializeField] private CardDatabaseStatic cardDBStatic;   // 静态卡库
    [SerializeField] private PlayerCardBank     cardBank;       // 玩家已有
    [SerializeField] private UIDocument         uiDoc;          // 可不填，自动抓

    [Header("ScrollViewPro 名称")]
    [SerializeField] private string heroPoolName = "HeroSelectionPool";

    [Header("网格布局 (可选)")]
    [SerializeField] private int   itemsPerRow = 0;    // <=0 时自动列数
    [SerializeField] private float cardWidth   = 210f; // 单张宽高
    [SerializeField] private float colGap      = 8f;   // 列间距
    [SerializeField] private float rowGap      = 12f;  // 行间距

    /*───────── ② 运行时字段 ─────────*/
    private ScrollViewPro heroPool;
    private VisualElement root;
    private Label         titleLabel;

    private LineupSlot currentSlot;
    private Action<CardInfoStatic, PlayerCard> onSelected;   // 选中回调

    /*───────── ③ 生命周期 ─────────*/
    private void Awake()
    {
        if (uiDoc == null) uiDoc = GetComponent<UIDocument>();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        InitUI();
        BuildHeroGrid();
        heroPool.RegisterCallback<GeometryChangedEvent>(OnViewportChanged);
    }

    private void OnDisable()
    {
        heroPool?.UnregisterCallback<GeometryChangedEvent>(OnViewportChanged);
    }

    private void OnViewportChanged(GeometryChangedEvent _) => BuildHeroGrid();

    /*───────── ④ 对外接口 ─────────*/
    public void Open(LineupSlot slot, Action<CardInfoStatic, PlayerCard> callback)
    {
        currentSlot = slot;
        onSelected  = callback;
        gameObject.SetActive(true);   // 触发 OnEnable → BuildHeroGrid
    }

    public void Close() => gameObject.SetActive(false);

    /*───────── ⑤ UI 初始化 ─────────*/
    private void InitUI()
    {
        heroPool = uiDoc.rootVisualElement.Q<ScrollViewPro>(heroPoolName);
        if (heroPool == null)
        {
            Debug.LogError("[HeroPanel] 找不到 ScrollViewPro: " + heroPoolName);
            enabled = false; return;
        }

        titleLabel = uiDoc.rootVisualElement.Q<Label>("SlotPickedTitle");
        SetTitleBySlot();

        heroPool.mode = ScrollViewMode.Vertical;
        heroPool.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        heroPool.verticalScrollerVisibility = ScrollerVisibility.Hidden;

        root = heroPool.contentContainer;
        root.style.paddingLeft = 10;
        root.style.paddingTop = 15;
        root.style.flexDirection = FlexDirection.Column;
        root.style.alignSelf = Align.Center;
    }

    private void SetTitleBySlot()
    {
        if (titleLabel == null) return;
        titleLabel.text = currentSlot switch
        {
            LineupSlot.Main       => "选择主将",
            LineupSlot.Sub1       => "选择副将 1",
            LineupSlot.Sub2       => "选择副将 2",
            LineupSlot.Strategist => "选择军师",
            _                     => "选择武将"
        };
    }

    /*───────── ⑥ 网格生成 ─────────*/
    private void BuildHeroGrid()
    {
        if (cardBank == null || cardFactory == null || cardDBStatic == null || root == null) return;

        root.Clear();

        // 决定列数
        int cols = itemsPerRow > 0 ? itemsPerRow : CalcAutoColumns();
        cols = Mathf.Max(cols, 1);

        var owned = cardBank.cards;
        if (owned.Count == 0)
        {
            root.Add(new Label("（暂无可用武将）")
            { style = { height = 60, unityTextAlign = TextAnchor.MiddleCenter, color = Color.gray } });
            heroPool.RefreshAfterHierarchyChange();
            return;
        }

        int idx = 0;
        while (idx < owned.Count)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom  = rowGap,
                    flexShrink    = 0,
                }
            };

            for (int c = 0; c < cols && idx < owned.Count; c++, idx++)
            {
                var dyn  = owned[idx];
                var info = cardDBStatic.Get(dyn.id);
                if (info == null) continue;

                var ve = cardFactory.BuildCard(
                    info,
                    (_, _) => { onSelected?.Invoke(info, dyn); Close(); },
                    broadcastSelection: false);

                ve.style.width  = cardWidth;
                ve.style.height = cardWidth;          // 正方形
                if (c > 0) ve.style.marginLeft = colGap;

                row.Add(ve);
            }
            root.Add(row);
        }
        heroPool.RefreshAfterHierarchyChange();
    }

    /*───────── ⑦ 自动列数 ─────────*/
    private int CalcAutoColumns()
    {
        float width = heroPool.contentViewport.layout.width;
        if (width < 1f) width = heroPool.contentViewport.worldBound.width;
        if (width < 1f) return 1;

        return Mathf.Max(1, Mathf.FloorToInt((width + colGap) / (cardWidth + colGap)));
    }
}
