using UnityEngine;

public class LoadingPanelManager : MonoBehaviour
{
    public static LoadingPanelManager Instance { get; private set; }

    [SerializeField] Canvas loadingCanvas;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);      // 删掉重复体，不影响首个
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Hide();
    }
    public void Show() => loadingCanvas.enabled = true;
    public void Hide() => loadingCanvas.enabled = false;
}
