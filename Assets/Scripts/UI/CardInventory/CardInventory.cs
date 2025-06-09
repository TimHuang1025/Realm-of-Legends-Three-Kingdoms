using UnityEngine;
using UnityEngine.UIElements;
using Kamgam.UIToolkitScrollViewPro;
using System.Collections.Generic;
using System.Linq;
using System; 
using Game.Core;
using Game.Data;                      // CardDatabaseStatic, CardInfoStatic

[RequireComponent(typeof(UIDocument))]
public class CardInventory : MonoBehaviour
{
    /*──────────── 1. Inspector 资源 ────────────*/
    [Header("UI 文件 / 数据")]
    [SerializeField] private VisualTreeAsset cardTemplate;
    [SerializeField] private CardDatabaseStatic cardDatabase;
    [SerializeField] private PlayerCardBank cardBank;
    [SerializeField] private PlayerGearBank     gearBank;
    [SerializeField] private GearDatabaseStatic gearStaticDB;


    [SerializeField] UnitGiftLevel unitGiftLevel;
    [SerializeField] UpgradePanelController upgradePanelCtrl;


    [Header("星星贴图")]
    [SerializeField] private Sprite filledStarSprite;
    [SerializeField] private Sprite emptyStarSprite;
    public Sprite FilledStarSprite => filledStarSprite;
    public Sprite EmptyStarSprite => emptyStarSprite;

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
    Dictionary<string, Button> cardBtnMap = new();  // 卡片 ↔ 按钮
    private CardInfoStatic currentSelectedStatic;
    private PlayerCard currentSelectedDyn;



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
        PlayerCardBankMgr.I.onCardChanged += OnCardStatChanged;
        PlayerCardBankMgr.I.onCardUpdated += OnCardUpdated;
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
        var list = cardDatabase.All.ToList();   // 取所有静态卡

        list.Sort((a, b) =>
        {
            /* 1. 拥有优先 */
            bool ownedA = cardBank.Get(a.id) != null;
            bool ownedB = cardBank.Get(b.id) != null;
            int ownCmp = ownedB.CompareTo(ownedA);   // true > false
            if (ownCmp != 0) return ownCmp;

            /* 2. 稀有度：S(0) < A(1) < B(2)…   用 a.tier.CompareTo(b.tier) */
            int tierCmp = a.tier.CompareTo(b.tier);   // 小的在前 → S > A > B
            if (tierCmp != 0) return tierCmp;

            /* 3. 星级 */
            int starA = cardBank.Get(a.id)?.star ?? -1;
            int starB = cardBank.Get(b.id)?.star ?? -1;
            int starCmp = starB.CompareTo(starA);      // 星多的在前
            if (starCmp != 0) return starCmp;

            /* 4. 等级 */
            int lvA = cardBank.Get(a.id)?.level ?? -1;
            int lvB = cardBank.Get(b.id)?.level ?? -1;
            return lvB.CompareTo(lvA);                // 等级高的在前
        });

