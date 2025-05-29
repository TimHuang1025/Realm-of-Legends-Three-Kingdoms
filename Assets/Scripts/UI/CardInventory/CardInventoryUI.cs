using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UIDocument))]
public class CardInventoryUI : MonoBehaviour
{
    Button returnBtn;

    void Awake()
    {
        // ① 找到 UXML 根
        var root = GetComponent<UIDocument>().rootVisualElement;

        // ② 拿到按钮（UXML 里 name="ReturnBtn"）
        returnBtn = root.Q<Button>("ReturnBtn");
        if (returnBtn == null)
        {
            Debug.LogError("CardInventoryUI: 找不到 <Button name=\"ReturnBtn\">");
            return;
        }

        // ③ 绑定点击事件
        returnBtn.clicked += OnClickReturn;
    }

    /*──────── 点击返回 ────────*/
    void OnClickReturn()
    {
        // 若本脚本挂载的物体被 DontDestroyOnLoad 过，这里销毁以免跟到下个场景
        Destroy(gameObject);

        // 加载主 UI 场景（需已加入 Build Settings）
        SceneManager.LoadScene("MainUI", LoadSceneMode.Single);
    }
}
