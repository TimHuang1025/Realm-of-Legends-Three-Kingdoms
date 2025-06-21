// Assets/Scripts/Game/UI/SettingsPanelController.cs
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SettingsPanelController : MonoBehaviour
{
    [Header("主界面控制器 (拖 MainMenuController)")]
    [SerializeField] MainMenuController mainMenuCtrl;

    string closeBtnName = "CloseBtn";

    Button closeBtn;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        closeBtn = root.Q<Button>(closeBtnName);
        if (closeBtn == null)
        {
            Debug.LogError($"[SettingsPanelController] 找不到按钮 {closeBtnName}");
            return;
        }

        closeBtn.clicked += ReturnToMainMenu;   // 直接返回，不确认
    }

    void OnDisable()
    {
        if (closeBtn != null)
            closeBtn.clicked -= ReturnToMainMenu;
    }

    /*── 直接返回主界面 ──*/
    void ReturnToMainMenu()
    {
        if (mainMenuCtrl != null)
            mainMenuCtrl.HideSettingsPanel();
        else
            Debug.LogError("[SettingsPanelController] mainMenuCtrl 未赋值");
    }
}
