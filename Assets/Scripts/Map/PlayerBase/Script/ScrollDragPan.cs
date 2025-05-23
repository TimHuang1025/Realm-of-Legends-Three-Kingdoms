using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 给 UI Toolkit ScrollView 添加左键拖拽（鼠标 / 触摸）
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class ScrollDragPan : MonoBehaviour
{
    [SerializeField] string scrollViewName = "CampScroll";   // Inspector 可改
    ScrollView sv;
    Vector3  lastPos;
    int      dragId = -1;

    void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        sv = root.Q<ScrollView>(scrollViewName);
        if (sv == null) { Debug.LogError("找不到 ScrollView"); return; }

        sv.verticalScroller.style.display   = DisplayStyle.None;
        sv.horizontalScroller.style.display = DisplayStyle.None;

        var vp = sv.contentContainer;   // viewport

        /* PointerDown —— 开始拖拽 */
        vp.RegisterCallback<PointerDownEvent>(e =>
        {
            if (e.button != 0) return;               // 只管左键 / 第一指
            dragId  = e.pointerId;
            lastPos = e.position;
            vp.CapturePointer(dragId);
            // 不再 StopPropagation，免得上层丢事件
        }, TrickleDown.TrickleDown);

        /* PointerMove —— 拖动 */
        vp.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (e.pointerId != dragId) return;
            Vector3 delta = e.position - lastPos;
            lastPos = e.position;
            sv.scrollOffset -= new Vector2(delta.x, delta.y);
        }, TrickleDown.TrickleDown);

        /* PointerUp —— 结束拖拽 */
        vp.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        /* 如果指针捕获意外丢失（移出窗口等），也重置 */
        vp.RegisterCallback<PointerCaptureOutEvent>(_ => ResetDrag());
    }

    void OnPointerUp(PointerUpEvent e)
    {
        if (e.pointerId != dragId) return;
        (sv.contentContainer).ReleasePointer(dragId);
        ResetDrag();
    }

    void ResetDrag()
    {
        dragId = -1;
    }
}
