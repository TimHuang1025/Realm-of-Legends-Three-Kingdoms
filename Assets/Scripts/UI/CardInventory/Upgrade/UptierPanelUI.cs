using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UptierPanelController : MonoBehaviour
{
    [SerializeField] VhSizer vhSizer;

    UIDocument doc;

    void Awake()
    {
        doc = GetComponent<UIDocument>();
        gameObject.SetActive(false);  // 默认隐藏（注：Awake 里关没问题）
    }

    void OnEnable()
    {
        var root       = doc.rootVisualElement;
        var closeBtn   = root.Q<Button>("ClosePanel");
        var closeBtn2  = root.Q<Button>("CloseBtn2");
        var blackspace = root.Q<VisualElement>("BlackSpace");

        if (closeBtn   != null) closeBtn.clicked   += Close;
        if (closeBtn2  != null) closeBtn2.clicked  += Close;
        if (blackspace != null)
            blackspace.RegisterCallback<ClickEvent>(_ => Close());
    }

    /* ---------- 对外接口 ---------- */
    public void Open()
    {
        gameObject.SetActive(true);            // ① 先激活自己
        StartCoroutine(AfterEnableRoutine());  // ② 再开协程
    }

    public void Close() => gameObject.SetActive(false);

    IEnumerator AfterEnableRoutine()
    {
        yield return null;     // 等 1 帧布局
        vhSizer?.Apply();      // 统一刷新
    }
}
