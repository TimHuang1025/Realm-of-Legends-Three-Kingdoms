using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Core;
using Game.Data;

[RequireComponent(typeof(UIDocument))]
public class CardInventoryUI : MonoBehaviour
{
    /*──────── Inspector 引用 ────────*/
    [Header("面板控制器")]

    [SerializeField] UptierPanelController uptierPanelCtrl;
    [SerializeField] private GearSelectionPanel gearPanel;

    [SerializeField] private PlayerGearBank playerGearBank;   // 玩家所有动态装备
    [SerializeField] private GearDatabaseStatic gearDB;       // 静态装备库

    [SerializeField] GiftPanelController giftPanelCtrl;
    [SerializeField] GachaPanelController gachaPanelCtrl;
    [SerializeField] PlayerBaseController playerBaseController;
    [SerializeField] UnitGiftLevel unitGiftLevel;
    [SerializeField] UpgradePanelController upgradePanelCtrl;
    [SerializeField] VhSizer vhSizer;
    [SerializeField] private CardInventory cardInv;
    [SerializeField] private ActiveSkillDatabase activeSkillDB;
    [SerializeField] private PassiveSkillDatabase passiveSkillDB;
    private VisualElement[] starSlots;
    VisualElement mainSkillImg;
    Label mainSkillNameLbl, mainSkillDescLbl;
    VisualElement passive1Img, passive2Img;
    Label passive1NameLbl, passive1DescLbl;
    Label passive2NameLbl, passive2DescLbl;

    /*──────── 私有 UI 元素 ────────*/
    VisualElement cardsVe, infoVe;
    Button returnBtn, upgradeBtn, uptierBtn, giftBtn, infoBtn, closeInfoBtn, gachaBtn;
    Label mat2valueLbl, expvalueLbl;

    /*──────── 当前选中数据 ────────*/
    CardInfoStatic currentStatic;   // 静态
    PlayerCard currentDyn;      // 动态 (null = 未拥有)

    /*========================================
     * 生命周期
     *========================================*/
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        starSlots = new VisualElement[5];
        for (int i = 0; i < 5; i++)
            starSlots[i] = root.Q<VisualElement>($"DynStar{i + 1}");
        var bank = PlayerCardBankMgr.I;
        if (bank != null)
        {
            // ⬇ 新增，用于所有动态字段实时刷新
            bank.onCardUpdated += OnCardUpdated;

            // 旧的：只在抽到 / 移除卡牌时用来重排网格
            bank.onCardChanged += OnCardChanged;
        }

        /*──── 左侧 & Info 面板 ────*/
        cardsVe = root.Q<VisualElement>("Cards");
        infoVe = root.Q<VisualElement>("Info");
        infoVe.style.display = DisplayStyle.None;
        cardsVe.style.display = DisplayStyle.Flex;

        /*──── 资源显示 ────*/
        mat2valueLbl = root.Q<Label>("mat2value");
        expvalueLbl = root.Q<Label>("expvalue");
        RefreshResource();
        PlayerResourceBank.I.onBankChanged += OnBankChanged;

        /*──── 按钮 ────*/
        returnBtn = root.Q<Button>("ReturnBtn");
        upgradeBtn = root.Q<Button>("InfoUpgradeBtn");
        uptierBtn = root.Q<Button>("InfoUptierBtn");
        giftBtn = root.Q<Button>("InfoGiftBtn");
        infoBtn = root.Q<Button>("InfoBtn");
        closeInfoBtn = root.Q<Button>("ClosePanelForInfo");
        gachaBtn = root.Q<Button>("GachaBtn");

        var weaponL = root.Q<Button>("weaponslot");
        var armorL = root.Q<Button>("armorslot");
        var mountL = root.Q<Button>("horseslot");

        weaponL?.RegisterCallback<ClickEvent>(evt =>
        {
            if (!(bool)weaponL.userData)
            {
                PopupManager.Show("提示", "武器槽未解锁");
                evt.StopPropagation();
                return;
            }
            HandleSlotClick(currentStatic, currentDyn, EquipSlotType.Weapon);
        }, TrickleDown.TrickleDown);

