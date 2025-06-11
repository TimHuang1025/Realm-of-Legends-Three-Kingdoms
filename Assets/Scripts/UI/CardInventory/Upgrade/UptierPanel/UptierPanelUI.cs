// Assets/Scripts/Game/UI/UptierPanelController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Core;
using Game.Data;

[RequireComponent(typeof(UIDocument))]
public class UptierPanelController : MonoBehaviour, IUIPanelController
{
    /*──────── 数据 ────────*/
    CardInfoStatic info;
    PlayerCard     dyn;

    [SerializeField] VhSizer vhSizer;

    [Header("星星贴图")]
    [SerializeField] Sprite emptyStar;
    [SerializeField] Sprite blueStar;
    [SerializeField] Sprite purpleStar;
    [SerializeField] Sprite goldStar;

    [Header("数据库引用")]
    [SerializeField] ActiveSkillDatabase  activeSkillDB;
    [SerializeField] PassiveSkillDatabase passiveSkillDB;
    
    readonly Color okColor  = new (0.32f, 0.65f, 0.53f, 1f); // #51A687
    readonly Color errColor = Color.red;


    /*── 单例资产 ─*/
    static PlayerResources RES => _res ??= Resources.Load<PlayerResources>("PlayerResources");
    static PlayerResources     _res;
    static CardDatabaseStatic  DB  => _db  ??= Resources.Load<CardDatabaseStatic>("CardDatabaseStatic");
    static CardDatabaseStatic  _db;

    /*──────── UI 节点 ────────*/
    UIDocument doc;
    Label copiesHaveLbl, copiesNeedLbl;
    Button rankUpBtn;

    Label[] lblTitle     = new Label[3];   // 技能名称
    Label[] lblBeforeLv  = new Label[3];   // 当前等级
    Label[] lblAfterLv   = new Label[3];   // 升星后等级

    VisualElement[] curStars  = new VisualElement[5];
    VisualElement[] nextStars = new VisualElement[5];

    Label powerNowLbl;   // 当前战力
    Label powerNextLbl;  // 下一星战力

    /*──────── 生命周期 ────────*/
    void Awake()
    {
        doc = GetComponent<UIDocument>();
        gameObject.SetActive(false);
    }
    void OnEnable()
    {
        CacheNodes();                     // 每次都重新抓节点
        StartCoroutine(AfterEnable());
    }
    IEnumerator AfterEnable() { yield return null; vhSizer?.Apply(); }

    void OnDisable()
    {
        if (rankUpBtn != null)
            rankUpBtn.clicked -= OnRankUpClicked;
    }

    /*──────── IUIPanelController ────────*/
    public void Open(CardInfoStatic stat, PlayerCard dynInfo)
    {
        info = stat;
        dyn  = dynInfo;
        gameObject.SetActive(true);
        RefreshUI();
    }
    public void Close() => gameObject.SetActive(false);

    /*──────── 节点缓存 & 事件 ────────*/
    void CacheNodes()
    {
        var root = doc.rootVisualElement;

        copiesHaveLbl = root.Q<Label>("PlayerMaterial1Num");
        copiesNeedLbl = root.Q<Label>("UpgradeMaterial1Num");
        powerNowLbl   = root.Q<Label>("PowerSlotStat");
        powerNextLbl  = root.Q<Label>("AfterUptierPower");
        rankUpBtn = root.Q<Button>("RankUp");

        lblTitle[0]    = root.Q<Label>("BeforeUptierSlotTitle1");
        lblTitle[1]    = root.Q<Label>("BeforeUptierSlotTitle2");
        lblTitle[2]    = root.Q<Label>("BeforeUptierSlotTitle3");

        lblBeforeLv[0] = root.Q<Label>("BeforeUptierSlotStat1");
        lblBeforeLv[1] = root.Q<Label>("BeforeUptierSlotStat2");
        lblBeforeLv[2] = root.Q<Label>("BeforeUptierSlotStat3");

        lblAfterLv[0]  = root.Q<Label>("AfterUptierSlotTitle1");
        lblAfterLv[1]  = root.Q<Label>("AfterUptierSlotTitle2");
        lblAfterLv[2]  = root.Q<Label>("AfterUptierSlotTitle3");

        /* ─ 星星行 ─ */
        var rowBefore  = root.Q<VisualElement>("StarLevelBefore");
        var rowAfter   = root.Q<VisualElement>("StarLevelAfter");
        for (int i = 0; i < 5; i++)
        {
            curStars[i]  = rowBefore?.Q<VisualElement>($"Star{i+1}");
            nextStars[i] = rowAfter ?.Q<VisualElement>($"Star{i+1}After");
        }

        /* 事件 */
        if (rankUpBtn != null) rankUpBtn.clicked += OnRankUpClicked;
        var close1 = root.Q<Button>("ClosePanel");
        if (close1 != null) close1.clicked += Close;
        var close2 = root.Q<Button>("CloseBtn2");
        if (close2 != null) close2.clicked += Close;
        var mask = root.Q<VisualElement>("BlackSpace");
        if (mask != null) mask.RegisterCallback<ClickEvent>(_ => Close());
    }

