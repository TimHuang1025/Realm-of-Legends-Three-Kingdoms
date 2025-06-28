// ────────────────────────────────────────────────────────────────────────────────
// 主公卡牌页面：等级 / 声望 / 技能加点面板 / 技能弹窗（技能环实时刷新）
// ────────────────────────────────────────────────────────────────────────────────
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Game;

[RequireComponent(typeof(UIDocument))]
public class PlayerCardPage : MonoBehaviour
{
    /*──────── Inspector ────────*/
    [Header("面板控制器")]
    [SerializeField] private SkillUpgradePanelController skillPanelCtrl;
    [SerializeField] private SkillDetailPopupController  skillPopupCtrl;

    [Header("数据")]
    [SerializeField] private LordCardStaticData   lordCardStatic;
    [SerializeField] private PlayerLordCard       playerLordCard;
    [SerializeField] private ActiveSkillDatabase  activeDB;
    [SerializeField] private PassiveSkillDatabase passiveDB;
    [SerializeField] private PlayerBaseController playerBaseController;

    [Header("UI Sprites")]
    [Tooltip("技能等级环：索引 0-3 → Lv1-Lv4")]
    [SerializeField] private Sprite[] ringSprites;

    /*──────── UI 组件 ────────*/
    Label playerLevelLbl, nextLevelLbl,
          nextLevelFameLbl, nextLvAddSkillLbl,
          totalSkillPtsLbl,
          atkStatLbl, defStatLbl, iqStatLbl,
          atkPtsAddedLbl, defPtsAddedLbl, iqPtsAddedLbl,
          fameValueLbl;
    Button upgradeLvBtn;

    VisualElement activeImg,  passive1Img,  passive2Img;
    VisualElement activeRing, passive1Ring, passive2Ring;

    /*──────── 其它 ────────*/
    List<Button> addBtns;
    CardInfoStatic currentStatic;
    PlayerCard    currentDyn;

    /*──────────────── 生命周期 ─────────────────*/
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        /*── 基础节点 ──*/
        playerLevelLbl   = root.Q<Label>("PlayerLevelLabel");
        nextLevelLbl     = root.Q<Label>("NextLevelLabel");
        nextLevelFameLbl = root.Q<Label>("NextLevelFameRequire");
        nextLvAddSkillLbl= root.Q<Label>("NextLvAddSkillPts");
        totalSkillPtsLbl = root.Q<Label>("TotalSkillPts");

        atkStatLbl       = root.Q<Label>("AtkStat");
        defStatLbl       = root.Q<Label>("DefStat");
        iqStatLbl        = root.Q<Label>("IQStat");

        atkPtsAddedLbl   = root.Q<Label>("AtkSkillPtsAdded");
        defPtsAddedLbl   = root.Q<Label>("DefSkillPtsAdded");
        iqPtsAddedLbl    = root.Q<Label>("IQSkillPtsAdded");

        fameValueLbl     = root.Q<Label>("FameValue");

        /*── 升级主公按钮 ──*/
        upgradeLvBtn = root.Q<Button>("UpgradeLvBtn");
        if (upgradeLvBtn != null) upgradeLvBtn.clicked += OnUpgradeLvClicked;

        /*── 技能图标 & 环 ──*/
        activeImg    = root.Q<VisualElement>("MainSkillImage");
        passive1Img  = root.Q<VisualElement>("Passive1Image");
        passive2Img  = root.Q<VisualElement>("Passive2Image");

        activeRing   = root.Q<VisualElement>("MainSkillRing");
        passive1Ring = root.Q<VisualElement>("Passive1Ring");
        passive2Ring = root.Q<VisualElement>("Passive2Ring");

        if (activeImg   != null) activeImg.RegisterCallback<ClickEvent>(_ => skillPopupCtrl?.Open(SkillSlot.Active));
        if (passive1Img != null) passive1Img.RegisterCallback<ClickEvent>(_ => skillPopupCtrl?.Open(SkillSlot.Passive1));
        if (passive2Img != null) passive2Img.RegisterCallback<ClickEvent>(_ => skillPopupCtrl?.Open(SkillSlot.Passive2));

        /*── “+”按钮 → 打开加点面板 ──*/
        addBtns = root.Query<Button>(className: "addBtn").ToList();
        foreach (var btn in addBtns)
            btn.RegisterCallback<ClickEvent>(_ => StartCoroutine(OpenSkillPanel()));

        root.Q<Button>("ReturnBtn")?.RegisterCallback<ClickEvent>(_ =>
            playerBaseController?.HidePlayerCardUpgradePage());

