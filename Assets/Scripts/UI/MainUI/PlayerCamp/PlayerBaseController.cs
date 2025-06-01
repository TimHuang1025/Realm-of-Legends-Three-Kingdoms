using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 玩家大本营 / 主界面控制器：
/// 1. 进入游戏时隐藏 CardInventoryPage，显示 MainUI & PlayerBaseMap.
/// 2. 点击 “CardInventoryBuilding” 按钮时：
///    - 关闭 MainUI & PlayerBaseMap
///    - 打开 CardInventoryPage
/// 3. CardInventoryPage 里点击 “返回” 时，调用 HideCardInventoryPage() 恢复主界面。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PlayerBaseController : MonoBehaviour
{
    /* Inspector 拖入 */
    [SerializeField] private GameObject mainUIPanel;
    [SerializeField] private GameObject playerBaseMap;
    [SerializeField] private GameObject cardInventoryPage;

    private Button cardInventoryBtn;

    /*———— 启动时默认状态 ————*/
    void Awake()
    {
        cardInventoryPage.SetActive(false);
        mainUIPanel.SetActive(true);
        playerBaseMap.SetActive(true);
    }

    /*———— 每次重新启用时重新取 root 并绑定事件 ————*/
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // 重新拿一次按钮（新的 Panel → 新的元素实例）
        cardInventoryBtn = root.Q<Button>("CardInventoryBuilding");
        if (cardInventoryBtn != null)
            cardInventoryBtn.clicked += ShowCardInventoryPage;
    }

    /*———— 对应卸载，避免重复绑定 ————*/
    void OnDisable()
    {
        if (cardInventoryBtn != null)
            cardInventoryBtn.clicked -= ShowCardInventoryPage;
    }

    /*———— 打开 / 关闭 逻辑保持不变 ————*/
    void ShowCardInventoryPage()
    {
        cardInventoryPage.SetActive(true);
        mainUIPanel.SetActive(false);
        playerBaseMap.SetActive(false);
    }

    public void HideCardInventoryPage()
    {
        cardInventoryPage.SetActive(false);
        mainUIPanel.SetActive(true);
        playerBaseMap.SetActive(true);
    }
}

