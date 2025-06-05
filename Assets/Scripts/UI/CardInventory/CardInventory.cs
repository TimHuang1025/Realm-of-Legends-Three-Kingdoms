using UnityEngine;
using UnityEngine.UIElements;
using Kamgam.UIToolkitScrollViewPro;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class CardInventory : MonoBehaviour
{
    /*──────────── 1. Inspector 资源 ────────────*/
    [Header("UI 文件 / 数据")]
    [SerializeField] private VisualTreeAsset cardTemplate;
    [SerializeField] private CardDatabase cardDatabase;

    [Header("星星贴图")]
    [SerializeField] private Sprite filledStarSprite;
    [SerializeField] private Sprite emptyStarSprite;

    [Header("稀有度贴图 (S / A / B)")]
    [SerializeField] private Sprite raritySpriteS;
    [SerializeField] private Sprite raritySpriteA;
    [SerializeField] private Sprite raritySpriteB;

    [Header("星星大小")]
    [SerializeField] private float starSize = 22f;

    [Header("品质边框颜色")]
    [SerializeField] private Color colorS = new(1f, 0.78f, 0.28f);
    [SerializeField] private Color colorA = new(0.64f, 0.30f, 1f);
    [SerializeField] private Color colorB = new(0.22f, 0.84f, 1f);
    [SerializeField] private Color defaultBorder = Color.black;

    [Header("网格布局")]
    [SerializeField] private int rows = 3;
    [SerializeField] private int cardSize = 180;
    [SerializeField] private float colGap = 8f;
    [SerializeField] private float rowGap = 12f;

    /*──────────── 2. 运行时引用 ────────────*/
    private VisualElement selectedCardVE;
    private Label cardNameLabel;

    private ScrollViewPro scroll;
    private VisualElement gridRoot;
    private CardInfo currentSelected;
    Dictionary<CardInfo, Button> cardBtnMap = new();  // 卡片 ↔ 按钮

    // ★★★ 新增：排序按钮与状态 ★★★
    private Button orderButton;               // #OrderButton
    readonly string[] sortModes = { "稀有度排序", "星级排序", "等级排序" };
    int modeIdx = 0;

    [SerializeField] CardInventoryUI inventoryUI;



    /*──────────────────────────────────────*/
    private void OnEnable()
    {
        if (cardTemplate == null || cardDatabase == null) return;

        var root = GetComponent<UIDocument>().rootVisualElement;

        /*──────── 左侧展示区 ────────────*/
        selectedCardVE = root.Q<VisualElement>("SelectedCardImage");
        cardNameLabel = root.Q<Label>("CardName");

        /*──────── ScrollViewPro ─────────*/
        scroll = root.Q<ScrollViewPro>("CardScrollView")
              ?? new ScrollViewPro { name = "CardScrollView" };
        if (scroll.parent == null) root.Add(scroll);

        scroll.mode = ScrollViewMode.Horizontal;
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        scroll.infinite = false;
        scroll.style.height = rows * cardSize + (rows - 1) * rowGap;

        gridRoot = scroll.contentContainer;
        gridRoot.style.flexDirection = FlexDirection.Row;
        gridRoot.style.paddingLeft = 20;
        gridRoot.style.paddingTop = 20;

        /*──────── 排序按钮 ─────────────*/
        orderButton = root?.Q<Button>("OrderButton");
        if (orderButton == null)
        {
            Debug.LogError("找不到 #OrderButton");
            return;
        }
        orderButton.text = sortModes[modeIdx];
        orderButton.clicked += OnOrderButtonClick;

        /*──────── 首次生成 ─────────────*/
        ApplySort();      // 先按默认模式排一次
        FocusFirstCard();
    }

    /*──────── 按钮点击：循环排序模式 ──────*/
    void OnOrderButtonClick()
    {
        modeIdx = (modeIdx + 1) % sortModes.Length;
        orderButton.text = sortModes[modeIdx];
        ApplySort();
    }

    /*──────── 根据当前模式排序并刷新 ──────*/
    void ApplySort()
    {
        switch (sortModes[modeIdx])
        {
            case "稀有度排序":
                cardDatabase.cards.Sort((a, b) =>
                    QualityWeight(b.quality).CompareTo(QualityWeight(a.quality)));
                break;

            case "星级排序":   // 5★ > 4★ …
                cardDatabase.cards.Sort((a, b) => b.rank.CompareTo(a.rank));
                break;

            case "等级排序":
                cardDatabase.cards.Sort((a, b) => b.level.CompareTo(a.level));
                break;
        }
        BuildGrid();
    }

    int QualityWeight(string q) => q switch
    {
        "S" => 3,
        "A" => 2,
        "B" => 1,
        _ => 0
    };

    /*──────── 生成 / 刷新网格 ───────────*/
    public void BuildGrid()
    {
        gridRoot.Clear();
        cardBtnMap.Clear();
        VisualElement btnToFocus = null;

        int cardIdx = 0;
        while (cardIdx < cardDatabase.cards.Count)
        {
            var col = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginRight   = colGap,
                    width         = cardSize,
                    flexShrink    = 0,
                    flexGrow      = 0
                }
            };

            for (int r = 0; r < rows && cardIdx < cardDatabase.cards.Count; r++)
            {
                var cardContainer = BuildCard(cardDatabase.cards[cardIdx]);

                // 找到真正带 userData 的按钮
                var cardBtn = cardContainer.Q<Button>("CardRoot");
                cardBtnMap[cardDatabase.cards[cardIdx]] = cardBtn;
                if (cardBtn != null && cardBtn.userData == currentSelected)
                    btnToFocus = cardBtn;

                if (r > 0) cardContainer.style.marginTop = rowGap;
                col.Add(cardContainer);
                cardIdx++;
            }
            gridRoot.Add(col);
        }

        scroll.RefreshAfterHierarchyChange();

        // 聚焦并滚动到旧选中卡；否则退回首张
        if (btnToFocus != null)
        {
            btnToFocus.schedule.Execute(() =>
            {
                btnToFocus.Focus();
                //scroll.SnapToItem(btnToFocus, animate:true);
            }).ExecuteLater(0);
        }
        else
        {
            FocusFirstCard();
        }
    }
    void OnDisable()
    {
        /* ✅ 退场时解除事件订阅，防止泄漏 */
        foreach (var c in cardDatabase.cards)
            c.OnStatsChanged -= OnCardStatChanged;
    }

        /*──────── 帮助函数：聚焦第一张 ─────────*/
    void FocusFirstCard()
    {
        if (gridRoot.childCount == 0) return;

        var firstCol = gridRoot[0];
        if (firstCol.childCount == 0) return;

        VisualElement cardRoot = firstCol[0];
        var cardBtn  = cardRoot.Q<Button>("CardRoot");
        var cardData = cardBtn?.userData as CardInfo;
        if (cardBtn == null || cardData == null) return;

        /* 1. 刷左侧信息 & 装备槽 */
        ShowSelected(cardData);
        inventoryUI.OnCardClicked(cardData);
        RefreshEquipSlots(cardData, cardRoot);

        /* 2. 下一帧再设置焦点与滚动，防止被布局抢焦点 */
        cardBtn.schedule.Execute(() =>
        {
            cardBtn.Focus();
            scroll.ScrollTo(cardBtn);
        }).ExecuteLater(1);   // ← 延迟 1 帧
    }


    class CardButtonData
    {
        public CardInfo      card;
        public VisualElement root;   // 这张卡片的根（方便外部再找槽）
    }

    /*──────── 构建单张卡片 ───────────────*/
    public VisualElement BuildCard(CardInfo data)
    {
        var container = cardTemplate.Instantiate();
        container.style.width = cardSize;
        container.style.height = cardSize;
        container.style.marginRight = colGap;
        container.style.flexShrink = 0;

        var cardBtn = container.Q<Button>("CardRoot");
        var lvlLabel = container.Q<Label>("Level");
        var starPanel = container.Q<VisualElement>("StarPanel");
        var rarityVe = container.Q<VisualElement>("CardRarity");

        cardBtn.AddToClassList("cardroot");
        cardBtn.userData = data;

        /* 背景图 */
        if (data.iconSprite != null)
        {
            cardBtn.style.backgroundImage = new StyleBackground(data.iconSprite);
            cardBtn.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }

        /* 边框 & 等级颜色 */
        Color borderCol = data.quality switch
        {
            "S" => colorS,
            "A" => colorA,
            "B" => colorB,
            _ => defaultBorder
        };
        cardBtn.style.borderTopColor =
        cardBtn.style.borderRightColor =
        cardBtn.style.borderBottomColor =
        cardBtn.style.borderLeftColor = borderCol;

        lvlLabel.text = data.level.ToString();
        data.OnStatsChanged += OnCardStatChanged;
        lvlLabel.style.color = borderCol;

        /* 星级 */
        FillStars(starPanel, data.rank);

        /* 稀有度角标 */
        if (rarityVe != null)
        {
            Sprite sprite = data.quality switch
            {
                "S" => raritySpriteS,
                "A" => raritySpriteA,
                "B" => raritySpriteB,
                _ => null
            };

            if (sprite == null)
            {
                rarityVe.style.display = DisplayStyle.None;
            }
            else
            {
                rarityVe.style.display = DisplayStyle.Flex;
                rarityVe.style.backgroundImage = new StyleBackground(sprite);
                rarityVe.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
            }
        }

        /* 点击行为 */
        cardBtn.clicked += () =>
        {
        ShowSelected(data);
        inventoryUI?.OnCardClicked(data);
        RefreshEquipSlots(data, container);

            //Debug.Log($"{data.cardName}  武器:{data.equip.weaponUnlocked}  盔甲:{data.equip.armorUnlocked}  坐骑:{data.equip.mountUnlocked}");
        };
        cardBtn.RegisterCallback<FocusInEvent>(_ =>
        {
            RefreshEquipSlots(data, container);
        });


        /* 获焦/失焦 动画 */
        var normalScale = new Scale(new Vector3(1f, 1f, 1f));
        var pressedScale = new Scale(new Vector3(1.10f, 1.10f, 1f));
        var overlay = container.Q<VisualElement>("DimOverlay");
        var glowS = container.Q<VisualElement>("Glow_S");
        var glowA = container.Q<VisualElement>("Glow_A");
        var glowB = container.Q<VisualElement>("Glow_B");

        float normalBorder = 4f;
        float focusedBorder = 8f;

        void ShowGlow(string q)
        {
            glowS.style.display = glowA.style.display = glowB.style.display = DisplayStyle.None;
            var target = q == "S" ? glowS : q == "A" ? glowA : glowB;
            target.RemoveFromClassList("rarity-s");
            target.RemoveFromClassList("rarity-a");
            target.RemoveFromClassList("rarity-b");
            target.AddToClassList($"rarity-{q.ToLower()}");
            target.style.display = DisplayStyle.Flex;
        }

        cardBtn.RegisterCallback<FocusInEvent>(e =>
        {

            cardBtn.style.scale = pressedScale;
            rarityVe.style.scale = pressedScale;
            overlay.style.display = DisplayStyle.Flex;
            ShowGlow(data.quality);
            ShowSelected(data);                           // <<< 新增: 焦点时同步展示

            cardBtn.style.borderTopWidth =
            cardBtn.style.borderRightWidth =
            cardBtn.style.borderBottomWidth =
            cardBtn.style.borderLeftWidth = focusedBorder;

        });

        cardBtn.RegisterCallback<FocusOutEvent>(e =>
        {
            cardBtn.style.scale = normalScale;
            rarityVe.style.scale = normalScale;
            overlay.style.display = DisplayStyle.None;
            glowS.style.display = glowA.style.display = glowB.style.display = DisplayStyle.None;

            cardBtn.style.borderTopWidth =
            cardBtn.style.borderRightWidth =
            cardBtn.style.borderBottomWidth =
            cardBtn.style.borderLeftWidth = normalBorder;
        });

        return container;
    }
    void OnCardStatChanged(CardInfo card)
    {
        /* 1. 更新网格中等级数字 */
        if (cardBtnMap.TryGetValue(card, out var btn))
            btn.Q<Label>("Level").text = card.level.ToString();

        /* 2. 若正选中，刷新左侧大图 / 名字 */
        if (card == currentSelected)
            ShowSelected(card);

        /* 3. 如果当前是“等级排序”，需要重排一次 */
        if (sortModes[modeIdx] == "等级排序")
            ApplySort();
    }

    /*──────── 填充星星 ───────────────*/
    private void FillStars(VisualElement panel, int rank)
    {
        panel.Clear();
        rank = Mathf.Clamp(rank, 0, 5);

        for (int i = 0; i < 5; i++)
        {
            var img = new Image
            {
                sprite = i < rank ? filledStarSprite : emptyStarSprite,
                scaleMode = ScaleMode.ScaleToFit
            };
            img.style.width = starSize;
            img.style.height = starSize;
            img.style.marginRight = i < 4 ? 2 : 0;
            panel.Add(img);
        }
    }

    /*──────── 展示大图 / 名称 ─────────*/
    private void ShowSelected(CardInfo data)
    {
        currentSelected = data;
        if (selectedCardVE != null)
        {
            var pic = data.fullBodySprite ?? data.iconSprite;
            if (pic != null)
            {
                selectedCardVE.style.backgroundImage = new StyleBackground(pic);
                selectedCardVE.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
            }
        }
        if (cardNameLabel != null)
            cardNameLabel.text = data.cardName;
    }
    void BindEquipSlot(VisualElement slot, bool unlocked)
    {
        if (slot == null) return;

        // 先去掉两种 class，保证互斥
        slot.RemoveFromClassList("equipmentlocked");
        slot.RemoveFromClassList("equipmentunlocked");

        // 再根据布尔值添加正确的那一个
        slot.AddToClassList(unlocked ? "equipmentunlocked"
                                    : "equipmentlocked");
    }

    void RefreshEquipSlots(CardInfo card, VisualElement cardRoot)
    {
        var weapon = cardRoot.Q<VisualElement>("weaponslot");
        var armor  = cardRoot.Q<VisualElement>("armorslot");
        var mount  = cardRoot.Q<VisualElement>("horseslot");

        BindEquipSlot(weapon, card.equip.weaponUnlocked);
        BindEquipSlot(armor,  card.equip.armorUnlocked);
        BindEquipSlot(mount,  card.equip.mountUnlocked);
    }


}
