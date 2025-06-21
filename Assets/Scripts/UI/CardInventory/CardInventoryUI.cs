using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Core;
using Game.Data;
using Game.Utils;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class CardInventoryUI : MonoBehaviour
{
    /*──────── Inspector 引用 ────────*/
    [Header("面板控制器")]

    [SerializeField] UptierPanelController uptierPanelCtrl;
    [SerializeField] private GearSelectionPanel gearPanel;
    [SerializeField] private HorseSelectionPanel horsePanel;

    [SerializeField] private PlayerGearBank playerGearBank;   // 玩家所有动态装备
    [SerializeField] private GearDatabaseStatic gearDB;       // 静态装备库
    [SerializeField] private PlayerHorseBank horseBank;
    [SerializeField] private HorseDatabaseStatic horseDB;

    


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
    // ── 私有 UI 元素 ────────────────────────────────
    Button weaponRemoveBtn, armorRemoveBtn, mountRemoveBtn;
    VisualElement atkTip;        // 当前悬浮气泡（null = 没弹）
    bool atkTipHooked;


    /*──────── 私有 UI 元素 ────────*/
    VisualElement cardsVe, infoVe;
    Button returnBtn, returnBtnforInfo, upgradeBtn, uptierBtn, giftBtn, infoBtn, closeInfoBtn, gachaBtn;
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
        BindStatButton(root.Q<Button>("AtkStatBtn"), StatType.Atk);
        BindStatButton(root.Q<Button>("DefStatBtn"), StatType.Def);
        BindStatButton(root.Q<Button>("IntStatBtn"), StatType.Int);
        BindStatButton(root.Q<Button>("CmdStatBtn"), StatType.Cmd);

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
        returnBtnforInfo = root.Q<Button>("ReturnBtnForInfo");
        upgradeBtn = root.Q<Button>("InfoUpgradeBtn");
        uptierBtn = root.Q<Button>("InfoUptierBtn");
        giftBtn = root.Q<Button>("InfoGiftBtn");
        infoBtn = root.Q<Button>("InfoBtn");
        closeInfoBtn = root.Q<Button>("ClosePanelForInfo");
        gachaBtn = root.Q<Button>("GachaBtn");

        var weaponL = root.Q<Button>("weaponslot");
        var armorL = root.Q<Button>("armorslot");
        var mountL = root.Q<Button>("horseslot");
        //卸下按钮
        weaponRemoveBtn = root.Q<Button>("WeaponRemoveBtn");
        armorRemoveBtn = root.Q<Button>("ArmorRemoveBtn");
        mountRemoveBtn = root.Q<Button>("MountRemoveBtn");


        weaponL?.RegisterCallback<ClickEvent>(evt =>
        {
            if (!(bool)weaponL.userData)
            {
                PopupManager.Show("提示", "武器槽未解锁");
                evt.StopPropagation();
                return;
            }
            HandleSlotClick(currentStatic, currentDyn, EquipSlotType.Weapon);
        });

        armorL?.RegisterCallback<ClickEvent>(evt =>
        {
            if (!(bool)armorL.userData)
            {
                PopupManager.Show("提示", "防具槽未解锁");
                evt.StopPropagation();
                return;
            }
            HandleSlotClick(currentStatic, currentDyn, EquipSlotType.Armor);
        });

        mountL?.RegisterCallback<ClickEvent>(evt =>
        {
            if (!(bool)mountL.userData)
            {
                PopupManager.Show("提示", "坐骑槽未解锁");
                evt.StopPropagation();
                return;
            }
            HandleSlotClick(currentStatic, currentDyn, EquipSlotType.Mount); // ★
        });


        weaponRemoveBtn?.RegisterCallback<ClickEvent>(evt =>
        {
            AttemptUnequip(EquipSlotType.Weapon);
            evt.StopImmediatePropagation();     // 关键
        }, TrickleDown.TrickleDown);
        armorRemoveBtn?.RegisterCallback<ClickEvent>(evt =>
        {
            AttemptUnequip(EquipSlotType.Armor);   // 复用同一个卸下函数
            evt.StopImmediatePropagation();        // 关键：截断捕获 + 冒泡
        }, TrickleDown.TrickleDown);

        mountRemoveBtn?.RegisterCallback<ClickEvent>(evt =>
        {
            AttemptUnequip(EquipSlotType.Mount);
            evt.StopImmediatePropagation();
        }, TrickleDown.TrickleDown);

        returnBtn?.RegisterCallback<ClickEvent>(_ => playerBaseController.HideCardInventoryPage());
        upgradeBtn?.RegisterCallback<ClickEvent>(_ => StartCoroutine(OpenPanel(upgradePanelCtrl)));
        uptierBtn?.RegisterCallback<ClickEvent>(_ => StartCoroutine(OpenPanel(uptierPanelCtrl)));
        giftBtn?.RegisterCallback<ClickEvent>(_ => StartCoroutine(OpenPanel(giftPanelCtrl)));
        infoBtn?.RegisterCallback<ClickEvent>(_ => OpenInfoPanel());
        returnBtnforInfo?.RegisterCallback<ClickEvent>(_ => CloseInfoPanel());
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

        StatBreakdownPanel.OnPanelShown = () => vhSizer?.Apply();
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
        PlayerCardBankMgr.I.onCardUpdated -= OnCardUpdated;
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
        returnBtnforInfo.style.display = DisplayStyle.Flex;
        unitGiftLevel.RefreshUI();
        vhSizer?.Apply();

    }
    void CloseInfoPanel()
    {
        infoVe.style.display = DisplayStyle.None;
        cardsVe.style.display = DisplayStyle.Flex;
        returnBtnforInfo.style.display = DisplayStyle.None;
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
        if (info == null) return;

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
        atkLbl.enableRichText = true;                 // 允许 <color> 标签
        atkLbl.style.whiteSpace = WhiteSpace.NoWrap;    // 防止数值换行

        /*── 3. 填数值 ───────────────────────────*/
        if (atkLbl != null)
        {

            int totalAtk = stats.Atk + equipAtk;          // ① 求和
            atkLbl.text = totalAtk.ToString();
        }
        if (defLbl != null)
        {
            int totalDef = stats.Atk + equipDef;
            defLbl.text = totalDef.ToString();
        }
        if (intLbl != null)
        {
            //int totalLbl = stats.Atk + equipInt;
            intLbl.text = stats.Int.ToString();
        }
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
        long mat2 = PlayerResourceBank.I[ResourceType.HeroMat2];
        long exp = PlayerResourceBank.I[ResourceType.HeroExp];

        // 1 行搞定数字缩写（默认保留 1 位小数，可传第二个参数改小数位）
        mat2valueLbl.text = NumberAbbreviator.Format(mat2, 2); // 例：23.0K
        expvalueLbl.text = NumberAbbreviator.Format(exp, 2);
    }
    void RefreshStars(PlayerCard dyn)
    {
        /* 1) 当前星级条目 */
        int star = dyn?.star ?? 0;
        var rule = CardDatabaseStatic.Instance.GetStar(star);

        /* 2) 点亮数量 & 亮星贴图 */
        int    lit       = rule != null ? rule.starsInFrame               : 0;
        string colorKey  = rule != null ? rule.frameColor.ToLowerInvariant() : "blue";

        Sprite litSprite = colorKey switch
        {
            "purple" => cardInv.purpleStarSprite,
            "gold"   => cardInv.goldStarSprite,
            "blue"   => cardInv.blueStarSprite,
            _        => cardInv.blueStarSprite
        };

        /* 3) 应用到 5 槽 */
        for (int i = 0; i < 5; i++)
        {
            var slot = starSlots[i];
            if (slot == null) continue;

            bool filled = i < lit;
            slot.style.backgroundImage = new StyleBackground(
                filled ? litSprite : cardInv.emptyStarSprite);
        }
    }

    /*───────────────────────────────────────────────*/
    /* 主动技能 ─ RefreshActiveSkillUI               */
    /*───────────────────────────────────────────────*/
    void RefreshActiveSkillUI(CardInfoStatic info)
    {
        if (activeSkillDB == null || info == null) return;
        /*──────── 调试输出 ────────*/
        int star = currentDyn?.star ?? 0;

        // ① 直接读 Star Table 条目
        var starRule = Resources
            .Load<CardDatabaseStatic>("CardDatabaseStatic")
            .GetStar(star);

        //Debug.Log($"★{star} skillLvGain = [{string.Join(",", starRule.skillLvGain)}]");

        // ② 主动技能 idx = 0
        int activeLv = SkillLevelHelper.GetSkillLevel(star, 0);
        //Debug.Log($"主动技能绝对等级 = {activeLv}");
        /*──────── 调试结束 ────────*/

        var skill = activeSkillDB.Get(info.activeSkillId);
        if (skill == null) return;

        /* 图标 & 名称 */
        mainSkillImg.style.backgroundImage = new StyleBackground(skill.iconSprite);
        mainSkillNameLbl.text = skill.cnName;

        /* 主动技能绝对等级：idx = 0 */
        int skillLv = SkillLevelHelper.GetSkillLevel(currentDyn?.star ?? 0, 0);

        /* 等级倍率：从 ActiveSkillDB.LevelMultiplier 里查 */
        var lvDict = activeSkillDB.LevelMultiplier;
        float lvMul = lvDict != null && lvDict.TryGetValue(skillLv, out var m) ? m : 1f;

        /* 百分比 */
        float pct = SkillValueCalculator.CalcPercent(
                        skill.coefficient,   // baseValue
                        info,                // 用于品阶倍率
                        lvMul);              // 等级倍率

        /* 描述 */
        mainSkillDescLbl.text = skill.description.Replace("{X}", pct.ToString("0.#"));
    }

    /*───────────────────────────────────────────────*/
    /* 被动技能整体刷新 ─ RefreshPassiveSkillUI       */
    /*───────────────────────────────────────────────*/
    void RefreshPassiveSkillUI(CardInfoStatic info, PlayerCard dyn)
    {
        if (passiveSkillDB == null || info == null) return;

        /* 被动 1 */
        ApplyPassive(info.passiveOneId,
                    passive1Img, passive1NameLbl, passive1DescLbl,
                    info, dyn, 1);

        /* 被动 2 */
        ApplyPassive(info.passiveTwoId,
                    passive2Img, passive2NameLbl, passive2DescLbl,
                    info, dyn, 2);
    }

    /*───────────────────────────────────────────────*/
    /* 单个被动技能刷新 ─ ApplyPassive                */
    /*───────────────────────────────────────────────*/
    void ApplyPassive(
        string          id,
        VisualElement   img,
        Label           nameLbl,
        Label           descLbl,
        CardInfoStatic  info,
        PlayerCard      dyn,
        int             skillIdx)     // 1 = 被动1, 2 = 被动2
    {
        var ps = passiveSkillDB.Get(id);
        if (ps == null)
        {
            img.style.backgroundImage = null;
            nameLbl.text = "—";
            descLbl.text = "";
            return;
        }

        /* 图标 & 名称 */
        img.style.backgroundImage = new StyleBackground(ps.iconSprite);
        nameLbl.text = ps.cnName;

        /* 绝对技能等级 */
        int   skillLv = SkillLevelHelper.GetSkillLevel(dyn?.star ?? 0, skillIdx);

        /* 等级倍率 */
        var  prov   = (ISkillMultiplierProvider)passiveSkillDB;
        var  lvDict = prov.LevelMultiplier;
        float lvMul = lvDict != null && lvDict.TryGetValue(skillLv, out var m) ? m : 1f;

        /* 百分比 */
        float pct = SkillValueCalculator.CalcPercent(
                        ps.baseValue,
                        info,
                        lvMul);

        /* 描述 */
        descLbl.text = ps.description.Replace("{X}", pct.ToString("0.#"));
    }



    public void HandleSlotClick(CardInfoStatic info, PlayerCard dyn, EquipSlotType slot)
    {
        if (dyn == null)
        {
            PopupManager.Show("提示", "尚未拥有该武将");
            return;
        }

        if (slot == EquipSlotType.Mount)           // ★ 坐骑槽
        {
            horsePanel.Open(dyn);                  //   → 打开 HorseSelectionPanel
        }
        else                                        // ★ 武器 / 防具槽
        {
            gearPanel.Open(dyn, slot);             //   → 继续用 GearSelectionPanel
        }
    }


    /// <summary>统计这张卡当前装备提供的额外 Atk / Def</summary>
    /// <summary>
    /// 统计当前装备提供的额外 Atk / Def（含战马百分比折算）
    /// </summary>
    private (int atk, int def) CalcEquipBonus(PlayerCard dyn)
    {
        if (dyn == null) return (0, 0);

        int bonusAtk = 0, bonusDef = 0;

        /*──────── 武器 / 防具：固定数值 ────────*/
        string[] gearUuids = { dyn.equip.weaponUuid, dyn.equip.armorUuid };

        foreach (var uuid in gearUuids)
        {
            if (string.IsNullOrEmpty(uuid)) continue;

            var pg = playerGearBank.Get(uuid);
            if (pg == null) continue;

            var gs = gearDB.Get(pg.staticId);
            if (gs == null) continue;

            var (atkF, defF) = gs.CalcStats(pg.level);  // 固定值
            bonusAtk += Mathf.RoundToInt(atkF);
            bonusDef += Mathf.RoundToInt(defF);
        }

        /*──────── 坐骑：百分比 → 实际数值 ────────
        string horseUuid = dyn.equip.accessoryUuid;
        if (!string.IsNullOrEmpty(horseUuid))
        {
            var ph = horseBank.Get(horseUuid);
            var hs = horseDB.Get(ph?.staticId);
            if (hs != null)
            {
                var (atkPct, defPct, _) = hs.CalcStats(ph.level);  // 0.1536 = +15.36 %

                // 先算武将不含装备的基础四维
                Stats4 baseStats = LevelStatCalculator.CalculateStats(currentStatic, dyn);

                bonusAtk += Mathf.RoundToInt(baseStats.Atk * atkPct);
                bonusDef += Mathf.RoundToInt(baseStats.Def * defPct);
            }
        }*/

        return (bonusAtk, bonusDef);
    }


    /// <summary>点击小 X 时调用；若该槽有装备就卸下</summary>
    void AttemptUnequip(EquipSlotType slot)
    {
        if (currentDyn == null)
        {
            PopupManager.Show("提示", "尚未拥有该武将");
            return;
        }

        // 1) 取得 uuid
        string uuid = slot switch
        {
            EquipSlotType.Weapon => currentDyn.equip.weaponUuid,
            EquipSlotType.Armor => currentDyn.equip.armorUuid,
            EquipSlotType.Mount => currentDyn.equip.accessoryUuid,
            _ => ""
        };

        if (string.IsNullOrEmpty(uuid))
        {
            PopupManager.Show("提示", "当前槽位没有装备");
            return;
        }

        // 2) 取出 PlayerGear，清掉 equip 关系
        var pg = playerGearBank.Get(uuid);
        if (pg != null) pg.equippedById = "";

        switch (slot)                 // 清武将身上的 uuid
        {
            case EquipSlotType.Weapon: currentDyn.equip.weaponUuid = ""; break;
            case EquipSlotType.Armor: currentDyn.equip.armorUuid = ""; break;
        }
        if (slot == EquipSlotType.Mount)
        {
            var ph = horseBank.Get(uuid);
            if (ph != null) { ph.equippedById = ""; horseBank.MarkDirty(uuid); }
            currentDyn.equip.accessoryUuid = "";
        }
        else
        {
            if (pg != null) { pg.equippedById = ""; playerGearBank.MarkDirty(uuid); }

            if (slot == EquipSlotType.Weapon) currentDyn.equip.weaponUuid = "";
            if (slot == EquipSlotType.Armor) currentDyn.equip.armorUuid = "";
        }


        // 3) 保存并刷新 UI
        PlayerCardBankMgr.I.MarkDirty(currentDyn.id);   // ← 你的存档/脏标记逻辑
        RefreshAfterEquipChanged();

        PopupManager.Show("提示", "已卸下装备");
    }

    /// <summary>卸下/换装后集中刷新界面</summary>
    void RefreshAfterEquipChanged()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        cardInv.RefreshEquipSlots(currentDyn, root);        // 刷 3 个槽
        SetInfoPanelData(currentStatic, currentDyn);        // 刷 4 维
        unitGiftLevel.SetData(currentStatic, currentDyn);   // 奖励
        unitGiftLevel.RefreshUI();

        vhSizer?.Apply();                                   // 重新排版
    }

    void BindStatButton(Button btn, StatType type)
    {
        if (btn == null) return;

        btn.pickingMode = PickingMode.Position;
        btn.userData = type;                                    // 存标识
        btn.RegisterCallback<ClickEvent>(OnStatClicked,            // 共用回调
                                        TrickleDown.TrickleDown);
    }
    enum StatType { Atk, Def, Int, Cmd }
    void OnStatClicked(ClickEvent evt)
    {
        if (currentStatic == null) return;

        var btn = (VisualElement)evt.currentTarget;
        var type = (StatType)btn.userData;                         // 取标识

        // ① 基础 & 装备
        var stats = LevelStatCalculator.CalculateStats(currentStatic, currentDyn);
        int baseVal = type switch
        {
            StatType.Atk => stats.Atk,
            StatType.Def => stats.Def,
            StatType.Int => stats.Int,
            StatType.Cmd => stats.Cmd,
            _ => 0
        };

        var (equipAtk, equipDef) = CalcEquipBonus(currentDyn);
        int equipVal = type switch
        {
            StatType.Atk => equipAtk,
            StatType.Def => equipDef,
            _ => 0          // Int/Cmd 暂无装备加成
        };

        // ② 战马 & Buff（仅 Atk/Def 有战马；Buff 按需实现）
        int horseVal = CalcHorseBonus(currentDyn, type); 
        int buffVal = CalcBuffBonus(type);     // 如果没有 Buff 系统可直接返回 0

        // ③ 组织列表
        var parts = new List<(string, int)>
        {
            ("基础",  baseVal)
            
        };
        if (equipVal != 0) parts.Add(("装备", equipVal));
        if (horseVal != 0) parts.Add(("战马", horseVal));
        if (buffVal != 0) parts.Add(("Buff", buffVal));

        string title = type switch
        {
            StatType.Atk => "攻击组成",
            StatType.Def => "防御组成",
            StatType.Int => "谋略组成",
            StatType.Cmd => "统率组成",
            _ => "属性组成"
        };

        // ④ 弹窗
        StatBreakdownPanel.Show(title, parts, evt.position);
    }
    
    int CalcHorseBonus(PlayerCard dyn, StatType type)
    {
        if (dyn == null) return 0;

        string uuid = dyn.equip.accessoryUuid;
        if (string.IsNullOrEmpty(uuid)) return 0;

        var ph = horseBank.Get(uuid);
        var hs = horseDB.Get(ph?.staticId);
        if (hs == null) return 0;

        // 假设 CalcStats 现在返回 (atk%, def%, int%)
        var (atkPct, defPct, intPct) = hs.CalcStats(ph.level);

        var stats = LevelStatCalculator.CalculateStats(currentStatic, dyn);

        return type switch
        {
            StatType.Atk => Mathf.RoundToInt(stats.Atk * atkPct),
            StatType.Def => Mathf.RoundToInt(stats.Def * defPct),
            StatType.Int => Mathf.RoundToInt(stats.Int * intPct),  // ← 只这行生效
            _            => 0
        };
    }


    int CalcBuffBonus(StatType type)
    {
        // 还没做 Buff 系统 → 直接返回 0
        return 0;
    }



}


