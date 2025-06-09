// Assets/Scripts/Game/UI/PanManipulator.cs
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PanManipulator : PointerManipulator
{
    readonly ScrollView sv;
    readonly string hotClass;
    const float DRAG_THRESHOLD = 6f;   // px

    int activeId = -1;
    Vector2 startPos;
    Vector2 startOffset;
    bool isDragging;
    VisualElement pressedVe;

    public PanManipulator(ScrollView scrollView, string ignoreClass)
    {
        sv = scrollView;
        hotClass = ignoreClass;
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    /*──────────────── 注册 / 注销 ────────────────*/
    protected override void RegisterCallbacksOnTarget()
    {
        // 用 TrickleDown 先拿事件，才能在按钮收到之前判断阈值
        target.RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
    }
    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
    }

    /*──────────────── Pointer 逻辑 ────────────────*/
    void OnDown(PointerDownEvent e)
    {
        if (e.button != (int)MouseButton.LeftMouse) return;
        if (!CanStartPan(e)) return;

        activeId = e.pointerId;
        pressedVe   = e.target as VisualElement;
        startPos = (Vector2)e.position;
        startOffset = sv.scrollOffset;
        isDragging = false;             // 先视为点击候选

        // 不 CapturePointer，允许按钮继续接收 Down，待阈值触发后再夺取
    }

    void OnMove(PointerMoveEvent e)
    {
        if (e.pointerId != activeId) return;

        Vector2 cur = (Vector2)e.position;
        Vector2 delta = cur - startPos;

        // 未进入拖拽模式，判断是否超过阈值
        if (!isDragging)
        {
            if (delta.sqrMagnitude < DRAG_THRESHOLD * DRAG_THRESHOLD)
                return; // 还在点击判定范围内

            // —— 超阈值：切换为拖拽模式 ——
            isDragging = true;
            if (pressedVe != null && pressedVe.HasPointerCapture(activeId))
                pressedVe.ReleasePointer(activeId);
            target.CapturePointer(activeId);     // 抢指针
            e.StopImmediatePropagation();        // 阻断按钮后续 Move/Up
        }

        // 已是拖拽：平移 scrollOffset
        Vector2 raw = new Vector2(
            startOffset.x - delta.x,     // 横向：鼠标右拖地图向右
            startOffset.y - delta.y);    // 纵向：鼠标下拖地图向下（若相反改成 +delta.y）

        sv.scrollOffset = ClampOffset(raw);
        e.StopImmediatePropagation();
    }

    void OnUp(PointerUpEvent e)
    {
        if (e.pointerId != activeId) return;

        if (target.HasPointerCapture(activeId))
            target.ReleasePointer(activeId);     // 释放给系统

        // isDragging==false 时认为是一次点击，按钮 Up 事件会继续触发
        activeId = -1;
        isDragging = false;
    }

    /*──────────────── 工具 ────────────────*/
    bool CanStartPan(PointerDownEvent e)
    {
        // 只排除你明确标了 hotClass 的元素（比如 minimap、UI 面板等）
        if (e.target is VisualElement ve && ve.ClassListContains(hotClass))
            return false;

        // Button 不再排除，让它也能先当“点击候选”再根据位移切换成拖拽
        return true;
    }

    Vector2 ClampOffset(Vector2 raw)
    {
        Vector2 vp = sv.contentViewport.layout.size;
        Vector2 ct = sv.contentContainer.layout.size;

        float maxX = Mathf.Max(0, ct.x - vp.x);
        float maxY = Mathf.Max(0, ct.y - vp.y);

        raw.x = Mathf.Clamp(raw.x, 0, maxX);
        raw.y = Mathf.Clamp(raw.y, 0, maxY);
        return raw;
    }
    void OnCaptureOut(PointerCaptureOutEvent _)
    {
        // 无论因为什么丢失捕获，都重置状态，避免按钮保持 Pressed
        isDragging = false;
        activeId   = -1;
    }
}
