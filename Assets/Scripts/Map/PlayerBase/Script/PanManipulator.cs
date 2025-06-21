// Assets/Scripts/Game/UI/PanManipulator.cs
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ScrollView 拖拽 + 捏合/滚轮缩放一体操控器（UI Toolkit）
/// </summary>
public sealed class PanManipulator : PointerManipulator
{
    /*──────── 可调参数 ────────*/
    public float minZoom       = 0.5f;   // 最小缩放
    public float maxZoom       = 3f;     // 最大缩放
    public float wheelStep     = 0.1f;   // 鼠标滚轮步长（±10%）
    public int   moveSkipFrame = 1;      // PointerMove 隔帧：0=每帧，1=隔1帧

    /*──────── 拖拽状态 ────────*/
    readonly ScrollView sv;
    readonly string     hotClass;
    readonly int        dragThreshold;   // 按 DPI 计算像素阈值
    int     activeId = -1;
    Vector2 startPos, startOffset;
    bool    isDragging, dragFirstFrame;
    VisualElement pressedVe;

    /*──────── 捏合状态 ────────*/
    int     pinchIdA = -1, pinchIdB = -1;
    float   pinchStartDist, pinchStartScale;
    Vector2 pinchMidStart;
    VisualElement content => sv.contentContainer;

