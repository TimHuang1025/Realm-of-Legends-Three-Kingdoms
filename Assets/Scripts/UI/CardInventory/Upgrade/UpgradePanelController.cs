using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Core;

/// <summary>
/// 升级面板：显示当前等级 & 下一等级属性（Atk / Def / Int / Command）并处理升级逻辑
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UpgradePanelController : MonoBehaviour, IUIPanelController
{
    [SerializeField] VhSizer vhSizer;

    /*──────── 数据缓存 ────────*/
    CardInfoStatic info;      // 静态
    PlayerCard     dyn;       // 动态 (null = 未拥有)

    /*──────── UI 元素 ────────*/
    UIDocument doc;

    /* 等级 */
    Label lvlBeforeLbl, lvlAfterLbl;

    /* 属性 4 维 */
    Label beforeAtkLbl, beforeDefLbl, beforeIntLbl, beforeCmdLbl;
    Label afterAtkLbl,  afterDefLbl,  afterIntLbl,  afterCmdLbl;
    Label addAtkLbl,    addDefLbl,    addIntLbl,    addCmdLbl;

    /* 材料 */
    Label expNeedLbl, matNeedLbl, expHaveLbl, matHaveLbl;
    VisualElement mat2VE;

    /* 升级按钮 */
    Button upgradeBtn;

    /*──────── 生命周期 ────────*/
    void Awake()
    {
        doc = GetComponent<UIDocument>();
        gameObject.SetActive(false);                // 默认隐藏
    }

    void OnEnable()
    {
        CacheNodesAndBind();
        RefreshUI();
        StartCoroutine(AfterEnableRoutine());
    }

    void OnDisable()
    {
        if (upgradeBtn != null)
            upgradeBtn.clicked -= OnUpgradeClicked;
    }

    /*──────── IUIPanelController ────────*/
    
    public void Open(CardInfoStatic staticInfo, PlayerCard dynamicInfo)
    {
        gameObject.SetActive(true);
        SetData(staticInfo, dynamicInfo); // 
    }

    public void Close() => gameObject.SetActive(false);

    public void SetData(CardInfoStatic staticInfo, PlayerCard dynamicInfo)
    {
        info = staticInfo;
        dyn  = dynamicInfo;
        RefreshUI();
    }

    /*──────── 抓节点 & 绑定按钮 ────────*/
    void CacheNodesAndBind()
    {
        var root = doc.rootVisualElement;

        /* 等级 */
        lvlBeforeLbl = root.Q<Label>("LevelBeforeUpgrade");
        lvlAfterLbl  = root.Q<Label>("LevelAfterUpgrade");

        /* 属性 4 维 */
        beforeAtkLbl = root.Q<Label>("BeforeUpgradeAtkStat");
        beforeDefLbl = root.Q<Label>("BeforeUpgradeDefStat");
        beforeIntLbl = root.Q<Label>("BeforeUpgradeIQStat");
        beforeCmdLbl = root.Q<Label>("BeforeUpgradeCmdStat");

        afterAtkLbl  = root.Q<Label>("AfterOrgAtkStat");
        afterDefLbl  = root.Q<Label>("AfterOrgDefStat");
        afterIntLbl  = root.Q<Label>("AfterOrgIQStat");
        afterCmdLbl  = root.Q<Label>("AfterOrgCmdStat");

        addAtkLbl    = root.Q<Label>("UpgradeAddAtk");
        addDefLbl    = root.Q<Label>("UpgradeAddDef");
        addIntLbl    = root.Q<Label>("UpgradeAddIQ");
        addCmdLbl    = root.Q<Label>("UpgradeAddCmd");

        /* 材料 */
        expNeedLbl   = root.Q<Label>("UpgradeMaterial1Num");
        expHaveLbl   = root.Q<Label>("PlayerMaterial1Num");
        matNeedLbl   = root.Q<Label>("UpgradeMaterial2Num");
        matHaveLbl   = root.Q<Label>("PlayerMaterial2Num");
        mat2VE       = root.Q<VisualElement>("Material2VE");

        /* 升级按钮 */
        upgradeBtn = root.Q<Button>("UpgradeBtn");
        if (upgradeBtn != null)
            upgradeBtn.clicked += OnUpgradeClicked;

        // 2) 按钮 & 黑幕
        var closeBtn   = root.Q<Button>("ClosePanel");
        var closeBtn2  = root.Q<Button>("CloseBtn2");
        var blackspace = root.Q<VisualElement>("BlackSpace");

        if (closeBtn  != null) closeBtn.clicked  += Close;
        if (closeBtn2 != null) closeBtn2.clicked += Close;
        if (blackspace != null)
            blackspace.RegisterCallback<ClickEvent>(_ => Close());
    }

    /*──────── 升级回调 ────────*/
    void OnUpgradeClicked()
    {
        if (dyn == null) return;

        var (expNeed, matNeed) = LevelStatCalculator.GetUpgradeCost(dyn.level);

        bool hasExp  = PlayerResourceBank.I[ResourceType.HeroExp]  >= expNeed;
        bool hasMat2 = PlayerResourceBank.I[ResourceType.HeroMat2] >= matNeed;

        if (!hasExp || !hasMat2)
        {
            PopupManager.Show("提示", "材料不足，无法升级！");
            return;
        }

        // 扣资源
        PlayerResourceBank.I.Spend(ResourceType.HeroExp,  expNeed);
        if (matNeed > 0)
            PlayerResourceBank.I.Spend(ResourceType.HeroMat2, matNeed);

        //  通过 BankMgr 升级 → 自动触发 onCardUpdated
        PlayerCardBankMgr.I.AddLevel(dyn.id, 1);

        // 本面板自身也刷新一次（更平滑）
        RefreshUI();
    }

    /*──────── 刷 UI ────────*/
    void RefreshUI()
    {
        if (info == null || lvlBeforeLbl == null) return;

        int lvNow  = dyn?.level ?? 1;
        int lvNext = lvNow + 1;
        lvlBeforeLbl.text = $"等级 {lvNow}";
        lvlAfterLbl.text  = $"等级 {lvNext}";

        /* 当前属性 & 增量 */
        var (atk, def, intel, cmd)  = LevelStatCalculator.CalculateStats(info, dyn);
        var (dAtk, dDef, dInt, dCmd)= LevelStatCalculator.CalculateDeltaNextLevel(info, dyn);

        beforeAtkLbl.text = atk.ToString();
        beforeDefLbl.text = def.ToString();
        beforeIntLbl.text = intel.ToString();
        //beforeCmdLbl.text = cmd.ToString();

        afterAtkLbl.text  = (atk + dAtk).ToString();
        afterDefLbl.text  = (def + dDef).ToString();
        afterIntLbl.text  = (intel + dInt).ToString();
        //afterCmdLbl.text  = (cmd + dCmd).ToString();

        addAtkLbl.text = $"+{dAtk}";
        //Debug.Log($"UpgradePanel: {info} Lv{lvNow} -> Lv{lvNext}, Atk {atk} -> {atk + dAtk}");
        addDefLbl.text = $"+{dDef}";
        addIntLbl.text = $"+{dInt}";
        //addCmdLbl.text = $"+{dCmd}";

        /* 材料 */
        var (expNeed, matNeed) = LevelStatCalculator.GetUpgradeCost(lvNow);
        expNeedLbl.text = expNeed.ToString();

        if (matNeed > 0)
        {
            mat2VE.style.display = DisplayStyle.Flex;
            matNeedLbl.text      = matNeed.ToString();
        }
        else
        {
            mat2VE.style.display = DisplayStyle.None;
        }

        /* 玩家持有 */
        int expHave  = PlayerResourceBank.I[ResourceType.HeroExp];
        int mat2Have = PlayerResourceBank.I[ResourceType.HeroMat2];
        expHaveLbl.text = expHave.ToString();
        matHaveLbl.text = mat2Have.ToString();

        Color okColor = new(0.32f, 0.65f, 0.53f, 1f);   // #51A687
        expHaveLbl.style.color = expHave >= expNeed ? okColor : Color.red;
        matHaveLbl.style.color = mat2Have >= matNeed ? okColor : Color.red;
    }

    IEnumerator AfterEnableRoutine()
    {
        yield return null;          // 等 1 帧再布局
        vhSizer?.Apply();
    }
}
