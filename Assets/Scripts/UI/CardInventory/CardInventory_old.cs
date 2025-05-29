using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CardInventoryOld : MonoBehaviour
{
    /* ───── 数据源 ───── */
    [Header("数据源")]
    public CardDatabase cardDatabase;

    /* ───── 网格布局 ───── */
    [Header("网格")]
    public int   rows     = 3;      // 固定行数
    public int   cardSize = 180;    // 卡片边长
    public float colGap   = 8f;     // 列间距
    public float rowGap   = 12f;    // 行间距

    /* ───── 卡片外观 ───── */
    [Header("统一外观")]
    public float borderWidth  = 2f;
    public float cornerRadius = 10f;
    public Color defaultBorderColor = Color.black;
    public Color fillColor          = new(0.85f, 0.85f, 0.85f, 1);

    /* ───── 等级文字 ───── */
    [Header("等级文字")]
    public int   levelFontSize  = 16;
    public Color levelFontColor = Color.white;
    public Font  levelFont;                       // 自定义字体
    [Header("等级描边 (Unity 2023.1+)")]
    public Color levelOutlineColor = Color.black;
    [Range(0, 8)] public int levelOutlineWidth = 2;

    /* ───── 星星 ───── */
    [Header("星星外观")]
    public Sprite filledStarSprite;
    public Sprite emptyStarSprite;
    public int    starSize = 22;
    public int    starGap  = 2;

    /* ───── 品质颜色 ───── */
    static readonly Color COLOR_S = Hex("#FFC947");
    static readonly Color COLOR_A = Hex("#A44DFF");
    static readonly Color COLOR_B = Hex("#37D5FF");

    /* 选中展示区 */
    VisualElement selectedCardImage;
    Label cardNameLabel;

    /* ──────────────── */
    void OnEnable()
    {

        if (cardDatabase == null) return;

        var root = GetComponent<UIDocument>().rootVisualElement;
        cardNameLabel = root.Q<Label>("CardName");

        /* 0) 选中展示位 */
        selectedCardImage = root.Q<VisualElement>("SelectedCardImage");

        /* 1) ScrollView */
        var scroll = root.Q<ScrollView>("CardScrollView");
        if (scroll == null)
        {
            scroll = new ScrollView { name = "CardScrollView" };
            root.Add(scroll);
        }

        scroll.mode = ScrollViewMode.Horizontal;
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        scroll.verticalScrollerVisibility   = ScrollerVisibility.Hidden;

        /* 设定整体高度 */
        scroll.style.height = rows * cardSize + (rows - 1) * rowGap;

        /* 2) 列排布容器 */
        var cc = scroll.contentContainer;
        cc.Clear();
        cc.style.flexDirection = FlexDirection.Column;

        /* 3) 创建行容器 */
        var rowVE = new VisualElement[rows];
        for (int r = 0; r < rows; r++)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom  = (r < rows - 1) ? rowGap : 0
                }
            };
            cc.Add(row);
            rowVE[r] = row;
        }

        /* 4) 生成卡片 */
        for (int i = 0; i < cardDatabase.cards.Count; i++)
        {
            var data = cardDatabase.cards[i];
            rowVE[i % rows].Add(CreateCardButton(data));
        }
    }

    /* ───── 创建按钮 ───── */
    Button CreateCardButton(CardInfo d)
    {
        var btn = new Button
        {
            name = d.cardName,      // 内部标识，不会显示
            style =
            {
                width       = cardSize,
                height      = cardSize,
                marginRight = colGap,
                backgroundColor = fillColor,
                unityTextAlign  = TextAnchor.MiddleCenter,

                /* 圆角 */
                borderTopLeftRadius     = cornerRadius,
                borderTopRightRadius    = cornerRadius,
                borderBottomLeftRadius  = cornerRadius,
                borderBottomRightRadius = cornerRadius,

                /* 边框粗细 */
                borderLeftWidth   = borderWidth,
                borderRightWidth  = borderWidth,
                borderTopWidth    = borderWidth,
                borderBottomWidth = borderWidth
            }
        };

        /* 边框颜色 */
        var borderCol = d.quality switch
        {
            "S" => COLOR_S,
            "A" => COLOR_A,
            "B" => COLOR_B,
            _   => HexOrDefault(d.quality, defaultBorderColor)
        };
        btn.style.borderLeftColor =
        btn.style.borderRightColor =
        btn.style.borderTopColor   =
        btn.style.borderBottomColor = borderCol;

        /* 背景图：头像 */
        if (d.iconSprite != null)
            btn.style.backgroundImage = new StyleBackground(d.iconSprite);

        /* ── Overlay：等级 & 星星 ── */
        var overlay = new VisualElement
        {
            pickingMode = PickingMode.Ignore,
            style =
            {
                position = Position.Absolute,
                left = 0, top = 0, right = 0, bottom = 0
            }
        };
        btn.Add(overlay);

        /* 等级标签 */
        var lvlLabel = new Label($"{d.level}")
        {
            style =
            {
                position  = Position.Absolute,
                right      = 6,
                top       = 4,
                fontSize  = levelFontSize,
                color     = levelFontColor,
                unityFont = levelFont
#if UNITY_2023_1_OR_NEWER
                ,unityTextOutlineColor = levelOutlineColor,
                 unityTextOutlineWidth = levelOutlineWidth
#endif
            }
        };
        overlay.Add(lvlLabel);

        /* 星星行 */
        overlay.Add(BuildStarRow(d.rank));


        /* 点击事件：切换大图 */
        btn.clicked += () =>
        {
            if (selectedCardImage == null) return;

            var body = d.fullBodySprite ?? d.iconSprite;
            if (body != null)
            {
                selectedCardImage.style.backgroundImage =
                    new StyleBackground(body);
                selectedCardImage.style.unityBackgroundScaleMode =
                    ScaleMode.ScaleToFit;
            }
            cardNameLabel.text = d.cardName;
            Debug.Log(cardNameLabel.text +" 替换Card Name: " + d.cardName);
        };

        return btn;
    }

    /* ───── 星星行 ───── */
    VisualElement BuildStarRow(int rank)
    {
        var row = new VisualElement
        {
            pickingMode = PickingMode.Ignore,
            style =
            {
                position = Position.Absolute,
                bottom   = 4,
                left     = 0,
                right    = 0,
                flexDirection  = FlexDirection.Row,
                justifyContent = Justify.Center
            }
        };

        rank = Mathf.Clamp(rank, 0, 5);
        for (int i = 0; i < 5; i++)
        {
            row.Add(new Image
            {
                sprite    = i < rank ? filledStarSprite : emptyStarSprite,
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width  = starSize,
                    height = starSize,
                    marginRight = (i < 4) ? starGap : 0
                }
            });
        }
        return row;
    }

    /* ───── 小工具 ───── */
    static Color Hex(string hex)
        { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

    static Color HexOrDefault(string hex, Color fallback)
        => ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
}
