// PopupManager.cs  ——  Unity · C# 11
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PopupManager : MonoBehaviour
{
    /*── Inspector 资源 ──*/
    [SerializeField] VisualTreeAsset popupTpl;
    [SerializeField] PanelSettings popupPanelSettings;
    [SerializeField] VhSizerConfig vhsizerConfig;

    /*── 运行期字段 ──*/
    static PopupManager inst;

    UIDocument popupDoc;
    VisualElement docRoot;
    VhSizer vhSizer;          // ★ 保存 VhSizer 引用

    /*───────── 生命周期 ─────────*/
    void Awake()
    {
        if (inst != null) { Destroy(gameObject); return; }

        inst = this;
        DontDestroyOnLoad(gameObject);
        InitPopupPanel();
    }

    /*───────── 初始化专用 Panel ─────────*/
    void InitPopupPanel()
    {
        var go = new GameObject("PopupCanvas", typeof(UIDocument));
        go.transform.SetParent(transform, false);

        popupDoc = go.GetComponent<UIDocument>();
        popupDoc.panelSettings = popupPanelSettings;
        popupDoc.sortingOrder = 1000;

        // ① 生成并缓存 VhSizer
        vhSizer = go.AddComponent<VhSizer>();
        vhSizer.config = vhsizerConfig;

        docRoot = popupDoc.rootVisualElement;
        docRoot.style.position = Position.Absolute;
        docRoot.style.left = docRoot.style.top = 0;
        docRoot.style.width = Length.Percent(100);
        docRoot.style.height = Length.Percent(100);

        // ② 下一帧执行一次缩放（第一次 root 还是空）
        StartCoroutine(ApplyNextFrame());
    }

    IEnumerator ApplyNextFrame()
    {
        yield return null;
        vhSizer?.Apply();
    }

    /*────────── 公共接口 ──────────*/
    public static void Show(string msg, float sec = 0f)
        => inst?.CreatePopup("提示", msg, sec);

    public static void Show(string title, string msg, float sec = 0f)
        => inst?.CreatePopup(title, msg, sec);

    /*────────── 生成弹窗 ──────────*/
    void CreatePopup(string title, string msg, float autoClose)
    {
        if (popupTpl == null || docRoot == null) return;

        // 1) CloneTree 并放进 Panel
        var root = popupTpl.CloneTree();
        docRoot.Add(root);
        root.BringToFront();

        // 2) 文案 + 关闭逻辑
        root.Q<Label>("Popuptitle")?.SetText(title);
        var txtLbl = root.Q<Label>("Popuptext");
        if (txtLbl != null)
            txtLbl.SetText(msg);
        txtLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        var yesBtnShadow = root.Q<VisualElement>("YesBtnShadow");
        yesBtnShadow.style.display = DisplayStyle.None;      // 强制显示


        void Close() => root.RemoveFromHierarchy();
        root.Q<VisualElement>("BlackSpace")?.RegisterCallback<ClickEvent>(_ => Close());
        root.Q<VisualElement>("ClosePanel")?.RegisterCallback<ClickEvent>(_ => Close());
        var btn = root.Q<Button>("CloseBtn2");
        if (btn != null) btn.clicked += Close;

        if (autoClose > 0f)
#if UNITY_2022_2_OR_NEWER
            root.schedule.Execute(Close).StartingIn((long)(autoClose * 1000));
#else
            root.schedule.Execute(Close).ExecuteLater((long)(autoClose * 1000));
#endif

        // 3) 弹窗加入后 ⇒ 再执行一次自适应
        vhSizer?.Apply();
    }
    public static void ShowConfirm(string message,
                               System.Action onYes,
                               System.Action onNo,
                               string yesText = "确定",
                               string noText  = "取消")
{
    inst?.CreateConfirmPopup("提示", message, onYes, onNo, yesText, noText);
}


void CreateConfirmPopup(string title, string msg,
                        System.Action onYes, System.Action onNo,
                        string yesText, string noText)
{
    var root = popupTpl.CloneTree();
    docRoot.Add(root); root.BringToFront();

    // ─ 文案 ─
    root.Q<Label>("Popuptitle")?.SetText(title);
    root.Q<Label>("Popuptext") ?.SetText(msg);

    // ─ Yes 按钮 ─
    var yesBtn = root.Q<Button>("YesBtn");
    if (yesBtn != null)
    {
        var yesBtnShadow = root.Q<VisualElement>("YesBtnShadow");
        yesBtnShadow.style.display = DisplayStyle.Flex;      // 强制显示
        yesBtn.style.display = DisplayStyle.Flex;      // 强制显示
        yesBtn.text = yesText;                         // ★ 自定义文字
        yesBtn.clicked += () => { Close(); onYes?.Invoke(); };
    }

    // ─ No 按钮 ─
    var noBtn = root.Q<Button>("CloseBtn2");
    if (noBtn != null)
    {
        noBtn.text = noText;                           // ★ 自定义文字
        noBtn.clicked += () => { Close(); onNo?.Invoke(); };
    }

    // 点击黑幕 = 取消
    root.Q<VisualElement>("BlackSpace")
        ?.RegisterCallback<ClickEvent>(_ => { Close(); onNo?.Invoke(); });

    root.Q<Button>("ClosePanel")
        ?.RegisterCallback<ClickEvent>(_ => { Close(); onNo?.Invoke(); });

    vhSizer?.Apply();                                  // 适配

    void Close() => root.RemoveFromHierarchy();
}



}

/* 小工具：Label 快捷赋值 */
static class VEExt
{
    public static void SetText(this Label lbl, string txt) { if (lbl != null) lbl.text = txt; }
}
