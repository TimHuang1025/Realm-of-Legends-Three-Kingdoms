// Assets/Scripts/Game/UI/MainMenuController.cs
using UnityEngine;
using UnityEngine.UIElements;
using UIExt;                 // 如果没有 Bounce 扩展，可删掉

/// <summary>
/// 主菜单 → 设定页切换控制器  
/// • 点击 ProfileIcon 打开 SettingsPanel  
/// • 点击 SettingsPanel 里的 CloseBtn 返回主界面  
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [Header("面板引用（Inspector 拖）")]
    [SerializeField] private GameObject mainMenuRoot;   // 主界面整体 GameObject
    [SerializeField] private GameObject settingsPanel;  // SettingsPanel (带 UIDocument)

    private string closeBtnName = "CloseBtn";

    /* ───── 私有缓存 ───── */
    private VisualElement profileIcon;
    private Button        closeBtn;

    void Awake()
    {
        // 启动时只显示主界面
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuRoot  != null) mainMenuRoot.SetActive(true);
    }

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        /* —— 头像按钮 —— */
        profileIcon = root.Q<VisualElement>("ProfileIcon");
        if (profileIcon == null)
        {
            Debug.LogError("[MainMenuController] 找不到名为 “ProfileIcon” 的元素，请确认 UXML 节点 name");
            return;
        }

        // 让整块区域可点 & 动效
        profileIcon.pickingMode = PickingMode.Position;
        profileIcon.Bounce(0.9f, 0.08f);              // 没用到可删
        profileIcon.RegisterCallback<ClickEvent>(OnProfileClicked);
    }

    void OnDisable()
    {
        if (profileIcon != null)
            profileIcon.UnregisterCallback<ClickEvent>(OnProfileClicked);
        if (closeBtn != null)
            closeBtn.clicked -= HideSettingsPanel;
    }

    /* ───── 事件 ───── */
    void OnProfileClicked(ClickEvent _)
    {
        ShowSettingsPanel();
    }

    public void ShowSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (mainMenuRoot  != null) mainMenuRoot.SetActive(false);
    }

    public void HideSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuRoot  != null) mainMenuRoot.SetActive(true);
    }
}
