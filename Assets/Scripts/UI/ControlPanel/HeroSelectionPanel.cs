using System;
using Kamgam.UIToolkitScrollViewPro;
using UnityEngine;
using UnityEngine.UIElements;

public enum LineupSlot
{
    Main,
    Sub1,
    Sub2,
    Strategist
}

[RequireComponent(typeof(UIDocument))]
public class HeroSelectionPanel : MonoBehaviour
{
    /*──────── Inspector ────────*/
    [Header("引用")]
    [SerializeField] CardInventory      cardFactory;   // 负责生成单张卡 UI
    [SerializeField] CardDatabaseStatic cardDBStatic;  // 全部静态卡
    [SerializeField] PlayerCardBank     cardBank;      // 玩家已拥有

    [SerializeField] UIDocument uiDoc;

    [Header("卡片与间距")]
    [SerializeField] float cardWidth = 210f;   // 估算：卡片含留白宽
    [SerializeField] float colGap    = 8f;
    [SerializeField] float rowGap    = 12f;

    /*──────── 运行时 ────────*/
    ScrollViewPro heroPool;
    VisualElement root;
    Label         titleLabel;

    int  cachedColumns = -1;
    bool isBuilt       = false;

    LineupSlot currentSlot;

    // 选中回调：返回 (静态, 动态)
    Action<CardInfoStatic, PlayerCard> onSelected;

    /*──────────────── 公共接口 ─────────────────*/
    public void Open(LineupSlot slot, Action<CardInfoStatic, PlayerCard> callback)
    {
        currentSlot = slot;
        onSelected  = callback;

        gameObject.SetActive(true);

        if (!isBuilt)
            BuildGrid(CalcColumns());
    }

    public void Close() => gameObject.SetActive(false);

    /*──────────────── 生命周期 ─────────────────*/
    void OnEnable()
    {
        heroPool = uiDoc.rootVisualElement.Q<ScrollViewPro>("HeroSelectionPool");
        if (heroPool == null)
        {
            Debug.LogError("未找到 ScrollViewPro: HeroSelectionPool");
            return;
        }

        titleLabel = uiDoc.rootVisualElement.Q<Label>("SlotPickedTitle");
        SetTitleBySlot();

        root = heroPool.contentContainer;
        heroPool.mode                         = ScrollViewMode.Vertical;
        heroPool.infinite                     = false;
        heroPool.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        heroPool.verticalScrollerVisibility   = ScrollerVisibility.Hidden;

        root.style.paddingLeft   = 20;
        root.style.paddingTop    = 20;
        root.style.flexDirection = FlexDirection.Column;

        heroPool.RegisterCallback<GeometryChangedEvent>(_ => TryRebuild());
        TryRebuild();
    }

    /*──────────────── 标题 ─────────────────*/
    void SetTitleBySlot()
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

    /*──────────────── 自适应列数 ────────────*/
    void TryRebuild()
    {
        int cols = CalcColumns();
        if (cols == cachedColumns) return;

        cachedColumns = cols;
        BuildGrid(cols);
    }

    int CalcColumns()
    {
        float available = heroPool.worldBound.width
                        - root.resolvedStyle.paddingLeft
                        - root.resolvedStyle.paddingRight;

        int cols = Mathf.FloorToInt(available / cardWidth);
        return Mathf.Max(cols, 1);
    }

    /*──────────────── 构建网格 ───────────────*/
    void BuildGrid(int columns)
    {
        root.Clear();
        int idx = 0;

        // 只遍历玩家已经拥有的卡
        var owned = cardBank.cards;

        while (idx < owned.Count)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom  = rowGap,
                    flexShrink    = 0,
                    flexGrow      = 0
                }
            };

            for (int c = 0; c < columns && idx < owned.Count; c++)
            {
                PlayerCard dyn = owned[idx];
                CardInfoStatic info = cardDBStatic.Get(dyn.id);
                if (info == null) { idx++; continue; } // 若静态缺失

                // 工厂生成卡片 UI
                var cardVe = cardFactory.BuildCard(info, (stat, dyn) =>
                {
                    // 这里可以添加卡片点击时的额外逻辑
                    onSelected?.Invoke(info, dyn);
                          Close();
                });

                if (c > 0)
                    cardVe.style.marginLeft = colGap;
                row.Add(cardVe);
                idx++;
            }

            root.Add(row);
        }

        heroPool.RefreshAfterHierarchyChange();
        isBuilt = true;
    }
}