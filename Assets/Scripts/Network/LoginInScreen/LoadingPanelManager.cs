using UnityEngine;

public class LoadingPanelManager : MonoBehaviour
{
    // 单例方便全局调用
    public static LoadingPanelManager Instance { get; private set; }

    // 把 Canvas_Loading 拖进来
    [SerializeField] Canvas loadingCanvas;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);   // 切场景也能继续用
        }
        else { Destroy(gameObject); return; }

        Hide();                              // 开局默认隐藏
    }

    public void Show() => loadingCanvas.enabled = true;
    public void Hide() => loadingCanvas.enabled = false;
}