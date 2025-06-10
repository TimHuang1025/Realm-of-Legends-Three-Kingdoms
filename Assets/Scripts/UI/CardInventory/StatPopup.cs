// Assets/Scripts/UI/StatBreakdownPanel.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class StatBreakdownPanel : MonoBehaviour
{
    [Header("资源")]
    [SerializeField] VisualTreeAsset panelTpl;   // 上面的 Panel UXML
    [SerializeField] VisualTreeAsset rowTpl;     // 行 UXML（可空，用代码生成也行）

    /* ───────── 单例（可改成你的 UIManager 方式） ───────── */
    static StatBreakdownPanel _inst;
    public static System.Action OnPanelShown;
    void Awake() => _inst = this;

    /* ───────── 公共 API ───────── */
    public static void Show(string title, List<(string name, int val)> parts, Vector2 pos)
    {
        _inst.InternalShow(title, parts, pos);
    }
    public static void Hide() => _inst?.InternalHide();

    /* ───────── 私有 ───────── */
    VisualElement rootPanel;   // 当前面板

    void InternalShow(string title, List<(string name, int val)> parts, Vector2 pos)
    {
        if (rootPanel != null) InternalHide();           // 已开 → 先关

        var uiRoot = GetComponent<UIDocument>().rootVisualElement;
        rootPanel = panelTpl.CloneTree().Q<VisualElement>("RootPopup");

        uiRoot.Add(rootPanel);
        uiRoot.schedule.Execute(() =>
        {
            SetPopupSizeByVh(rootPanel, 20f);
            rootPanel.style.flexGrow = 0;
        }).ExecuteLater(0);

        /* 标题 */
        rootPanel.Q<Label>("TitleLbl").text = title;

        /* 列表 */
        var listRoot = rootPanel.Q<VisualElement>("ListRoot");
        foreach (var (name, val) in parts)
        {
            VisualElement row;
            if (rowTpl != null)
            {
                row = rowTpl.CloneTree();
                row.Q<Label>("NameLbl").text = name;
                SetFontByVh(row.Q<Label>("NameLbl"), 5f);
                row.Q<Label>("ValueLbl").text = $"+{val}";
                SetFontByVh(row.Q<Label>("ValueLbl"), 5f);
            }
            else
            {
                row = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row,
                                                    justifyContent = Justify.SpaceBetween }
                };
                row.Add(new Label(name));
                row.Add(new Label($"+{val}"));
            }
            listRoot.Add(row);
        }

        /* 定位 */
        rootPanel.style.left = pos.x;
        rootPanel.style.top = pos.y - 8; // 上方 8 像素
        // 如果你想永远在数字下方：  pos.y + atkLbl.layout.height + 8

        /* 点击任意处关闭 */
        uiRoot.RegisterCallback<PointerDownEvent>(OnAnyClick, TrickleDown.TrickleDown);
        OnPanelShown?.Invoke();
    }

    void OnAnyClick(PointerDownEvent evt)
    {
        // 点自己也算一次，照样关
        InternalHide();
        evt.StopPropagation();
    }

    void InternalHide()
    {
        if (rootPanel == null) return;
        rootPanel.RemoveFromHierarchy();
        rootPanel = null;

        // 解绑点击回调
        GetComponent<UIDocument>().rootVisualElement
            .UnregisterCallback<PointerDownEvent>(OnAnyClick, TrickleDown.TrickleDown);
    }
    void SetPopupSizeByVh(VisualElement panel, float vh)
    {
        var sizer = FindObjectOfType<VhSizer>();
        if (sizer == null) return;
        float px = sizer.VhToPx(vh);
        panel.style.width = px;
    }
    void SetFontByVh(VisualElement element, float vh)
    {
        var sizer = FindObjectOfType<VhSizer>();
        if (sizer == null) return;

        float px = sizer.VhToPx(vh);
        element.style.fontSize = px;
    }


}
