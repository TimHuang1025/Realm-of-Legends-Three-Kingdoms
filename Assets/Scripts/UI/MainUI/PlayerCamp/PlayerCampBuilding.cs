using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UIDocument))]
public class PlayerCampBuilding : MonoBehaviour
{
    // ① UXML 里 #CardInventoryBuilding 按钮的引用
    private Button cardInventoryBtn;

    void Awake()
    {
        // ② 取得 VisualTree 根
        var root = GetComponent<UIDocument>().rootVisualElement;

        // ③ 根据 name 查找按钮
        cardInventoryBtn = root.Q<Button>("CardInventoryBuilding");

        if (cardInventoryBtn == null)
        {
            Debug.LogError("找不到 #CardInventoryBuilding 按钮；请检查 UXML 名称是否一致");
            return;
        }

        // ④ 绑定点击回调
        cardInventoryBtn.clicked += OnClickCardInventory;
    }

    // ⑤ 点击后切到 CardInventory 场景
    private void OnClickCardInventory()
    {
        // 注意：CardInventory 场景必须已加入 File ▸ Build Settings ▸ Scenes In Build
        SceneManager.LoadScene("CardInventory");
    }
}
