// ────────────────────────────────────────────────────────────────────────────────
// Assets/Scripts/UI/SkillUpgradePanelController.cs
// ResetAllBtn → 弹出确认框，消耗 1×RespecPotion 洗点
// ────────────────────────────────────────────────────────────────────────────────
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Game;

[RequireComponent(typeof(UIDocument))]
public class SkillUpgradePanelController : MonoBehaviour, IUIPanelController
{
    [SerializeField] private LordCardStaticData lordCardStatic;
    [SerializeField] private PlayerLordCard    playerLordCard;

    /*── 暂存滑杆值 ──*/
    private int sliderAtk, sliderDef, sliderIQ;

    /*── UI 节点 ──*/
    UIDocument doc;
    SliderInt atkSlider, defSlider, iqSlider;
    Button upgradeAtkBtn, upgradeDefBtn, upgradeIQBtn,
           resetBtn, confirmBtn, resetAllBtn;
    Label remainingLbl,
          addAtkLbl, addDefLbl, addIQLbl,
          atkPtsAddLbl, defPtsAddLbl, iqPtsAddLbl,
          beforeAtkLbl, beforeDefLbl, beforeIQLbl,
          afterAtkLbl,  afterDefLbl,  afterIQLbl,
          atkUsedLbl,   defUsedLbl,   iqUsedLbl;

    /*──────── 生命周期 ────────*/
    void Awake()
    {
        doc = GetComponent<UIDocument>();
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        CacheNodes();
        ResetTemp();
        RefreshUI();
    }

    void OnDisable()
    {
        atkSlider.UnregisterValueChangedCallback(OnAtkSlider);
        defSlider.UnregisterValueChangedCallback(OnDefSlider);
        iqSlider.UnregisterValueChangedCallback(OnIQSlider);

        upgradeAtkBtn.clicked -= OnUpgradeAtkClicked;
        upgradeDefBtn.clicked -= OnUpgradeDefClicked;
        upgradeIQBtn.clicked  -= OnUpgradeIQClicked;
        resetBtn.clicked      -= OnResetClicked;
        confirmBtn.clicked    -= OnConfirmClicked;
        resetAllBtn.clicked   -= OnResetAllClicked;
    }

    public void Open(CardInfoStatic _, PlayerCard __) => gameObject.SetActive(true);
    public void Close() => gameObject.SetActive(false);

    /*──────── 抓节点 ────────*/
    void CacheNodes()
    {
        var root = doc.rootVisualElement;

        atkSlider = root.Q<SliderInt>("AtkSlider");
        defSlider = root.Q<SliderInt>("DefSlider");
        iqSlider  = root.Q<SliderInt>("IQSlider");
        atkSlider.lowValue = defSlider.lowValue = iqSlider.lowValue = 0;

        upgradeAtkBtn = root.Q<Button>("upgradeAtkBtn");
        upgradeDefBtn = root.Q<Button>("upgradeDefBtn");
        upgradeIQBtn  = root.Q<Button>("upgradeIQBtn");
        resetBtn      = root.Q<Button>("ResetBtn");
        confirmBtn    = root.Q<Button>("ConfirmBtn");
        resetAllBtn   = root.Q<Button>("ResetAllBtn");   // 洗点按钮

        remainingLbl    = root.Q<Label>("RemainingSkillPts");
        addAtkLbl       = root.Q<Label>("UpgradeAddAtk");
        addDefLbl       = root.Q<Label>("UpgradeAddDef");
        addIQLbl        = root.Q<Label>("UpgradeAddIQ");
        atkPtsAddLbl    = root.Q<Label>("AtkPointsAdd");
        defPtsAddLbl    = root.Q<Label>("DefPointsAdd");
        iqPtsAddLbl     = root.Q<Label>("IQPointsAdd");

        beforeAtkLbl = root.Q<Label>("BeforeUpgradeAtkStat");
        beforeDefLbl = root.Q<Label>("BeforeUpgradeDefStat");
        beforeIQLbl  = root.Q<Label>("BeforeUpgradeIQStat");
        afterAtkLbl  = root.Q<Label>("AfterOrgAtkStat");
        afterDefLbl  = root.Q<Label>("AfterOrgDefStat");
        afterIQLbl   = root.Q<Label>("AfterOrgIQStat");

        atkUsedLbl = root.Q<Label>("AtkPoints");
        defUsedLbl = root.Q<Label>("DefPoints");
        iqUsedLbl  = root.Q<Label>("IQPoints");

        /*── 事件绑定 ──*/
        atkSlider.RegisterValueChangedCallback(OnAtkSlider);
        defSlider.RegisterValueChangedCallback(OnDefSlider);
        iqSlider.RegisterValueChangedCallback(OnIQSlider);

        upgradeAtkBtn.clicked += OnUpgradeAtkClicked;
        upgradeDefBtn.clicked += OnUpgradeDefClicked;
        upgradeIQBtn.clicked  += OnUpgradeIQClicked;
        resetBtn.clicked      += OnResetClicked;
        confirmBtn.clicked    += OnConfirmClicked;
        resetAllBtn.clicked   += OnResetAllClicked;

        root.Q<Button>("ClosePanel")?.RegisterCallback<ClickEvent>(_ => Close());
        root.Q<VisualElement>("BlackSpace")?.RegisterCallback<ClickEvent>(_ => Close());
    }

