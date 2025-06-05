using System.Collections;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CardInventoryUI : MonoBehaviour
{
    /*──────── Inspector 拖入 ────────*/
    [SerializeField] private UpgradePanelController upgradePanelCtrl;
    [SerializeField] private UptierPanelController uptierPanelCtrl;
    [SerializeField] private GiftPanelController giftPanelCtrl;
    [SerializeField] private VhSizer vhSizer;
    [SerializeField] private PlayerBaseController playerBaseController;
    [SerializeField] private UnitGiftLevel UnitGiftLevel;
    [SerializeField] private GachaPanelController gachaPanelCtrl;



    /*──────── 私有字段 ────────*/
    private VisualElement cardsVe;
    private VisualElement infoVe;

    private Button returnBtn;
    private Button upgradeBtn;
    private Button infobtn;
    private Button uptierBtn;
    private Button closeInfoBtn;
    private Button giftBtn;
    private Button gachaBtn;
    private Label mat2valueLbl;
    private Label expvalueLbl;



    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        gachaBtn = root.Q<Button>("GachaBtn");
        if (gachaBtn != null)
            gachaBtn.clicked += () => playerBaseController?.ShowGachaPage();


        // —— Cards / Info —— //
        cardsVe = root.Q<VisualElement>("Cards");
        infoVe = root.Q<VisualElement>("Info");
        infoVe.style.display = DisplayStyle.None;
        cardsVe.style.display = DisplayStyle.Flex;
        mat2valueLbl = root.Q<Label>("mat2value");
        expvalueLbl = root.Q<Label>("expvalue");
        Refresh();
        PlayerBank.I.onBankChanged += OnBankChanged;
        

        // —— 按钮绑定 —— //
        returnBtn = root.Q<Button>("ReturnBtn");
        upgradeBtn = root.Q<Button>("InfoUpgradeBtn");
        uptierBtn = root.Q<Button>("InfoUptierBtn");
        giftBtn = root.Q<Button>("InfoGiftBtn");
        infobtn = root.Q<Button>("InfoBtn");
        closeInfoBtn = root.Q<Button>("ClosePanelForInfo");


        if (returnBtn != null) returnBtn.clicked += () => playerBaseController?.HideCardInventoryPage();
        if (upgradeBtn != null) upgradeBtn.clicked += () => StartCoroutine(OpenUpgradePanelRefresh());
        if (uptierBtn != null) uptierBtn.clicked += () => StartCoroutine(OpenUptierPanelRefresh());
        if (giftBtn != null) giftBtn.clicked += () => StartCoroutine(OpenGiftPanelRefresh());
        if (infobtn != null) infobtn.clicked += OpenInfoPanel;
        if (closeInfoBtn != null) closeInfoBtn.clicked += CloseInfoPanel;
    }

    /*──────── 打开升级面板 ────────*/
    IEnumerator OpenUpgradePanelRefresh()
    {
        if (upgradePanelCtrl == null) yield break;

        upgradePanelCtrl.Open();
        yield return null;        // 等 1 帧布局
        vhSizer?.Apply();
    }
    IEnumerator OpenUptierPanelRefresh()
    {
        if (uptierPanelCtrl == null) yield break;

        uptierPanelCtrl.Open();
        yield return null;        // 等 1 帧布局
        vhSizer?.Apply();
    }

    IEnumerator OpenGiftPanelRefresh()
    {
        if (giftPanelCtrl == null) yield break;

        giftPanelCtrl.Open();
        yield return null;        // 等 1 帧布局
        vhSizer?.Apply();
    }

    /*──────── Info 面板切换 ────────*/
    void OpenInfoPanel()
    {
        if (cardsVe == null || infoVe == null) return;
        cardsVe.style.display = DisplayStyle.None;
        infoVe.style.display = DisplayStyle.Flex;
        UnitGiftLevel.RefreshUI();
        vhSizer?.Apply();
    }
    void CloseInfoPanel()
    {
        if (cardsVe == null || infoVe == null) return;
        infoVe.style.display = DisplayStyle.None;
        cardsVe.style.display = DisplayStyle.Flex;
        vhSizer?.Apply();
    }

    public CardInfo CurrentCard { get; private set; }   // 给 GiftPanel 用

    CardInfo currentCard;   // 记录当前选中，用来解绑

    public void OnCardClicked(CardInfo card)
    {
        /* 1. 先把旧的解绑 */
        if (currentCard != null)
            currentCard.OnStatsChanged -= OnCardStatChanged;

        /* 2. 新卡订阅 */
        currentCard = card;
        currentCard.OnStatsChanged += OnCardStatChanged;

        /* 3. 原本就有的逻辑保持不变 */
        UnitGiftLevel.data = card;
        upgradePanelCtrl.SetData(card);        // 建议直接 Open，内部会 SetData & 刷 UI
        UnitGiftLevel.RefreshUI();
        UnitGiftLevel.RefreshEquipSlots();
    }

    /* 事件回调：这张卡属性（如等级）变时触发 */
    void OnCardStatChanged(CardInfo card)
    {
        // 让 Gift 面板 & 其它 UI 立即同步
        UnitGiftLevel.RefreshUI();
        UnitGiftLevel.RefreshEquipSlots();
    }

    void OnDisable()
    {
        // ④ 解绑，防止多次加载重复回调
        if (PlayerBank.I != null)
            PlayerBank.I.onBankChanged -= OnBankChanged;
    }

    /* ===== 回调 & 刷新 ===== */
    void OnBankChanged(ResourceType type)
    {
        if (type == ResourceType.HeroMat2)
            Refresh();
        if (type == ResourceType.HeroExp)
            Refresh();
    }

    void Refresh()
    {
        mat2valueLbl.text = PlayerBank.I[ResourceType.HeroMat2].ToString("N0");
        expvalueLbl.text = PlayerBank.I[ResourceType.HeroExp].ToString("N0");
    }
    
}