        armorL?.RegisterCallback<ClickEvent>(evt =>
        {
            if (!(bool)armorL.userData)
            {
                PopupManager.Show("提示", "防具槽未解锁");
                evt.StopPropagation();
                return;
            }
            HandleSlotClick(currentStatic, currentDyn, EquipSlotType.Armor);
        }, TrickleDown.TrickleDown);

        returnBtn?.RegisterCallback<ClickEvent>(_ => playerBaseController.HideCardInventoryPage());
        upgradeBtn?.RegisterCallback<ClickEvent>(_ => StartCoroutine(OpenPanel(upgradePanelCtrl)));
        uptierBtn?.RegisterCallback<ClickEvent>(_ => StartCoroutine(OpenPanel(uptierPanelCtrl)));
        giftBtn?.RegisterCallback<ClickEvent>(_ => StartCoroutine(OpenPanel(giftPanelCtrl)));
        infoBtn?.RegisterCallback<ClickEvent>(_ => OpenInfoPanel());
        closeInfoBtn?.RegisterCallback<ClickEvent>(_ => CloseInfoPanel());
        gachaBtn?.RegisterCallback<ClickEvent>(_ => playerBaseController.ShowGachaPage());

        /*──── 卡牌技能 ────*/
        mainSkillImg = root.Q<VisualElement>("MainSkillImage");
        mainSkillNameLbl = root.Q<Label>("MainSkillNameLbl");
        mainSkillDescLbl = root.Q<Label>("MainSkillDescriptionLbl");
        /* 主动技能节点已缓存，下面只缓存被动 */
        passive1Img = root.Q<VisualElement>("Passive1Image");
        passive1NameLbl = root.Q<Label>("Passive1NameLbl");
        passive1DescLbl = root.Q<Label>("Passive1DescLbl");

        passive2Img = root.Q<VisualElement>("Passive2Image");
        passive2NameLbl = root.Q<Label>("Passive2NameLbl");
        passive2DescLbl = root.Q<Label>("Passive2DescLbl");

