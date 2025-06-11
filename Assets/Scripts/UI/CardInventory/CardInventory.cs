using UnityEngine;
using UnityEngine.UIElements;
using Kamgam.UIToolkitScrollViewPro;
using System.Collections.Generic;
using System.Linq;
using System;
using Game.Core;
using UIExt;
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
    [SerializeField] private PlayerHorseBank     horseBank;      
    [SerializeField] private HorseDatabaseStatic horseStaticDB;  



    [SerializeField] UnitGiftLevel unitGiftLevel;
    [SerializeField] UpgradePanelController upgradePanelCtrl;

    [Header("稀有度贴图 (S / A / B)")]
    [SerializeField] private Sprite raritySpriteS;
    [SerializeField] private Sprite raritySpriteA;
    [SerializeField] private Sprite raritySpriteB;

    [Header("星星贴图")]
    [SerializeField] public Sprite emptyStarSprite;   // 灰/空星 
    [SerializeField] public Sprite blueStarSprite;    // 蓝星
    [SerializeField] public Sprite purpleStarSprite;  // 紫星
    [SerializeField] public Sprite goldStarSprite;    // 金星
    [SerializeField] int    starSize = 24;     // 像素或 USS 单位


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
        var list = cardDatabase.AllCards.ToList();

        list.Sort((a, b) =>
        {
            bool ownedA = cardBank.Get(a.id) != null;
            bool ownedB = cardBank.Get(b.id) != null;
            if (ownedA != ownedB) return ownedB.CompareTo(ownedA); // 已拥有在前

            /* 玩家动态数据（可能为 null） */
            var dynA = cardBank.Get(a.id);
            var dynB = cardBank.Get(b.id);

            int starA = dynA?.star  ?? -1;
            int starB = dynB?.star  ?? -1;
            int lvlA  = dynA?.level ?? -1;
            int lvlB  = dynB?.level ?? -1;

            switch (sortModes[modeIdx])
            {
                /* ───── 稀有度排序 ───── */
                case "稀有度排序":
                {
                    int tierCmp = a.tier.CompareTo(b.tier);  // S(0) < A(1)…
                    if (tierCmp != 0) return tierCmp;

                    int starCmp = starB.CompareTo(starA);    // 星多在前
                    if (starCmp != 0) return starCmp;

                    return lvlB.CompareTo(lvlA);             // 等级高在前
                }

                /* ───── 星级排序 ───── */
                case "星级排序":
                {
                    int starCmp = starB.CompareTo(starA);    // 星多在前
                    if (starCmp != 0) return starCmp;

                    int tierCmp = a.tier.CompareTo(b.tier);  // S > A > B
                    if (tierCmp != 0) return tierCmp;

                    return lvlB.CompareTo(lvlA);             // 等级高在前
                }

                /* ───── 等级排序 ───── */
                case "等级排序":
                default:
                {
                    int lvlCmp = lvlB.CompareTo(lvlA);       // 等级高在前
                    if (lvlCmp != 0) return lvlCmp;

                    int starCmp = starB.CompareTo(starA);    // 星多在前
                    if (starCmp != 0) return starCmp;

                    return a.tier.CompareTo(b.tier);         // S > A > B
                }
            }
        });

        BuildGrid(list);    // 重新生成网格
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
    // CardInventory.cs  —— BuildCard（完整版）
    public VisualElement BuildCard(
            CardInfoStatic info,
            Action<CardInfoStatic, PlayerCard> onClickOverride = null,
            bool broadcastSelection = true)   // ← 新增：是否广播到 InfoPanel
    {
        /*── 1. 实例化模板 ───────────────────────*/
        var container = cardTemplate.Instantiate();
        container.style.width       = cardSize;
        container.style.height      = cardSize;
        container.style.marginRight = colGap;
        container.style.flexShrink  = 0;

        /*── 2. 抓子元素 ────────────────────────*/
        var cardBtn   = container.Q<Button>("CardRoot");
        var lvlLabel  = container.Q<Label>("Level");
        var starPanel = container.Q<VisualElement>("StarPanel");
        var rarityVe  = container.Q<VisualElement>("CardRarity");
        var dim       = container.Q<VisualElement>("DimOverlay");

        cardBtn.userData    = info;
        cardBtnMap[info.id] = cardBtn;

        /*── 3. 静态视觉（头像 / 边框）──────────*/
        if (info.iconSprite != null)
        {
            cardBtn.style.backgroundImage          = new StyleBackground(info.iconSprite);
            cardBtn.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }

        Color borderCol = info.tier switch
        {
            Tier.S => colorS,
            Tier.A => colorA,
            Tier.B => colorB,
            _      => defaultBorder
        };
        cardBtn.style.borderTopColor =
        cardBtn.style.borderRightColor =
        cardBtn.style.borderBottomColor =
        cardBtn.style.borderLeftColor = borderCol;
        lvlLabel.style.color          = borderCol;

        /*── 4. 动态数据：玩家是否拥有 ──────────*/
        PlayerCard pCard = cardBank.Get(info.id);       // null = 未拥有
        bool owned = pCard != null;

        if (owned)
        {
            lvlLabel.text = pCard.level.ToString();
            FillStars(starPanel, pCard.star);
            RefreshEquipSlots(pCard, container);

            // ── 装备槽点击（保持你原来的写法） ──
            var weaponSlot = container.Q<Button>("weaponslot");
            var armorSlot  = container.Q<Button>("armorslot");
            var mountSlot  = container.Q<Button>("horseslot");

            if (weaponSlot != null)
                weaponSlot.RegisterCallback<ClickEvent>(
                    _ =>
                    {
                        if (broadcastSelection)        // 仅背包界面需要
                            inventoryUI.HandleSlotClick(info, pCard, EquipSlotType.Weapon);
                        else
                            Debug.Log("weapon click");
                    });

            if (armorSlot != null)
                armorSlot.RegisterCallback<ClickEvent>(
                    _ =>
                    {
                        if (broadcastSelection)
                            inventoryUI.HandleSlotClick(info, pCard, EquipSlotType.Armor);
                    });

            
            if (mountSlot != null)
                mountSlot.RegisterCallback<ClickEvent>(
                    _ =>
                    {
                        if (broadcastSelection)
                            inventoryUI.HandleSlotClick(info, pCard, EquipSlotType.Mount);
                    });
            
        }
        else
        {
            dim.style.display = DisplayStyle.Flex;
            lvlLabel.text     = string.Empty;
            FillStars(starPanel, 0);
            RefreshEquipSlots(null, container);
            rarityVe.style.display = DisplayStyle.None;
        }

        /*── 5. 稀有度角标 ──────────────────────*/
        if (owned && rarityVe != null)
        {
            Sprite badge = info.tier switch
            {
                Tier.S => raritySpriteS,
                Tier.A => raritySpriteA,
                Tier.B => raritySpriteB,
                _      => null
            };
            if (badge != null)
            {
                rarityVe.style.display                 = DisplayStyle.Flex;
                rarityVe.style.backgroundImage         = new StyleBackground(badge);
                rarityVe.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
            }
        }

        /*── 6. 点击行为 ───────────────────────*/
        cardBtn.clicked += () =>
        {
            if (onClickOverride != null)
            {
                onClickOverride(info, pCard);          // 弹窗模式走这里
            }
            else
            {
                currentSelectedStatic = info;
                currentSelectedDyn    = pCard;

                if (broadcastSelection)
                {
                    ShowSelected(info, pCard);         // 更新 InfoPanel
                    inventoryUI?.OnCardClicked(info, pCard);
                    BroadcastSelection(info, pCard);
                }
            }
        };

        /*── 7. 焦点动画 ───────────────────────*/
        var normalScale  = new Scale(Vector3.one);
        var pressedScale = new Scale(new Vector3(1.10f, 1.10f, 1f));
        var overlay      = container.Q<VisualElement>("FocusOverlay");
        var glowS        = container.Q<VisualElement>("Glow_S");
        var glowA        = container.Q<VisualElement>("Glow_A");
        var glowB        = container.Q<VisualElement>("Glow_B");
        float normalBorder  = 4f;
        float focusedBorder = 8f;

        void ShowGlow(Tier t)
        {
            glowS.style.display = glowA.style.display = glowB.style.display = DisplayStyle.None;
            var target = t switch
            {
                Tier.S => glowS,
                Tier.A => glowA,
                Tier.B => glowB,
                _ => null
            };
            if (target != null)
                target.style.display = DisplayStyle.Flex;
        }

        cardBtn.RegisterCallback<FocusInEvent>(_ =>
        {
            cardBtn.style.scale   = pressedScale;
            rarityVe.style.scale  = pressedScale;
            overlay.style.display = DisplayStyle.Flex;
            ShowGlow(info.tier);

            if (broadcastSelection)                // 只在背包界面更新 InfoPanel
                ShowSelected(info, pCard);

            cardBtn.style.borderTopWidth =
            cardBtn.style.borderRightWidth =
            cardBtn.style.borderBottomWidth =
            cardBtn.style.borderLeftWidth = focusedBorder;
        });

        cardBtn.RegisterCallback<FocusOutEvent>(_ =>
        {
            cardBtn.style.scale   = normalScale;
            rarityVe.style.scale  = normalScale;
            overlay.style.display = DisplayStyle.None;
            glowS.style.display = glowA.style.display = glowB.style.display = DisplayStyle.None;

            cardBtn.style.borderTopWidth =
            cardBtn.style.borderRightWidth =
            cardBtn.style.borderBottomWidth =
            cardBtn.style.borderLeftWidth = normalBorder;
        });

        /*── 8. 焦点广播（仅背包需要） ───────────*/
        if (broadcastSelection)
            cardBtn.RegisterCallback<FocusInEvent>(_ => BroadcastSelection(info, pCard));

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
    /*───────────────────────────────────────────*/
/* A. 旧调用保持不变：FillStars(panel, star) */
    /*───────────────────────────────────────────*/
    private void FillStars(VisualElement panel, int star)
    {
        var rule = CardDatabaseStatic.Instance.GetStar(star);
        int lit         = rule != null ? rule.starsInFrame               : 0;
        string colorKey = rule != null ? rule.frameColor.ToLowerInvariant() : "blue";
        FillStars(panel, lit, colorKey);          // 调到新版核心
    }

    /*───────────────────────────────────────────*/
    /* B. 核心版本：亮星数量 + 颜色               */
    /*───────────────────────────────────────────*/
    private void FillStars(VisualElement panel, int lit, string frameColor)
    {
        panel.Clear();
        lit = Mathf.Clamp(lit, 0, 5);

        Sprite litSprite = frameColor switch
        {
            "purple" => purpleStarSprite,
            "gold"   => goldStarSprite,
            "blue"   => blueStarSprite,
            _        => blueStarSprite
        };

        for (int i = 0; i < 5; i++)
        {
            var img = new Image
            {
                sprite    = i < lit ? litSprite : emptyStarSprite,
                scaleMode = ScaleMode.ScaleToFit
            };
            img.style.width       = starSize;
            img.style.height      = starSize;
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

        ApplySlotIcon(weapon, equip?.weaponUuid,   false);
        ApplySlotIcon(armor,  equip?.armorUuid,    false);
        ApplySlotIcon(mount,  equip?.accessoryUuid, true);
    }


    void ApplySlotIcon(Button slot, string uuid, bool isMount)
    {
        if (slot == null) return;

        var iconVe = slot.Q<VisualElement>("EquipIcon");
        if (iconVe == null) return;

        // ------ 1) 槽为空：隐藏图标 ------
        if (string.IsNullOrEmpty(uuid))
        {
            iconVe.style.display = DisplayStyle.None;
            return;
        }

        // ------ 2) 取动态条目 + 静态条目 ------
        Sprite iconSprite = null;

        if (isMount)
        {
            var pHorse = horseBank.Get(uuid);
            var hStat  = horseStaticDB.Get(pHorse?.staticId);
            iconSprite = hStat?.iconSprite;
        }
        else
        {
            var pGear  = gearBank.Get(uuid);
            var gStat  = gearStaticDB.Get(pGear?.staticId);
            iconSprite = gStat?.iconSprite;
        }

        // ------ 3) 设置或隐藏 ------
        if (iconSprite != null)
        {
            iconVe.style.backgroundImage = new StyleBackground(iconSprite);
            iconVe.style.display         = DisplayStyle.Flex;
            iconVe.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }
        else
        {
            iconVe.style.display = DisplayStyle.None;
        }
    }

}
