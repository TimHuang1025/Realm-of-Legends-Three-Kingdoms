using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UpgradePanelController : MonoBehaviour
{
    [SerializeField] VhSizer vhSizer;

    /* ───────── UI 节点 ───────── */
    UIDocument doc;
    // 等级
    Label lvlBeforeLbl, lvlAfterLbl;
    // 三围
    Label beforeAtkLbl, beforeDefLbl, beforeIntLbl;
    Label afterAtkLbl,  afterDefLbl,  afterIntLbl;
    Label addAtkLbl,    addDefLbl,    addIntLbl;
    // 材料
    Label expNeedLbl, matNeedLbl, expHaveLbl, matHaveLbl;
    VisualElement mat2VE;
    // 按钮
    Button upgradeBtn;

    /* ───────── 运行时 ───────── */
    CardInfo curCard;

    /* ───────── 生命周期 ───────── */
    void Awake()
    {
        doc = GetComponent<UIDocument>();
        gameObject.SetActive(false);          // 默认隐藏
    }

    void OnEnable()
    {
        CacheNodesAndBind();                  // 抓控件 + 事件绑定
        RefreshUI();                          // 若 curCard 已有，先显示
        StartCoroutine(AfterEnableRoutine());
    }

    void OnDisable()
    {
        if (upgradeBtn != null)
            upgradeBtn.clicked -= OnUpgradeClicked;

        if (curCard != null)
            curCard.OnStatsChanged -= OnCardChanged;   // 解绑，防泄漏
    }

    /* ───────── 抓节点 & 关闭按钮 ───────── */
    void CacheNodesAndBind()
    {
        var root = doc.rootVisualElement;

        // 等级
        lvlBeforeLbl = root.Q<Label>("LevelBeforeUpgrade");
        lvlAfterLbl  = root.Q<Label>("LevelAfterUpgrade");

        // 三围
        beforeAtkLbl = root.Q<Label>("BeforeUpgradeAtkStat");
        beforeDefLbl = root.Q<Label>("BeforeUpgradeDefStat");
        beforeIntLbl = root.Q<Label>("BeforeUpgradeIQStat");

        afterAtkLbl  = root.Q<Label>("AfterOrgAtkStat");
        afterDefLbl  = root.Q<Label>("AfterOrgDefStat");
        afterIntLbl  = root.Q<Label>("AfterOrgIQStat");

        addAtkLbl    = root.Q<Label>("UpgradeAddAtk");
        addDefLbl    = root.Q<Label>("UpgradeAddDef");
        addIntLbl    = root.Q<Label>("UpgradeAddIQ");

        // 材料
        expNeedLbl   = root.Q<Label>("UpgradeMaterial1Num");
        expHaveLbl   = root.Q<Label>("PlayerMaterial1Num");
        matNeedLbl   = root.Q<Label>("UpgradeMaterial2Num");
        matHaveLbl   = root.Q<Label>("PlayerMaterial2Num");
        mat2VE       = root.Q<VisualElement>("Material2VE");

        // 关闭按钮 & 黑幕（固定模板）
        var closeBtn   = root.Q<Button>("ClosePanel");
        var closeBtn2  = root.Q<Button>("CloseBtn2");
        var blackspace = root.Q<VisualElement>("BlackSpace");
        if (closeBtn  != null) closeBtn.clicked  += Close;
        if (closeBtn2 != null) closeBtn2.clicked += Close;
        if (blackspace != null)
            blackspace.RegisterCallback<ClickEvent>(_ => Close());

        // 升级按钮
        upgradeBtn = root.Q<Button>("UpgradeBtn");
        if (upgradeBtn != null)
            upgradeBtn.clicked += OnUpgradeClicked;
    }

    /* ───────── 公共接口 ───────── */
    public void SetData(CardInfo card)
    {
        // 解绑旧订阅
        if (curCard != null)
            curCard.OnStatsChanged -= OnCardChanged;

        curCard = card;

        // 订阅新卡
        if (curCard != null)
            curCard.OnStatsChanged += OnCardChanged;

        RefreshUI();
    }

    public void Open(CardInfo card = null)
    {
        if (card != null) SetData(card);
        gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    /* ───────── 升级按钮回调 ───────── */
    void OnUpgradeClicked()
    {
        if (curCard == null) return;

        // 1) 计算消耗
        var (expNeed, matNeed) = LevelStatCalculator.GetUpgradeCost(curCard.level);

        // 2) 余额检测
        bool hasExp  = PlayerBank.I[ResourceType.HeroExp]  >= expNeed;
        bool hasMat2 = PlayerBank.I[ResourceType.HeroMat2] >= matNeed;
        if (!hasExp || !hasMat2)
        {
            PopupManager.Show("提示", "材料不足，无法升级！");
            return;
        }

        // 3) 扣除
        PlayerBank.I.Spend(ResourceType.HeroExp, expNeed);
        if (matNeed > 0)
            PlayerBank.I.Spend(ResourceType.HeroMat2, matNeed);

        // 4) 升级 —— 会触发 OnStatsChanged → OnCardChanged → RefreshUI
        curCard.AddLevel(1);
    }

    /* ───────── 事件包装 ───────── */
    void OnCardChanged(CardInfo _) => RefreshUI();

    /* ───────── 刷 UI ───────── */
    void RefreshUI()
    {
        if (curCard == null) return;
        Debug.Log($"RefreshUI run. lvlBeforeLbl = {lvlBeforeLbl}");


        /* 等级 */
        int lvNow  = curCard.level;
        int lvNext = lvNow + 1;
        lvlBeforeLbl.text = $"等级 {lvNow}";
        lvlAfterLbl.text  = $"等级 {lvNext}";

        /* 三围 */
        var (atk, def, intel)    = LevelStatCalculator.CalculateStats(curCard);
        var (dAtk, dDef, dInt)   = LevelStatCalculator.CalculateDeltaNextLevel(curCard);

        beforeAtkLbl.text = atk.ToString();
        beforeDefLbl.text = def.ToString();
        beforeIntLbl.text = intel.ToString();

        afterAtkLbl.text  = atk.ToString();
        afterDefLbl.text  = def.ToString();
        afterIntLbl.text  = intel.ToString();

        addAtkLbl.text    = $"+{dAtk}";
        addDefLbl.text    = $"+{dDef}";
        addIntLbl.text    = $"+{dInt}";

        /* 材料需求 */
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
        int expHave  = PlayerBank.I[ResourceType.HeroExp];
        int mat2Have = PlayerBank.I[ResourceType.HeroMat2];

        expHaveLbl.text = expHave.ToString();
        matHaveLbl.text = mat2Have.ToString();
        Color okColor   = new Color32(0x51, 0xA6, 0x87, 0xFF);

        /* === 新增：不足时文字变红 === */
        expHaveLbl.style.color = expHave >= expNeed ? okColor : Color.red;
        matHaveLbl.style.color = mat2Have >= matNeed ? okColor : Color.red;
    }

    IEnumerator AfterEnableRoutine()
    {
        yield return null;
        vhSizer?.Apply();
    }
}
