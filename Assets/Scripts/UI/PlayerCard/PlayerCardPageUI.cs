// ────────────────────────────────────────────────────────────────────────────────
// 玩家卡牌页面 UI 逻辑
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
    /*──────── Inspector 引用 ────────*/
    [Header("面板控制器")]
    [SerializeField] private SkillUpgradePanelController skillPanelCtrl;

    [Header("数据")]
    [SerializeField] private LordCardStaticData lordCardStatic;   // Static SO
    [SerializeField] private PlayerLordCard playerLordCard;    // Player SO
    [SerializeField] private PlayerBaseController playerBaseController;

    /*──────── UI 元素 ────────*/
    private Label playerLevelLbl, nextLevelLbl;
    private Label nextLevelFameLbl, nextLvAddSkillLbl;
    private Label totalSkillPtsLbl;
    private Label atkStatLbl, defStatLbl, iqStatLbl;
    private Label atkPtsAddedLbl, defPtsAddedLbl, iqPtsAddedLbl;
    private Label fameValueLbl;           // 当前声望
    private Button upgradeLvBtn;

    /*──────── 其它 ────────*/
    private List<Button> addBtns;
    private CardInfoStatic currentStatic;
    private PlayerCard currentDyn;

    /*──────────────────────── 生命周期 ────────────────────────*/
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // 1⃣  获取所有标签 / 按钮
        playerLevelLbl = root.Q<Label>("PlayerLevelLabel");
        nextLevelLbl = root.Q<Label>("NextLevelLabel");
        nextLevelFameLbl = root.Q<Label>("NextLevelFameRequire");
        nextLvAddSkillLbl = root.Q<Label>("NextLvAddSkillPts");
        totalSkillPtsLbl = root.Q<Label>("TotalSkillPts");

        atkStatLbl = root.Q<Label>("AtkStat");
        defStatLbl = root.Q<Label>("DefStat");
        iqStatLbl = root.Q<Label>("IQStat");

        atkPtsAddedLbl = root.Q<Label>("AtkSkillPtsAdded");
        defPtsAddedLbl = root.Q<Label>("DefSkillPtsAdded");
        iqPtsAddedLbl = root.Q<Label>("IQSkillPtsAdded");

        fameValueLbl = root.Q<Label>("FameValue");            // ⬅️ 统一大小写

        upgradeLvBtn = root.Q<Button>("UpgradeLvBtn");
        if (upgradeLvBtn != null) upgradeLvBtn.clicked += OnUpgradeLvClicked;

        // 2⃣  “+”按钮
        addBtns = root.Query<Button>(className: "addBtn").ToList();
        foreach (var btn in addBtns)
            btn.RegisterCallback<ClickEvent>(_ => StartCoroutine(OpenSkillPanel()));

        root.Q<Button>("ReturnBtn")?.RegisterCallback<ClickEvent>(_ =>
            playerBaseController?.HidePlayerCardUpgradePage());

        // ★ 监听 onConfirm：加点确认后刷新
        if (skillPanelCtrl != null)
            skillPanelCtrl.onConfirm += RefreshAll;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (upgradeLvBtn != null) upgradeLvBtn.clicked -= OnUpgradeLvClicked;

        if (addBtns != null)
            foreach (var btn in addBtns)
                btn.UnregisterCallback<ClickEvent>(_ => StartCoroutine(OpenSkillPanel()));

        // ★ 解绑 onConfirm
        if (skillPanelCtrl != null)
            skillPanelCtrl.onConfirm -= RefreshAll;
    }

    /*──────────────────────── 升级按钮 ────────────────────────*/
    private void OnUpgradeLvClicked()
    {
        bool ok = playerLordCard.TryLevelUpWithFame(lordCardStatic);
        Debug.Log(ok ? "升级成功！" : "声望不足或已满级 " + PlayerResourceBank.I[ResourceType.Fame]);
        RefreshAll();
    }

    /*──────────────────────── 公共刷新 ────────────────────────*/
    private void RefreshAll()
    {
        if (lordCardStatic == null || playerLordCard == null) return;

        int lv = playerLordCard.currentLevel;
        var cur = lordCardStatic.GetLevel(lv);
        var nxt = lordCardStatic.GetLevel(lv + 1);

        /* 当前等级 & 称号 */
        if (playerLevelLbl != null) playerLevelLbl.text = $"Lv{lv} {cur?.title ?? ""}";
        if (nextLevelLbl != null) nextLevelLbl.text = nxt == null ? "已达最高等级" : $"下一等级: {nxt.title}";

        /* 升级需求 & 奖励 */
        if (nextLevelFameLbl != null) nextLevelFameLbl.text = nxt == null ? "--" : nxt.requiredFame.ToString();
        if (nextLvAddSkillLbl != null) nextLvAddSkillLbl.text = nxt == null ? "+0" : $"+{nxt.skillPoints}";

        /* 技能点统计 */
        if (totalSkillPtsLbl != null) totalSkillPtsLbl.text = playerLordCard.totalSkillPointsEarned.ToString();

        /* 属性值 */
        if (atkStatLbl != null) atkStatLbl.text = playerLordCard.atk.ToString();
        if (defStatLbl != null) defStatLbl.text = playerLordCard.def.ToString();
        if (iqStatLbl != null) iqStatLbl.text = playerLordCard.iq.ToString();

        /* 已投技能点 */
        if (atkPtsAddedLbl != null) atkPtsAddedLbl.text = playerLordCard.atkPointsUsed.ToString();
        if (defPtsAddedLbl != null) defPtsAddedLbl.text = playerLordCard.defPointsUsed.ToString();
        if (iqPtsAddedLbl != null) iqPtsAddedLbl.text = playerLordCard.iqPointsUsed.ToString();

        /* 当前声望 */
        if (fameValueLbl != null && PlayerResourceBank.I != null)
            fameValueLbl.text = PlayerResourceBank.I[ResourceType.Fame].ToString();
    }

    /*──────────────────────── 技能面板 ────────────────────────*/
    private IEnumerator OpenSkillPanel()
    {
        if (skillPanelCtrl != null)
            skillPanelCtrl.Open(currentStatic, currentDyn);
        yield return null;
    }

    /* 供外部调用：更新当前选中的卡 */
    public void OnCardClicked(CardInfoStatic info, PlayerCard dyn)
    {
        currentStatic = info;
        currentDyn = dyn;
    }
    
}
