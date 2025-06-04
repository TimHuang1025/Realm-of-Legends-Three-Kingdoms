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
    /* ───────── Inspector ───────── */
    [Header("引用")]
    [SerializeField] private CardInventory cardFactory;  // “工厂”空物体
    [SerializeField] private CardDatabase  cardDB;       // 武将库
    [SerializeField] private UIDocument    uiDoc;        // 本面板 UIDocument

    [Header("卡片与间距")]
    [SerializeField] private float cardWidth = 210f;     // 估算：卡片含留白宽
    [SerializeField] private float colGap    = 8f;
    [SerializeField] private float rowGap    = 12f;

    /* ───────── 运行时 ───────── */
    private ScrollViewPro  heroPool;         // ScrollViewPro 组件
    private VisualElement  root;             // contentContainer
    private int            cachedColumns = -1;
    private bool           isBuilt       = false;
    private Action<CardInfo> onSelected;    // 选中回调
    
    private LineupSlot currentSlot;
    private Label titleLabel;             // 标题标签


    /* ===== 公共接口 ===== */
    public void Open(LineupSlot slot, Action<CardInfo> callback)
    {
        currentSlot = slot;            // 记录“谁”在召唤
        onSelected = callback;

        gameObject.SetActive(true);

        if (!isBuilt)
            BuildGrid(CalcColumns());
    }

    public void Close() => gameObject.SetActive(false);

    /* ===== 生命周期 ===== */
    private void OnEnable()
    {
        heroPool = uiDoc.rootVisualElement.Q<ScrollViewPro>("HeroSelectionPool");
        if (heroPool == null)
        {
            Debug.LogError("未找到 ScrollViewPro：HeroSelectionPool");
            return;
        }
        titleLabel = uiDoc.rootVisualElement.Q<Label>("SlotPickedTitle");

        // 根据槽位改标题
        if (titleLabel != null)
        {
            switch (currentSlot)
            {
                case LineupSlot.Main:        titleLabel.text = "选择主将";        break;
                case LineupSlot.Sub1:        titleLabel.text = "选择副将1";     break;
                case LineupSlot.Sub2:        titleLabel.text = "选择副将2";     break;
                case LineupSlot.Strategist:  titleLabel.text = "选择军师";        break;
                default:                     titleLabel.text = "选择武将";        break;
            }
        }

        root = heroPool.contentContainer;

        /* ScrollView 基础设置 */
        heroPool.mode                       = ScrollViewMode.Vertical;
        heroPool.infinite                   = false;
        heroPool.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        heroPool.verticalScrollerVisibility   = ScrollerVisibility.Hidden;

        root.style.paddingLeft   = 20;
        root.style.paddingTop    = 20;
        root.style.flexDirection = FlexDirection.Column;

        /* 监听窗口尺寸变化，列数改变时重建 */
        heroPool.RegisterCallback<GeometryChangedEvent>(_ => TryRebuild());
        TryRebuild();   // 进场先构建一次
    }

    /* ===== 自适应列数 ===== */
    private void TryRebuild()
    {
        int cols = CalcColumns();
        if (cols == cachedColumns) return;

        cachedColumns = cols;
        BuildGrid(cols);
    }

    private int CalcColumns()
    {
        float available = heroPool.worldBound.width
                        - root.resolvedStyle.paddingLeft
                        - root.resolvedStyle.paddingRight;

        int cols = Mathf.FloorToInt(available / cardWidth);
        return Mathf.Max(cols, 1);   // 至少 1 列
    }

    /* ===== 构建卡片网格 ===== */
    private void BuildGrid(int columns)
    {
        root.Clear();
        int idx = 0;

        while (idx < cardDB.cards.Count)
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

            for (int c = 0; c < columns && idx < cardDB.cards.Count; c++)
            {
                var cardInfo = cardDB.cards[idx];
                var cardVe   = cardFactory.BuildCard(cardInfo);

                /* 行内左右间距 */
                if (c > 0) cardVe.style.marginLeft = colGap;

                /* 点击：回调 & 关闭面板 */
                var btn = cardVe.Q<Button>("CardRoot");
                if (btn != null)
                {
                    btn.clicked += () =>
                    {
                        onSelected?.Invoke(cardInfo);  // 把选中卡返回
                        Close();
                    };
                }

                row.Add(cardVe);
                idx++;
            }

            root.Add(row);
        }

        heroPool.RefreshAfterHierarchyChange();
        isBuilt = true;
    }
}