        /*──── 监听卡牌升级／获得 ────*/
        if (PlayerCardBankMgr.I != null)
            PlayerCardBankMgr.I.onCardChanged += OnCardChanged;
    }

    void OnDisable()
    {
        if (PlayerResourceBank.I != null)
            PlayerResourceBank.I.onBankChanged -= OnBankChanged;

        if (PlayerCardBankMgr.I != null)
            PlayerCardBankMgr.I.onCardChanged -= OnCardChanged;
    }
    void OnCardUpdated(string id)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        // 只在当前 Info 面板正展示这张卡时刷新
        if (currentStatic == null || currentStatic.id != id) return;

        currentDyn = PlayerCardBankMgr.I.Data.Get(id);

        SetInfoPanelData(currentStatic, currentDyn);   // 四维 + 描述
        cardInv.RefreshEquipSlots(currentDyn, root);             // 三大槽
        unitGiftLevel.SetData(currentStatic, currentDyn);
        unitGiftLevel.RefreshUI();
        RefreshPassiveSkillUI(currentStatic, currentDyn);
    }

    /*========================================
     * 面板切换 / 弹窗
     *========================================*/
    IEnumerator OpenPanel(IUIPanelController ctrl)
    {
        if (ctrl == null) yield break;
        ctrl.Open(currentStatic, currentDyn);   // 让子面板自己决定需要哪些数据
        yield return null;                      // 等 1 帧确保布局
        vhSizer?.Apply();
    }

    void OpenInfoPanel()
    {
        cardsVe.style.display = DisplayStyle.None;
        infoVe.style.display = DisplayStyle.Flex;
        unitGiftLevel.RefreshUI();
        vhSizer?.Apply();
    }
    void CloseInfoPanel()
    {
        infoVe.style.display = DisplayStyle.None;
        cardsVe.style.display = DisplayStyle.Flex;
        vhSizer?.Apply();
    }

    /*========================================
     * 外部调用：点击卡片
     *========================================*/
    public void OnCardClicked(CardInfoStatic info, PlayerCard dyn)
    {
        // 1) 记录当前选中
        currentStatic = info;
        currentDyn = dyn;

        // 2) 立刻把数据塞进左侧 Info 面板


        // 3) 预加载升级 / 突破面板的数据（只是准备，不弹窗）
        upgradePanelCtrl.SetData(info, dyn);
        unitGiftLevel.SetData(info, dyn);
        unitGiftLevel.RefreshUI();

        // 4) 根据“是否已拥有”启用 / 禁用按钮
        bool owned = dyn != null;
        upgradeBtn.SetEnabled(owned);
        uptierBtn.SetEnabled(owned);
        giftBtn.SetEnabled(owned);
        SetInfoPanelData(info, dyn);

        RefreshActiveSkillUI(info);
        RefreshPassiveSkillUI(info, dyn);

        // 5) 如果 Info 子页当前正在显示，可能要根据新数据重新排版
        vhSizer?.Apply();
    }

    /// <summary>
    /// 把卡片静态 + 动态数据填进 Info 面板左侧 4 维和描述
    /// </summary>
    void SetInfoPanelData(CardInfoStatic info, PlayerCard dyn)
    {
        if (info == null) return;   // 安全兜底
        //Debug.Log($"[SetInfoPanelData] {info?.id}  atk={LevelStatCalculator.CalculateStats(info, dyn).Atk}");

        RefreshStars(dyn);

        /*── 1. 计算当前四维 ─────────────────────*/
        Stats4 stats = LevelStatCalculator.CalculateStats(info, dyn);
        var (equipAtk, equipDef) = CalcEquipBonus(dyn);   // ★ 新增这一行
        //stats.Atk += equipAtk;
        //stats.Def += equipDef;


        /*── 2. 抓 UI 节点（或换成已缓存字段）─────*/
        var root = GetComponent<UIDocument>().rootVisualElement;

        var atkLbl = root.Q<Label>("InfoAtkStatsLbl");
        var defLbl = root.Q<Label>("InfoDefStatLbl");
        var intLbl = root.Q<Label>("InfoIntStatLbl");
        var cmdLbl = root.Q<Label>("InfoCmdStatLbl");

        var descLbl = root.Q<Label>("HeroDescription");
        var cardLvLbl = root.Q<Label>("CardLvLbl");
        var herofragmentLbl = root.Q<Label>("HeroFragmentsLbl");
        atkLbl.enableRichText   = true;                 // 允许 <color> 标签
        atkLbl.style.whiteSpace = WhiteSpace.NoWrap;    // 防止数值换行

        /*── 3. 填数值 ───────────────────────────*/
        if (atkLbl != null)
        {
            atkLbl.text = $"{stats.Atk}+{equipAtk}";
        }
        if (defLbl != null)
            defLbl.text = $"{stats.Def}+{equipDef}";//  ()
        if (intLbl != null) intLbl.text = stats.Int.ToString();
        if (cmdLbl != null) cmdLbl.text = stats.Cmd.ToString();

        /*── 4. 填描述（字段名按你的静态库来改）────*/
        if (descLbl != null)
            descLbl.text = info.description;   // 若是 info.desc / info.flavor，改字段名

        if (cardLvLbl != null)
        {
            if (dyn != null)
                cardLvLbl.text = $"等级 {dyn.level}";
            else
                cardLvLbl.text = "未拥有";
        }

        if (herofragmentLbl != null)
        {
            // 显示当前碎片数量
            herofragmentLbl.text = $"武将碎片 x {dyn?.copies ?? 0}";
        }
        cardInv.RefreshEquipSlots(dyn, root);

    }



    /*========================================
     * 事件回调
     *========================================*/
    void OnCardChanged(string id)
    {
        // 只在左侧正展示这张卡时才刷新
        if (currentStatic != null && currentStatic.id == id)
        {
            // 取最新动态数据
            currentDyn = PlayerCardBankMgr.I.Data.Get(id);

            // 把静态 + 最新动态一起塞进去
            unitGiftLevel.SetData(currentStatic, currentDyn);

            // 刷新面板（如果 SetData 内部已自动刷新，可省掉这一行）
            unitGiftLevel.RefreshUI();
        }
    }

    void OnBankChanged(ResourceType type)
    {
        if (type == ResourceType.HeroMat2 || type == ResourceType.HeroExp)
            RefreshResource();
    }

    void RefreshResource()
    {
        mat2valueLbl.text = PlayerResourceBank.I[ResourceType.HeroMat2].ToString("N0");
        expvalueLbl.text = PlayerResourceBank.I[ResourceType.HeroExp].ToString("N0");
    }
    void RefreshStars(PlayerCard dyn)
    {
        int rank = Mathf.Clamp(dyn?.star ?? 0, 0, 5);
        //Debug.Log($"[RefreshStars] {currentStatic?.id} rank={rank}");

        for (int i = 0; i < 5; i++)
        {
            var slot = starSlots[i];
            if (slot == null) continue;

            bool filled = i < rank;
            slot.style.backgroundImage = new StyleBackground(
                filled ? cardInv.FilledStarSprite
                    : cardInv.EmptyStarSprite);
        }
    }

    void RefreshActiveSkillUI(CardInfoStatic info)
    {
        if (activeSkillDB == null || info == null) return;

        // 直接取静态表里的主动技能 ID
        var skill = activeSkillDB.Get(info.activeSkillId);
        if (skill == null) return;

        /* 图标 */
        mainSkillImg.style.backgroundImage = new StyleBackground(skill.iconSprite);

        /* 名称 */
        mainSkillNameLbl.text = skill.cnName;

        /* 描述 */
        mainSkillDescLbl.text = skill.description;
    }
    void RefreshPassiveSkillUI(CardInfoStatic info, PlayerCard dyn)
    {
        if (passiveSkillDB == null || info == null) return;

        // ▸ 被动 1
        ApplyPassive(
            info.passiveOneId,
            passive1Img, passive1NameLbl, passive1DescLbl,
            info, dyn);

        // ▸ 被动 2（可选）
        ApplyPassive(
            info.passiveTwoId,
            passive2Img, passive2NameLbl, passive2DescLbl,
            info, dyn);
    }

    void ApplyPassive(
        string id,
        VisualElement img, Label nameLbl, Label descLbl,
        CardInfoStatic info, PlayerCard dyn)
    {
        var ps = passiveSkillDB.Get(id);
        if (ps == null)
        {
            img.style.backgroundImage = null;
            nameLbl.text = "—";
            descLbl.text = "";
            return;
        }

        /* 1) 图标 */
        img.style.backgroundImage = new StyleBackground(ps.iconSprite);

        /* 2) 名称 */
        nameLbl.text = ps.cnName;

        /* 3) 描述（{X}→百分比） */
        float pct = SkillValueCalculator.CalcPercent(
                        ps.baseValue, info, dyn, passiveSkillDB);
        string desc = ps.description.Replace("{X}", pct.ToString("0.#"));
        descLbl.text = desc;
    }

    public void HandleSlotClick(CardInfoStatic info, PlayerCard dyn, EquipSlotType slot)
    {
        if (dyn == null)
        {
            PopupManager.Show("提示", "尚未拥有该武将");
            return;
        }
        gearPanel.Open(dyn, slot);   // 这里才有 gearPanel 引用
    }

    /// <summary>统计这张卡当前装备提供的额外 Atk / Def</summary>
    private (int atk, int def) CalcEquipBonus(PlayerCard dyn)
    {
        if (dyn == null) return (0, 0);

        int bonusAtk = 0, bonusDef = 0;

        string[] equipUuids =
        {
            dyn.equip.weaponUuid,
            dyn.equip.armorUuid,
            dyn.equip.accessoryUuid
        };

        foreach (var uuid in equipUuids)
        {
            if (string.IsNullOrEmpty(uuid)) continue;

            // PlayerGearBank.Get() 也是按 uuid 查
            var pg = playerGearBank.Get(uuid);
            if (pg == null) continue;

            var gs = gearDB.Get(pg.staticId);   // 取静态条目
            if (gs == null) continue;

            // ★ 直接用你已经写好的公式
            var (atkF, defF) = gs.CalcStats(pg.level);

            bonusAtk += Mathf.RoundToInt(atkF);
            bonusDef += Mathf.RoundToInt(defF);
        }

        return (bonusAtk, bonusDef);
    }




}


