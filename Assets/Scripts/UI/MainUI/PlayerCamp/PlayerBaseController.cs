// Assets/Scripts/Game/UI/PlayerBaseController.cs
using UnityEngine;
using UnityEngine.UIElements;
using UIExt;  // 你的 Bounce 扩展方法所在命名空间

[RequireComponent(typeof(UIDocument))]
public class PlayerBaseController : MonoBehaviour
{
    /*──────── Inspector 拖入 ────────*/
    [Header("主界面与地图")]
    [SerializeField] private GameObject mainUIPanel;
    [SerializeField] private GameObject playerBaseMap;

    [Header("各功能页面")]
    [SerializeField] private GameObject cardInventoryPage;
    [SerializeField] private GameObject gachaPage;
    [SerializeField] private GameObject armyControlPage;
    [SerializeField] private GameObject playerCardUpgradePage;   // ★ 新增：主将府页面

    /*──────── 建筑按钮 ────────*/
    private Button cardInventoryBtn;
    private Button armyControlBtn;
    private Button playerCardUpgradeBtn;                         // ★ 新增：主将府按钮

    /*──────── 初始化 ────────*/
    void Awake()
    {
        // 默认只开主界面 + 地图
        cardInventoryPage.SetActive(false);
        gachaPage.SetActive(false);
        armyControlPage.SetActive(false);
        playerCardUpgradePage.SetActive(false);   
        mainUIPanel.SetActive(true);
        playerBaseMap.SetActive(true);
    }

    /*──────── 注册按钮 ────────*/
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // ——— 卡牌仓库 ———
        cardInventoryBtn = root.Q<Button>("CardInventoryBuilding");
        if (cardInventoryBtn != null)
        {
            PrepareBuildingButton(cardInventoryBtn);
            cardInventoryBtn.clicked += ShowCardInventoryPage;
        }
        else Debug.LogError("找不到 CardInventoryBuilding 按钮");

        // ——— 军团指挥 ———
        armyControlBtn = root.Q<Button>("ArmyControlBuilding");
        if (armyControlBtn != null)
        {
            PrepareBuildingButton(armyControlBtn);
            armyControlBtn.clicked += ShowArmyControlPage;
        }
        else Debug.LogError("找不到 ArmyControlBuilding 按钮");

        // ——— 主将府（技能/等级升级）———
        playerCardUpgradeBtn = root.Q<Button>("PlayerCardBuilding");   // ★ 新增
        if (playerCardUpgradeBtn != null)
        {
            PrepareBuildingButton(playerCardUpgradeBtn);
            playerCardUpgradeBtn.clicked += ShowPlayerCardUpgradePage;
        }
        else Debug.LogError("找不到 PlayerCardBuilding 按钮");
    }

    void OnDisable()
    {
        if (cardInventoryBtn      != null) cardInventoryBtn.clicked      -= ShowCardInventoryPage;
        if (armyControlBtn        != null) armyControlBtn.clicked        -= ShowArmyControlPage;
        if (playerCardUpgradeBtn  != null) playerCardUpgradeBtn.clicked  -= ShowPlayerCardUpgradePage; // ★ 新增
    }

    /*──────── 公用：给建筑按钮做统一设置 ────────*/
    void PrepareBuildingButton(Button btn)
    {
        btn.pickingMode = PickingMode.Position;                         // 整块区域可点
        btn.style.backgroundColor = new StyleColor(new Color(0,0,0,0)); // 透明背景，拦截点击
        btn.Bounce(0.9f, 0.08f);                                        // 动效
    }

    /*──────── 页面切换逻辑 ────────*/
    void ShowCardInventoryPage()
    {
        cardInventoryPage.SetActive(true);
        gachaPage.SetActive(false);
        armyControlPage.SetActive(false);
        playerCardUpgradePage.SetActive(false);   // ★ 新增
        mainUIPanel.SetActive(false);
        playerBaseMap.SetActive(false);
    }

    public void HideCardInventoryPage()
    {
        cardInventoryPage.SetActive(false);
        mainUIPanel.SetActive(true);
        playerBaseMap.SetActive(true);
    }

    public void ShowGachaPage()
    {
        gachaPage.SetActive(true);
        cardInventoryPage.SetActive(false);
        armyControlPage.SetActive(false);
        playerCardUpgradePage.SetActive(false);   // ★ 新增
        mainUIPanel.SetActive(false);
        playerBaseMap.SetActive(false);
    }

    public void HideGachaPage()
    {
        gachaPage.SetActive(false);
        // 返回哪里按需求，这里默认回主界面
        mainUIPanel.SetActive(true);
        playerBaseMap.SetActive(true);
    }

    public void ShowArmyControlPage()
    {
        armyControlPage.SetActive(true);
        cardInventoryPage.SetActive(false);
        gachaPage.SetActive(false);
        playerCardUpgradePage.SetActive(false);   // ★ 新增
        mainUIPanel.SetActive(false);
        playerBaseMap.SetActive(false);
    }

    public void HideArmyControlPage()
    {
        armyControlPage.SetActive(false);
        mainUIPanel.SetActive(true);
        playerBaseMap.SetActive(true);
    }

    /*──────── 主将府页面 ────────*/
    void ShowPlayerCardUpgradePage()               
    {
        playerCardUpgradePage.SetActive(true);
        cardInventoryPage.SetActive(false);
        gachaPage.SetActive(false);
        armyControlPage.SetActive(false);
        mainUIPanel.SetActive(false);
        playerBaseMap.SetActive(false);
    }

    public void HidePlayerCardUpgradePage()        
    {
        playerCardUpgradePage.SetActive(false);
        mainUIPanel.SetActive(true);
        playerBaseMap.SetActive(true);
    }
}
