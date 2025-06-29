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

    [Header("外部资源拖拽")]
    [SerializeField] private PlayerResources playerResources;

    [SerializeField] VhSizer vhSizer;

    [Header("星星贴图")]
    [SerializeField] Sprite emptyStar;
    [SerializeField] Sprite blueStar;
    [SerializeField] Sprite purpleStar;
    [SerializeField] Sprite goldStar;

    [Header("数据库引用")]
    [SerializeField] ActiveSkillDatabase  activeSkillDB;
    [SerializeField] PassiveSkillDatabase passiveSkillDB;

    readonly Color okColor  = new (0.32f, 0.65f, 0.53f, 1f);
    readonly Color errColor = new (0.94f, 0.15f, 0.15f, 1f);

    /*── 静态表 ─*/
    static CardDatabaseStatic DB => _db ??= Resources.Load<CardDatabaseStatic>("CardDatabaseStatic");
    static CardDatabaseStatic _db;

    /*──────── UI 节点 ────────*/
    UIDocument doc;

    Label costSelLbl;                 // CostSelection
    Label m1OwnLbl, m2OwnLbl;         // Material1Own / Material2Own
    Toggle useGeneralTgl;
    VisualElement genMatRow;          // name="GenMat"
    Label plusLbl;                    // name="Plus"  (“+” 号标签)

    Button rankUpBtn;

    /* 其他节点(星星/技能/战力) 省略注释，内容未改 */
    Label[] lblTitle    = new Label[3];
    Label[] lblBeforeLv = new Label[3];
    Label[] lblAfterLv  = new Label[3];
    VisualElement[] curStars  = new VisualElement[5];
    VisualElement[] nextStars = new VisualElement[5];
    Label powerNowLbl;
    Label powerNextLbl;

    /*──────── 生命周期 ────────*/
    void Awake() { doc = GetComponent<UIDocument>(); gameObject.SetActive(false); }
    void OnEnable() { CacheNodes(); StartCoroutine(AfterEnable()); }
    IEnumerator AfterEnable() { yield return null; vhSizer?.Apply(); }
    void OnDisable()
    {
        if (rankUpBtn != null) rankUpBtn.clicked -= OnRankUpClicked;
        if (useGeneralTgl != null) useGeneralTgl.UnregisterValueChangedCallback(_ => RefreshUI());
    }

    /*──────── IUIPanelController ────────*/
    public void Open(CardInfoStatic stat, PlayerCard dynInfo) { info = stat; dyn = dynInfo; gameObject.SetActive(true); RefreshUI(); }
    public void Close() => gameObject.SetActive(false);

    /*──────── 节点缓存 ────────*/
    void CacheNodes()
    {
        var root = doc.rootVisualElement;

        costSelLbl = root.Q<Label>("CostSelection");

        m1OwnLbl   = root.Q<Label>("Material1Own");
        m2OwnLbl   = root.Q<Label>("Material2Own");

        genMatRow  = root.Q<VisualElement>("GenMat");
        if (genMatRow != null) genMatRow.style.display = DisplayStyle.None;

        plusLbl    = root.Q<Label>("Plus");                     // “+” 标签
        if (plusLbl != null) plusLbl.style.display = DisplayStyle.None;

        useGeneralTgl = root.Q<Toggle>("useGeneral");
        if (useGeneralTgl != null) useGeneralTgl.RegisterValueChangedCallback(_ => RefreshUI());

        powerNowLbl  = root.Q<Label>("PowerSlotStat");
        powerNextLbl = root.Q<Label>("AfterUptierPower");
        rankUpBtn    = root.Q<Button>("RankUp");

        lblTitle[0]    = root.Q<Label>("BeforeUptierSlotTitle1");
        lblTitle[1]    = root.Q<Label>("BeforeUptierSlotTitle2");
        lblTitle[2]    = root.Q<Label>("BeforeUptierSlotTitle3");

        lblBeforeLv[0] = root.Q<Label>("BeforeUptierSlotStat1");
        lblBeforeLv[1] = root.Q<Label>("BeforeUptierSlotStat2");
        lblBeforeLv[2] = root.Q<Label>("BeforeUptierSlotStat3");

        lblAfterLv[0]  = root.Q<Label>("AfterUptierSlotTitle1");
        lblAfterLv[1]  = root.Q<Label>("AfterUptierSlotTitle2");
        lblAfterLv[2]  = root.Q<Label>("AfterUptierSlotTitle3");

        var rowBefore = root.Q<VisualElement>("StarLevelBefore");
        var rowAfter  = root.Q<VisualElement>("StarLevelAfter");
        for (int i = 0; i < 5; i++)
        {
            curStars[i]  = rowBefore?.Q<VisualElement>($"Star{i+1}");
            nextStars[i] = rowAfter ?.Q<VisualElement>($"Star{i+1}After");
        }

        if (rankUpBtn != null) rankUpBtn.clicked += OnRankUpClicked;

        /*―― 关闭面板三行 —— 请勿改动 ――*/
        var close1 = root.Q<Button>("ClosePanel");  if (close1 != null) close1.clicked += Close;
        var close2 = root.Q<Button>("CloseBtn2");   if (close2 != null) close2.clicked += Close;
        var mask   = root.Q<VisualElement>("BlackSpace");
        if (mask != null) mask.RegisterCallback<ClickEvent>(_ => Close());
    }

    /*──────── 升星按钮 ────────*/
    void OnRankUpClicked()
    {
        if (info == null || dyn == null) return;
        var rule = DB.GetStar(dyn.star + 1); if (rule == null) return;

        int need = rule.shardsRequired;
        int specUse = Mathf.Min(dyn.copies, need);
        int gap     = need - specUse;
        int genHave = GetGeneralShardCount(info.tier);

        if (specUse + genHave < need) { PopupManager.Show("提示", "碎片不足或已满星"); return; }

        dyn.copies -= specUse;
        if (gap > 0) ConsumeGeneralShards(info.tier, gap);
        dyn.star += 1;
        PlayerCardBankMgr.I?.MarkDirty(dyn.id);
        RefreshUI();
    }

    /*──────── 刷 UI ────────*/
    void RefreshUI()
    {
        if (info == null || dyn == null) return;
        int curStar = dyn.star, nextStar = Mathf.Clamp(curStar + 1, 0, 15);
        var ruleNext = DB.GetStar(nextStar);

        /* 材料计算 */
        int need    = ruleNext?.shardsRequired ?? 0;
        int specOwn = dyn.copies;
        int specUse = Mathf.Min(specOwn, need);
        int gap     = need - specUse;

        int genOwn  = GetGeneralShardCount(info.tier);
        bool useGen = (useGeneralTgl?.value == true);
        int genUse  = useGen ? gap : 0;

        /* 更新数字 */
        m1OwnLbl.text = $"{specUse}/{specOwn}";
        m2OwnLbl.text = $"{genUse}/{genOwn}";

        /* 行、+号 同时显隐 */
        bool showGen = useGen && gap > 0;
        if (genMatRow != null) genMatRow.style.display = showGen ? DisplayStyle.Flex : DisplayStyle.None;
        if (plusLbl   != null) plusLbl.style.display   = showGen ? DisplayStyle.Flex : DisplayStyle.None;

        /* CostSelection */
        int haveTotal = Mathf.Min(specOwn + (useGen ? genOwn : 0), need);
        costSelLbl.text        = $"{haveTotal}/{need}";
        costSelLbl.style.color = haveTotal >= need ? okColor : errColor;

        /* Toggle */
        bool needGeneral = gap > 0 && curStar < 15;
        if (useGeneralTgl != null)
        {
            useGeneralTgl.style.display = needGeneral ? DisplayStyle.Flex : DisplayStyle.None;
            if (needGeneral) useGeneralTgl.text = $"使用{info.tier}级通用碎片";
        }

        /* 按钮 */
        rankUpBtn.SetEnabled(haveTotal >= need && curStar < 15);

        UpdateStarRows(curStar, nextStar);
    }

    /*──────── 星星行工具 ────────*/
    void UpdateStarRows(int cur, int nxt)
    {
        var curRule = DB.GetStar(cur);
        var nxtRule = DB.GetStar(nxt);
        if (curRule != null) SetStarRow(curStars, curRule.starsInFrame, curRule.frameColor);
        if (cur >= 15) SetStarRow(nextStars, 0, "blue");
        else if (nxtRule != null) SetStarRow(nextStars, nxtRule.starsInFrame, nxtRule.frameColor);
    }
    void SetStarRow(VisualElement[] row, int lit, string color)
    {
        if (row[0] == null) return;
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

    /*──────── 通用碎片工具 ────────*/
    int GetGeneralShardCount(Tier t) => playerResources == null ? 0 : t switch
    {
        Tier.S => playerResources.heroCrestSGeneral,
        Tier.A => playerResources.heroCrestAGeneral,
        Tier.B => playerResources.heroCrestBGeneral,
        _      => 0
    };
    void ConsumeGeneralShards(Tier t, int amount)
    {
        if (playerResources == null || amount <= 0) return;
        switch (t)
        {
            case Tier.S: playerResources.heroCrestSGeneral -= amount; break;
            case Tier.A: playerResources.heroCrestAGeneral -= amount; break;
            case Tier.B: playerResources.heroCrestBGeneral -= amount; break;
        }
    }
}
