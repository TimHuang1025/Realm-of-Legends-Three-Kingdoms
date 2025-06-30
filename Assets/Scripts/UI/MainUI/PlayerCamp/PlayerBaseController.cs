// Assets/Scripts/Game/UI/PlayerBaseController.cs
using UnityEngine;
using UnityEngine.UIElements;
using UIExt;   // 你的 Bounce 扩展方法所在命名空间

[RequireComponent(typeof(UIDocument))]
public class PlayerBaseController : MonoBehaviour
{
    /*──────── Inspector 拖入 ────────*/
    [Header("主界面与地图")]
    [SerializeField] private GameObject mainUIPanel;
    [SerializeField] private GameObject playerBaseMap;          

    [Header("各功能页面")]
    [SerializeField] private GameObject cardInventoryPage;       // 卡牌仓库
    [SerializeField] private GameObject gachaPage;               // 抽卡
    [SerializeField] private GameObject armyControlPage;         // 军团指挥
    [SerializeField] private GameObject playerCardUpgradePage;   // 主将府（升级主将卡）
    [SerializeField] private GameObject playerTechTreePage;      // ★ 科技树页面

    /*──────── 建筑按钮引用 ────────*/
    private Button cardInventoryBtn;
    private Button armyControlBtn;
    private Button playerCardUpgradeBtn;
    private Button playerTechTreeBtn;                            // ★ 科技树按钮

    /*──────── 生命周期：初始化 ────────*/
    private void Awake()
    {
        /* 仅打开主界面 & 地图，其余功能页隐藏 */
        mainUIPanel.SetActive(true);
        playerBaseMap.SetActive(true);

        cardInventoryPage.SetActive(false);
        gachaPage.SetActive(false);
        armyControlPage.SetActive(false);
        playerCardUpgradePage.SetActive(false);
        playerTechTreePage.SetActive(false);                     // ★
    }

    /*──────── 生命周期：绑定按钮 ────────*/
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        /* —— 卡牌仓库 —— */
        cardInventoryBtn = root.Q<Button>("CardInventoryBuilding");
        if (cardInventoryBtn != null)
        {
            PrepareBuildingButton(cardInventoryBtn);
            cardInventoryBtn.clicked += ShowCardInventoryPage;
        }
        else Debug.LogError("找不到 CardInventoryBuilding 按钮");

        /* —— 军团指挥 —— */
        armyControlBtn = root.Q<Button>("ArmyControlBuilding");
        if (armyControlBtn != null)
        {
            PrepareBuildingButton(armyControlBtn);
            armyControlBtn.clicked += ShowArmyControlPage;
        }
        else Debug.LogError("找不到 ArmyControlBuilding 按钮");

        /* —— 主将府 —— */
        playerCardUpgradeBtn = root.Q<Button>("PlayerCardBuilding");
        if (playerCardUpgradeBtn != null)
        {
            PrepareBuildingButton(playerCardUpgradeBtn);
            playerCardUpgradeBtn.clicked += ShowPlayerCardUpgradePage;
        }
        else Debug.LogError("找不到 PlayerCardBuilding 按钮");

        /* —— 科技树 —— */
        playerTechTreeBtn = root.Q<Button>("PlayerTechTreeBuilding");
        if (playerTechTreeBtn != null)
        {
            PrepareBuildingButton(playerTechTreeBtn);
            playerTechTreeBtn.clicked += ShowPlayerTechTreePage;
        }
        else Debug.LogError("找不到 PlayerTechTreeBuilding 按钮");
    }

    private void OnDisable()
    {
        if (cardInventoryBtn     != null) cardInventoryBtn.clicked     -= ShowCardInventoryPage;
        if (armyControlBtn       != null) armyControlBtn.clicked       -= ShowArmyControlPage;
        if (playerCardUpgradeBtn != null) playerCardUpgradeBtn.clicked -= ShowPlayerCardUpgradePage;
        if (playerTechTreeBtn    != null) playerTechTreeBtn.clicked    -= ShowPlayerTechTreePage;
    }

    /*──────── 公共：统一按钮外观 & 动效 ────────*/
    private void PrepareBuildingButton(Button btn)
    {
        btn.pickingMode            = PickingMode.Position;                     // 整块可点
        btn.style.backgroundColor  = new StyleColor(new Color(0, 0, 0, 0));    // 透明覆盖
        btn.Bounce(0.9f, 0.08f);                                               // UIExt 中的弹跳
    }

    /*──────── 页面切换逻辑 ────────*/
    #region Show / Hide helpers

    /* —— 卡牌仓库 —— */
    private void ShowCardInventoryPage()
    {
        TogglePages(cardInventory: true);
    }
    public void HideCardInventoryPage()
    {
        BackToMain();
    }

    /* —— 抽卡 —— */
    public void ShowGachaPage()
    {
        TogglePages(gacha: true);
    }
    public void HideGachaPage()
    {
        BackToMain();
    }

    /* —— 军团指挥 —— */
    public void ShowArmyControlPage()
    {
        TogglePages(armyControl: true);
    }
    public void HideArmyControlPage()
    {
        BackToMain();
    }

    /* —— 主将府 —— */
    private void ShowPlayerCardUpgradePage()
    {
        TogglePages(cardUpgrade: true);
    }
    public void HidePlayerCardUpgradePage()
    {
        BackToMain();
    }

    /* —— 科技树 —— */
    private void ShowPlayerTechTreePage()                                 // ★
    {
        TogglePages(techTree: true);
    }
    public void HidePlayerTechTreePage()                                  // ★
    {
        BackToMain();
    }

    /* —— 通用切页开关 —— */
    private void TogglePages(
        bool cardInventory = false,
        bool gacha         = false,
        bool armyControl   = false,
        bool cardUpgrade   = false,
        bool techTree      = false)                                       // ★
    {
        cardInventoryPage.SetActive(cardInventory);
        gachaPage.SetActive(gacha);
        armyControlPage.SetActive(armyControl);
        playerCardUpgradePage.SetActive(cardUpgrade);
        playerTechTreePage.SetActive(techTree);                           // ★

        bool isMain = !(cardInventory || gacha || armyControl || cardUpgrade || techTree);
        mainUIPanel.SetActive(isMain);
        playerBaseMap.SetActive(isMain);
    }

    private void BackToMain() => TogglePages();   // 全 false → 返回主界面
    #endregion
}