    /*──────── 升星按钮 ────────*/
    void OnRankUpClicked()
    {
        if (!StarUpgradeSystem.TryUpgrade(dyn))
        {
            PopupManager.Show("提示","碎片不足或已满星");
            return;
        }
        RefreshUI();
        // CardInventoryUI.I.RefreshCurrentCardPanels();
    }

    /*──────── 刷 UI ────────*/
    void RefreshUI()
    {
        if (info == null || dyn == null) return;

        int curStar  = dyn.star;
        int nextStar = Mathf.Clamp(curStar + 1, 0, 15);

        /* 技能名称 */
        lblTitle[0].text = activeSkillDB  ? activeSkillDB.Get(info.activeSkillId)?.cnName  ?? "—" : "—";
        lblTitle[1].text = passiveSkillDB ? passiveSkillDB.Get(info.passiveOneId )?.cnName ?? "—" : "—";
        lblTitle[2].text = passiveSkillDB ? passiveSkillDB.Get(info.passiveTwoId )?.cnName ?? "—" : "—";

        /* 碎片 */
        copiesHaveLbl.text = RES ? RES.upTierMaterial.ToString() : "0";


        /* 技能等级 */
        for (int i = 0; i < 3; i++)
        {
            lblBeforeLv[i].text = SkillLevelHelper.GetSkillLevel(curStar,  i).ToString();
            lblAfterLv [i].text = curStar >= 15 ? "—"
                                   : SkillLevelHelper.GetSkillLevel(nextStar, i).ToString();
        }

        /* 战力显示 */
        /* ─── 战力显示（直接取 Star Table 固定值） ─── */
        int curPower = DB.GetStar(curStar )?.battlePowerAdd ?? 0;
        Debug.Log(curPower);
        powerNowLbl.text = curPower.ToString();

        if (curStar >= 15)
        {
            powerNextLbl.text = "MAX";
        }
        else
        {
            int nextPower = DB.GetStar(nextStar)?.battlePowerAdd ?? 0;
            powerNextLbl.text = nextPower.ToString();
        }
        /*──────── 碎片需求 & 按钮 ────────*/
        int shardsHave = RES ? RES.upTierMaterial : 0;
        copiesHaveLbl.text = shardsHave.ToString();

        if (curStar >= 15)
        {
            // 满星：按钮禁用、需求显示 “—”
            copiesNeedLbl.text = "—";
            rankUpBtn.SetEnabled(false);
            copiesHaveLbl.style.color = okColor;
            var rowAfter = nextStars[0]?.parent;        // StarLevelAfter 容器
            if (rowAfter != null) rowAfter.style.display = DisplayStyle.None;
        }
        else
        {
            var rule = DB.GetStar(nextStar);                // 下一星规则
            int need = rule != null ? rule.shardsRequired : 0;
            copiesNeedLbl.text = need.ToString();

            bool enough = shardsHave >= need;
            rankUpBtn.SetEnabled(enough);
            copiesHaveLbl.style.color = enough ? okColor : errColor;
        }


        UpdateStarRows(curStar, nextStar);
    }

    /*──────── 星星行工具 ────────*/
    void UpdateStarRows(int curStar, int nextStar)
    {
        var curRule = DB.GetStar(curStar);
        if (curRule != null)
            SetStarRow(curStars, curRule.starsInFrame, curRule.frameColor);

        if (curStar < 15)
        {
            var nextRule = DB.GetStar(nextStar);
            if (nextRule != null)
                SetStarRow(nextStars, nextRule.starsInFrame, nextRule.frameColor);
        }
        else
        {
            SetStarRow(nextStars, 0, "blue");
        }
    }

    void SetStarRow(VisualElement[] row, int lit, string color)
    {
        if (row == null || row.Length < 5 || row[0] == null) return;

        Sprite litSprite = color switch
        {
            "purple" => purpleStar,
            "gold"   => goldStar,
            _        => blueStar
        };

        for (int i = 0; i < 5; i++)
            row[i].style.backgroundImage =
                new StyleBackground(i < lit ? litSprite : emptyStar);
    }
}