    public PanManipulator(ScrollView scroll, string ignoreClass)
    {
        sv        = scroll;
        hotClass  = ignoreClass;
        dragThreshold = CalcDragThresholdPx();
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    /*──────── 注册 ────────*/
    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>      (OnDown, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerMoveEvent>      (OnMove, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerUpEvent>        (OnUp,   TrickleDown.TrickleDown);
        target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        target.RegisterCallback<WheelEvent>            (OnWheel,TrickleDown.TrickleDown);
    }
    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>      (OnDown, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerMoveEvent>      (OnMove, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerUpEvent>        (OnUp,   TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        target.UnregisterCallback<WheelEvent>            (OnWheel,TrickleDown.TrickleDown);
    }

    /*──────── PointerDown ────────*/
    void OnDown(PointerDownEvent e)
    {
        /*--- 记录双指 ---*/
        if (pinchIdA == -1)       pinchIdA = e.pointerId;
        else if (pinchIdB == -1)  pinchIdB = e.pointerId;

        /*--- 拖拽候选 ---*/
        if (e.button == (int)MouseButton.LeftMouse && CanStartPan(e))
        {
            activeId = e.pointerId;
            startPos = e.position;
            startOffset = sv.scrollOffset;
            isDragging = false;
            dragFirstFrame = true;
            pressedVe = e.target as VisualElement;
            target.CapturePointer(activeId);
        }

        /*--- 捏合初值 ---*/
        if (pinchIdA != -1 && pinchIdB != -1)
        {
            pinchStartDist  = DistanceBetweenPointers();
            pinchStartScale = content.transform.scale.x;
            pinchMidStart   = MidPointBetweenPointers();
        }
    }

    /*──────── PointerMove ────────*/
    void OnMove(PointerMoveEvent e)
    {
        if (target.panel == null) return;

        /*===== 捏合缩放 =====*/
        if (pinchIdA != -1 && pinchIdB != -1)
        {
            float dist = DistanceBetweenPointers();
            if (Mathf.Approximately(dist, 0)) return;

            float newScale = Mathf.Clamp(pinchStartScale * (dist / pinchStartDist), minZoom, maxZoom);
            ApplyZoom(newScale, MidPointBetweenPointers());
            e.StopPropagation();
            return;
        }

        /*===== 单指拖拽 =====*/
        if (e.pointerId != activeId) return;

        if (!dragFirstFrame && moveSkipFrame > 0 &&
            Time.frameCount % (moveSkipFrame + 1) != 0) return;

        Vector2 delta = (Vector2)e.position - startPos;

        if (!isDragging)
        {
            if (Mathf.Abs(delta.x) > dragThreshold || Mathf.Abs(delta.y) > dragThreshold)
            {
                isDragging = true;
                dragFirstFrame = false;
                if (pressedVe != null && pressedVe.HasPointerCapture(activeId))
                    pressedVe.ReleasePointer(activeId);
                target.CapturePointer(activeId);
                e.StopImmediatePropagation();
            }
            else return;
        }

        dragFirstFrame = false;
        sv.scrollOffset = ClampOffset(new Vector2(startOffset.x - delta.x,
                                                  startOffset.y - delta.y));
        e.StopPropagation();
    }

    /*──────── PointerUp / CaptureOut ────────*/
    void OnUp(PointerUpEvent e)
    {
        if (e.pointerId == activeId && target.HasPointerCapture(activeId))
            target.ReleasePointer(activeId);
        if (e.pointerId == activeId) activeId = -1;

        if (e.pointerId == pinchIdA) pinchIdA = -1;
        if (e.pointerId == pinchIdB) pinchIdB = -1;
        isDragging = false;
    }
    void OnCaptureOut(PointerCaptureOutEvent _) { activeId = -1; isDragging = false; pinchIdA = pinchIdB = -1; }

    /*──────── 鼠标滚轮 ────────*/
    void OnWheel(WheelEvent e)
    {
        float targetS = Mathf.Clamp(content.transform.scale.x - e.delta.y * wheelStep,
                                    minZoom, maxZoom);
        ApplyZoom(targetS, e.mousePosition);
        e.StopPropagation();
    }

    /*──────── 缩放实现 ────────*/
    void ApplyZoom(float newScale, Vector2 pivotScreen)
    {
        float prev = content.transform.scale.x;
        if (Mathf.Approximately(prev, newScale)) return;

        Vector2 pivotLocal = content.WorldToLocal(pivotScreen);
        float   factor     = newScale / prev;
        content.transform.scale = Vector3.one * newScale;

        Vector2 offset = (content.transform.position - (Vector3)pivotLocal) * (factor - 1f);
        content.transform.position -= (Vector3)offset;
    }

    /*──────── 工具 ────────*/
    bool CanStartPan(PointerDownEvent e) =>
        !(e.target is VisualElement ve && ve.ClassListContains(hotClass));

    int CalcDragThresholdPx()
    {
        float dpi = Screen.dpi > 1 ? Screen.dpi : 320f;       // fallback
        return Mathf.Clamp(Mathf.RoundToInt(dpi * 0.4f * 0.03937f), 4, 14);
    }

    Vector2 ClampOffset(Vector2 raw)
    {
        Vector2 vp = sv.contentViewport.layout.size;
        Vector2 ct = sv.contentContainer.layout.size;
        return new Vector2(
            Mathf.Clamp(raw.x, 0, Mathf.Max(0, ct.x - vp.x)),
            Mathf.Clamp(raw.y, 0, Mathf.Max(0, ct.y - vp.y)));
    }

    float DistanceBetweenPointers()
    {
        Vector2 a = PointerUtils.GetPos(pinchIdA);
        Vector2 b = PointerUtils.GetPos(pinchIdB);
        return Vector2.Distance(a, b);
    }
    Vector2 MidPointBetweenPointers()
    {
        Vector2 a = PointerUtils.GetPos(pinchIdA);
        Vector2 b = PointerUtils.GetPos(pinchIdB);
        return (a + b) * 0.5f;
    }
}

/*──────── Touch 工具：兼容旧/新输入系统 ────────*/
static class PointerUtils
{
#if UNITY_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    public static Vector2 GetPos(int id)
    {
        foreach (var t in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            if (t.touchId == id) return t.screenPosition;
        return Vector2.zero;
    }
#else
    public static Vector2 GetPos(int id)
    {
        foreach (var t in Input.touches)
            if (t.fingerId == id) return t.position;
        return Vector2.zero;
    }
#endif
}