        BuildGrid(list);   // 把排好序的列表交给 BuildGrid
    }

    /*──────── 生成 / 刷新网格 ───────────*/
    public void BuildGrid(IReadOnlyList<CardInfoStatic> cards)
    {
        if (cards == null) return;

        gridRoot.Clear();
        cardBtnMap.Clear();

        VisualElement btnToFocus = null;

        int idx = 0;
        while (idx < cards.Count)
        {
            /*─ 新建一列 ─*/
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

            /*─ 每列塞 rows 张 ─*/
            for (int r = 0; r < rows && idx < cards.Count; r++)
            {
                CardInfoStatic info = cards[idx];

                /* 生成卡片 UI */
                var cardContainer = BuildCard(info);

                /* 行内垂直间距 */
                if (r > 0) cardContainer.style.marginTop = rowGap;

                /* 记录按钮映射与焦点检查 */
                var cardBtn = cardContainer.Q<Button>("CardRoot");
                if (cardBtn != null)
                {
                    cardBtnMap[info.id] = cardBtn;

                    if (currentSelectedStatic != null && cardBtn.userData == currentSelectedStatic)
                        btnToFocus = cardBtn;
                }

                col.Add(cardContainer);
                idx++;
            }

            gridRoot.Add(col);
        }

        scroll.RefreshAfterHierarchyChange();

        /*─ 恢复焦点 / 默认选第一张 ─*/
        if (btnToFocus != null)
        {
            btnToFocus.schedule.Execute(() =>
            {
                btnToFocus.Focus();
                scroll.ScrollTo(btnToFocus);   // 或 SnapToItem
            }).ExecuteLater(0);
        }
        else
        {
            FocusFirstCard();
        }
    }
    void OnDisable()
    {
        PlayerCardBankMgr.I.onCardUpdated -= OnCardUpdated;
    }
    void OnCardUpdated(string id)
    {
        if (!cardBtnMap.TryGetValue(id, out var btn)) return;

        var pCard = cardBank.Get(id);
        // 星级 / 等级文本
        btn.Q<Label>("Level").text = pCard.level.ToString();
        FillStars(btn.Q<VisualElement>("StarPanel"), pCard.star);

        // 三小槽
        RefreshEquipSlots(pCard, btn.parent);
    }

    /*──────── 帮助函数：聚焦第一张 ─────────*/
    void FocusFirstCard()
    {
        if (gridRoot.childCount == 0) return;

        var firstCol = gridRoot[0];
        if (firstCol.childCount == 0) return;

        /* 取第一张卡的模板实例 */
        VisualElement cardRoot = firstCol[0];
        var cardBtn = cardRoot.Q<Button>("CardRoot");
        var info = cardBtn?.userData as CardInfoStatic;   // ★ 改类型
        if (cardBtn == null || info == null) return;

        /* 对应的玩家动态数据 */
        var pCard = cardBank.Get(info.id);                    // 可能为 null（未拥有）

        /* 左侧展示 */
        currentSelectedStatic = info;
        currentSelectedDyn = pCard;
        ShowSelected(info, pCard);
        RefreshEquipSlots(pCard, cardRoot);            // ★ 传 EquipStatus

        BroadcastSelection(info, pCard);

        /* 延迟 1 帧设焦点，以免布局抢焦 */
        cardBtn.schedule.Execute(() =>
        {
            cardBtn.Focus();
            scroll.ScrollTo(cardBtn);
        }).ExecuteLater(1);

    }
    public void BroadcastSelection(CardInfoStatic info, PlayerCard dyn)
    {
        inventoryUI?.OnCardClicked(info, dyn);
    }




    class CardButtonData
    {
        public CardInfoStatic card;
        public VisualElement root;   // 这张卡片的根（方便外部再找槽）
    }

    /*──────── 构建单张卡片 ───────────────*/
    /// <summary>
    /// 生成单张武将卡的 UI（已兼容“未拥有”灰显、四维星级等）
    /// </summary>
    public VisualElement BuildCard(CardInfoStatic info, Action<CardInfoStatic, PlayerCard> onClickOverride = null)
    {
        /*── 1. 实例化模板 ───────────────────────*/
        var container = cardTemplate.Instantiate();
        container.style.width = cardSize;
        container.style.height = cardSize;
        container.style.marginRight = colGap;
        container.style.flexShrink = 0;

        /*── 2. 抓子元素 ────────────────────────*/
        var cardBtn = container.Q<Button>("CardRoot");
        var lvlLabel = container.Q<Label>("Level");
        var starPanel = container.Q<VisualElement>("StarPanel");
        var rarityVe = container.Q<VisualElement>("CardRarity");
        var dim = container.Q<VisualElement>("DimOverlay");

        cardBtn.userData = info;              // 静态放进 userData
        cardBtnMap[info.id] = cardBtn;        // 记录映射
        var cardRoot = new Button
        {
            name = "CardRoot"          // 供 Q<Button>("CardRoot") 使用
        };
        cardRoot.AddToClassList("cardroot"); // 让 USS 宽高、边框等样式生效

        // 如果模板 USS 被删，依然兜底 210×210
        cardRoot.style.width = 210;
        cardRoot.style.height = 210;

        // 可聚焦，方便 ScrollViewPro 定位
        cardRoot.focusable = true;
        cardRoot.tabIndex = 0;

        /*── 3. 静态视觉（头像 / 边框）──────────*/
        if (info.iconSprite != null)
        {
            cardBtn.style.backgroundImage = new StyleBackground(info.iconSprite);
            cardBtn.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }

        Color borderCol = info.tier switch
        {
            Tier.S => colorS,
            Tier.A => colorA,
            Tier.B => colorB,
            _ => defaultBorder
        };
        cardBtn.style.borderTopColor =
        cardBtn.style.borderRightColor =
        cardBtn.style.borderBottomColor =
        cardBtn.style.borderLeftColor = borderCol;
        lvlLabel.style.color = borderCol;

        /*── 4. 动态数据：玩家是否拥有 ──────────*/
        PlayerCard pCard = cardBank.Get(info.id);   // null = 未拥有
        bool owned = pCard != null;

        if (owned)
        {
            lvlLabel.text = pCard.level.ToString();
            FillStars(starPanel, pCard.star);
            RefreshEquipSlots(pCard, container);
            var weaponSlot = container.Q<Button>("weaponslot");
            var armorSlot = container.Q<Button>("armorslot");
            var mountSlot = container.Q<Button>("horseslot");

            if (weaponSlot != null)
                weaponSlot.RegisterCallback<ClickEvent>(
                    //_ => inventoryUI.HandleSlotClick(info, pCard, EquipSlotType.Weapon));
                    _ => Debug.Log("weapon click"));


            if (armorSlot != null)
                armorSlot.RegisterCallback<ClickEvent>(
                    _ => inventoryUI.HandleSlotClick(info, pCard, EquipSlotType.Armor));

            /*
                        if (mountSlot != null)
                            mountSlot.RegisterCallback<ClickEvent>(
                                _ => inventoryUI.HandleSlotClick(info, pCard, EquipSlotType.Mount));
            */
        }
        else
        {
            dim.style.display = DisplayStyle.Flex;  // 灰幕
            lvlLabel.text = "";
            FillStars(starPanel, 0);
            RefreshEquipSlots(null, container);     // 三槽锁住
            rarityVe.style.display = DisplayStyle.None;
        }

        /*── 5. 稀有度角标 (仅拥有时显示) ───────*/
        if (owned && rarityVe != null)
        {
            Sprite badge = info.tier switch
            {
                Tier.S => raritySpriteS,
                Tier.A => raritySpriteA,
                Tier.B => raritySpriteB,
                _ => null
            };
            if (badge != null)
            {
                rarityVe.style.display = DisplayStyle.Flex;
                rarityVe.style.backgroundImage = new StyleBackground(badge);
                rarityVe.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
            }
        }

        /*── 6. 点击 / 聚焦 行为 ───────────────*/
        cardBtn.clicked += () =>
        {
            if (onClickOverride != null)
            {
                onClickOverride(info, pCard);
            }
            else
            {
                currentSelectedStatic = info;   // 更新当前选中
                currentSelectedDyn = pCard;  // 可能为 null

                ShowSelected(info, pCard);
                inventoryUI?.OnCardClicked(info, pCard);
                BroadcastSelection(info, pCard);
            }
        };

        cardBtn.RegisterCallback<FocusInEvent>(_ =>
        {
            BroadcastSelection(info, pCard);
        });

        /* 获焦/失焦 动画 */
        var normalScale = new Scale(new Vector3(1f, 1f, 1f));
        var pressedScale = new Scale(new Vector3(1.10f, 1.10f, 1f));
        var overlay = container.Q<VisualElement>("FocusOverlay");
        var glowS = container.Q<VisualElement>("Glow_S");
        var glowA = container.Q<VisualElement>("Glow_A");
        var glowB = container.Q<VisualElement>("Glow_B");

        float normalBorder = 4f;
        float focusedBorder = 8f;

        void ShowGlow(Tier tier)
        {
            glowS.style.display = glowA.style.display = glowB.style.display = DisplayStyle.None;
            VisualElement target = tier switch
            {
                Tier.S => glowS,
                Tier.A => glowA,
                Tier.B => glowB,
                _ => null
            };
            if (target == null) return;

            target.RemoveFromClassList("rarity-s");
            target.RemoveFromClassList("rarity-a");
            target.RemoveFromClassList("rarity-b");
            target.AddToClassList($"rarity-{tier.ToString().ToLower()}");
            target.style.display = DisplayStyle.Flex;
        }

        cardBtn.RegisterCallback<FocusInEvent>(_ =>
        {
            cardBtn.style.scale = pressedScale;
            rarityVe.style.scale = pressedScale;
            overlay.style.display = DisplayStyle.Flex;

            ShowGlow(info.tier);                   // ← 用枚举
            ShowSelected(info, pCard);             // ← 传静态+动态

            cardBtn.style.borderTopWidth =
            cardBtn.style.borderRightWidth =
            cardBtn.style.borderBottomWidth =
            cardBtn.style.borderLeftWidth = focusedBorder;
        });

        cardBtn.RegisterCallback<FocusOutEvent>(_ =>
        {
            cardBtn.style.scale = normalScale;
            rarityVe.style.scale = normalScale;
            overlay.style.display = DisplayStyle.None;
            glowS.style.display = glowA.style.display =
            glowB.style.display = DisplayStyle.None;

            cardBtn.style.borderTopWidth =
            cardBtn.style.borderRightWidth =
            cardBtn.style.borderBottomWidth =
            cardBtn.style.borderLeftWidth = normalBorder;
        });


        return container;
    }



    void OnCardStatChanged(string id)
    {
        var pCard = cardBank.Get(id);
        if (pCard == null) return;

        /* 1. 更新等级文本 */
        if (cardBtnMap.TryGetValue(id, out var btn))
            btn.Q<Label>("Level").text = pCard.level.ToString();

        /* 2. 若正选中，刷新左侧大图 / 名字 */
        if (currentSelectedStatic != null && currentSelectedStatic.id == id)
            ShowSelected(currentSelectedStatic, pCard);

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
    void ShowSelected(CardInfoStatic info, PlayerCard pCard)
    {
        // 静态内容
        //Debug.Log($"info == {(info == null)} | selectedCardVE == {(selectedCardVE == null)} | cardNameLabel == {(cardNameLabel == null)}");
        selectedCardVE.style.backgroundImage = new StyleBackground(
            info.fullBodySprite ?? info.iconSprite);
        cardNameLabel.text = info.displayName;

        // 动态内容
        //levelLabel.text = pCard != null ? $"Lv.{pCard.level}" : "—";
        //FillStars(starPanel, pCard?.star ?? 0);
    }
    public static void BindEquipSlot(
            Button slot,
            bool unlocked,
            string tipWhenLocked = "该槽位未解锁")
    {
        if (slot == null) return;

        /*── 视觉 ───────────────────────────*/
        slot.EnableInClassList("equipmentlocked", !unlocked);
        slot.EnableInClassList("equipmentunlocked", unlocked);

        /*── 保存状态 + 提示 ────────────────*/
        slot.userData = unlocked;          // 用来给点击回调判断
        slot.tooltip = unlocked ? null : tipWhenLocked;
    }


    public void RefreshEquipSlots(PlayerCard dyn, VisualElement root)
    {
        if (root == null) return;

        var equip = dyn?.equip;

        // 1) 在 Info 面板里是 "WeaponSlot_L"，在卡片里可能是 "weaponslot"
        //    如果命名一致，就用同一个；不一致就传不同 name
        var weapon = root.Q<Button>("weaponslot");
        var armor = root.Q<Button>("armorslot");
        var mount = root.Q<Button>("horseslot");


        bool unlockedWeapon = dyn != null && dyn.equip.weaponUnlocked;
        bool unlockedArmor = dyn != null && dyn.equip.armorUnlocked;
        bool unlockedMount = dyn != null && dyn.equip.mountUnlocked;

        BindEquipSlot(weapon, unlockedWeapon, "武器槽未解锁");
        BindEquipSlot(armor, unlockedArmor, "防具槽未解锁");
        BindEquipSlot(mount, unlockedMount, "坐骑槽未解锁");

        ApplySlotIcon(weapon, equip?.weaponUuid);
        ApplySlotIcon(armor,  equip?.armorUuid);
        ApplySlotIcon(mount,  equip?.accessoryUuid);
    }
    private void ApplySlotIcon(Button slot, string gearUuid)
    {
        var iconVe = slot?.Q<VisualElement>("EquipIcon");
        if (iconVe == null) return;

        if (string.IsNullOrEmpty(gearUuid))
        {
            iconVe.style.display = DisplayStyle.None;
            return;
        }

        // ① 直接用 Inspector 拖进来的 gearBank
        var pGear = gearBank.Get(gearUuid);
        if (pGear == null)
        {
            iconVe.style.display = DisplayStyle.None;
            return;
        }

        // ② 用 gearStaticDB 拿静态表
        var st = gearStaticDB.Get(pGear.staticId);
        if (st?.iconSprite != null)
        {
            iconVe.style.display = DisplayStyle.Flex;
            iconVe.style.backgroundImage = new StyleBackground(st.iconSprite);
        }
        else
        {
            iconVe.style.display = DisplayStyle.None;
        }
    }

}