    /*──────── 滑杆 & +1 ────────*/
    void OnAtkSlider(ChangeEvent<int> e){ sliderAtk = Clamp(e.newValue, atkSlider.highValue); RefreshUI(); }
    void OnDefSlider(ChangeEvent<int> e){ sliderDef = Clamp(e.newValue, defSlider.highValue); RefreshUI(); }
    void OnIQSlider (ChangeEvent<int> e){ sliderIQ  = Clamp(e.newValue, iqSlider.highValue ); RefreshUI(); }

    void OnUpgradeAtkClicked(){ IncSlider(ref sliderAtk, atkSlider); }
    void OnUpgradeDefClicked(){ IncSlider(ref sliderDef, defSlider); }
    void OnUpgradeIQClicked (){ IncSlider(ref sliderIQ , iqSlider ); }
    void IncSlider(ref int v, SliderInt s){ if(v < s.highValue){ v++; s.SetValueWithoutNotify(v); RefreshUI(); } }

    /*──────── Reset / Confirm ────────*/
    void OnResetClicked(){ ResetTemp(); RefreshUI(); }

    public event Action onConfirm;
    void OnConfirmClicked()
    {
        int total = sliderAtk + sliderDef + sliderIQ;
        if (total == 0) { Close(); return; }

        if (sliderAtk > 0) playerLordCard.SpendSkillPoints(AttributeType.Atk, sliderAtk, lordCardStatic);
        if (sliderDef > 0) playerLordCard.SpendSkillPoints(AttributeType.Def, sliderDef, lordCardStatic);
        if (sliderIQ  > 0) playerLordCard.SpendSkillPoints(AttributeType.IQ , sliderIQ , lordCardStatic);

        onConfirm?.Invoke();
        Close();
    }

    /*──────── 洗点确认框 ────────*/
    void OnResetAllClicked()
    {
        int potions = (int)PlayerResourceBank.I[ResourceType.RespecPotion];
        if (potions <= 0)
        {
            PopupManager.Show("提示", "缺少洗点药水");
            return;
        }

        string msg = $"是否消耗 1 个洗点药水? (当前持有 {potions})";
        PopupManager.ShowConfirm(
            msg,
            onYes: () =>
            {
                playerLordCard.RefundAllSkillPoints();
                PlayerResourceBank.I.Spend(ResourceType.RespecPotion, 1);
                ResetTemp();
                RefreshUI();
                onConfirm?.Invoke();
            },
            onNo: null
        );
    }

    /*──────── 工具 ────────*/
    int Clamp(int v, int max) => Mathf.Clamp(v, 0, max);
    void ResetTemp()
    {
        sliderAtk = sliderDef = sliderIQ = 0;
        atkSlider.SetValueWithoutNotify(0);
        defSlider.SetValueWithoutNotify(0);
        iqSlider.SetValueWithoutNotify(0);
    }

    /*──────── 刷 UI ────────*/
    void RefreshUI()
    {
        int inc   = lordCardStatic.valuePerSkillPoint;
        int avail = playerLordCard.availableSkillPoints;

        atkSlider.highValue = avail - (sliderDef + sliderIQ);
        defSlider.highValue = avail - (sliderAtk + sliderIQ);
        iqSlider.highValue  = avail - (sliderAtk + sliderDef);

        int remain = Mathf.Max(0, avail - (sliderAtk + sliderDef + sliderIQ));
        remainingLbl.text = remain.ToString();

        int curAtk = playerLordCard.atk, curDef = playerLordCard.def, curIQ = playerLordCard.iq;
        beforeAtkLbl.text = afterAtkLbl.text = curAtk.ToString();
        beforeDefLbl.text = afterDefLbl.text = curDef.ToString();
        beforeIQLbl.text  = afterIQLbl.text  = curIQ.ToString();

        ShowPreview(addAtkLbl, atkPtsAddLbl, sliderAtk, inc);
        ShowPreview(addDefLbl, defPtsAddLbl, sliderDef, inc);
        ShowPreview(addIQLbl, iqPtsAddLbl, sliderIQ , inc);

        atkUsedLbl.text = (playerLordCard.atkPointsUsed + sliderAtk).ToString();
        defUsedLbl.text = (playerLordCard.defPointsUsed + sliderDef).ToString();
        iqUsedLbl.text  = (playerLordCard.iqPointsUsed + sliderIQ ).ToString();
    }

    void ShowPreview(Label attrAdd, Label ptsAdd, int val, int inc)
    {
        bool show = val > 0;
        attrAdd.style.display = ptsAdd.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (show)
        {
            attrAdd.text = "+" + (val * inc);
            ptsAdd.text  = "+" + val;
        }
    }
}