        /*── 事件监听 ──*/
        if (skillPanelCtrl != null)
            skillPanelCtrl.onConfirm += RefreshAll;

        if (skillPopupCtrl != null)
            skillPopupCtrl.onUpgrade += RefreshAll;

        RefreshAll();
    }

    void OnDisable()
    {
        if (upgradeLvBtn != null) upgradeLvBtn.clicked -= OnUpgradeLvClicked;

        if (addBtns != null)
            foreach (var btn in addBtns)
                btn.UnregisterCallback<ClickEvent>(_ => StartCoroutine(OpenSkillPanel()));

        if (skillPanelCtrl != null)
            skillPanelCtrl.onConfirm -= RefreshAll;

        if (skillPopupCtrl != null)
            skillPopupCtrl.onUpgrade -= RefreshAll;
    }

    /*──────────────── 升级主公 ─────────────────*/
    void OnUpgradeLvClicked()
    {
        bool ok = playerLordCard.TryLevelUpWithFame(lordCardStatic);
        Debug.Log(ok ? "升级成功！" : "声望不足或已满级");
        RefreshAll();
    }

    /*──────────────── 主刷新 ─────────────────*/
    void RefreshAll()
    {
        if (lordCardStatic == null || playerLordCard == null) return;

        /*── 1. 等级 / 声望 / 属性 ──*/
        int lv  = playerLordCard.currentLevel;
        var cur = lordCardStatic.GetLevel(lv);
        var nxt = lordCardStatic.GetLevel(lv + 1);

        playerLevelLbl.text     = $"Lv{lv} {cur?.title ?? ""}";
        nextLevelLbl.text       = nxt == null ? "已达最高等级" : $"下一等级: {nxt.title}";
        nextLevelFameLbl.text   = nxt == null ? "--" : nxt.requiredFame.ToString();
        nextLvAddSkillLbl.text  = nxt == null ? "+0" : $"+{nxt.skillPoints}";
        totalSkillPtsLbl.text   = playerLordCard.totalSkillPointsEarned.ToString();

        atkStatLbl.text = playerLordCard.atk.ToString();
        defStatLbl.text = playerLordCard.def.ToString();
        iqStatLbl.text  = playerLordCard.iq.ToString();

        atkPtsAddedLbl.text = playerLordCard.atkPointsUsed.ToString();
        defPtsAddedLbl.text = playerLordCard.defPointsUsed.ToString();
        iqPtsAddedLbl.text  = playerLordCard.iqPointsUsed.ToString();

        fameValueLbl.text = PlayerResourceBank.I != null
                                ? PlayerResourceBank.I[ResourceType.Fame].ToString()
                                : "0";

        /*── 2. 技能图标 + 环 ──*/
        ApplySkillUI(activeImg  , activeRing , activeDB , playerLordCard.activeSkillId , playerLordCard.activeSkillLv );
        ApplySkillUI(passive1Img, passive1Ring, passiveDB, playerLordCard.passiveOneId, playerLordCard.passiveOneLv);
        ApplySkillUI(passive2Img, passive2Ring, passiveDB, playerLordCard.passiveTwoId, playerLordCard.passiveTwoLv);
    }

    /*──────── 设置图标 & 环 ────────*/
    void ApplySkillUI(VisualElement iconVE, VisualElement ringVE,
                      ScriptableObject db, string id, int lv)
    {
        if (iconVE == null || ringVE == null || db == null || string.IsNullOrEmpty(id))
            return;

        // 图标
        Sprite icon = null;
        switch (db)
        {
            case ActiveSkillDatabase adb:
                icon = adb.Get(id)?.iconSprite;
                break;
            case PassiveSkillDatabase pdb:
                icon = pdb.Get(id)?.iconSprite;
                break;
        }
        if (icon != null)
            iconVE.style.backgroundImage = new StyleBackground(icon);

        // 等级环：索引 = Lv-1
        if (ringSprites != null && ringSprites.Length >= 4)
        {
            int idx = Mathf.Clamp(lv - 1, 0, ringSprites.Length - 1);
            ringVE.style.backgroundImage = new StyleBackground(ringSprites[idx]);
        }
    }

    /*──────────────── 打开加点面板 ─────────────────*/
    IEnumerator OpenSkillPanel()
    {
        if (skillPanelCtrl != null)
            skillPanelCtrl.Open(currentStatic, currentDyn);
        yield return null;
    }

    /* 外部：卡牌切换回调 */
    public void OnCardClicked(CardInfoStatic info, PlayerCard dyn)
    {
        currentStatic = info;
        currentDyn    = dyn;
    }
}
