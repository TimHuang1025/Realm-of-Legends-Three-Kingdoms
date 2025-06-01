using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ScrollDragPan : MonoBehaviour
{
    [SerializeField] private string scrollViewName = "CampScroll";

    static readonly Dictionary<string, Vector2> SavedOffset = new();

    ScrollView    sv;
    VisualElement vp;
    int           dragId = -1;
    Vector2       lastPos;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        sv = root.Q<ScrollView>(scrollViewName);
        if (sv == null) { Debug.LogError($"找不到 ScrollView: {scrollViewName}"); return; }

        // ←─────────① 推迟 1 帧恢复 ─────────→
        if (SavedOffset.TryGetValue(scrollViewName, out var off))
            sv.schedule.Execute(() => sv.scrollOffset = off).ExecuteLater(1);
        // --------------------------------------

        sv.verticalScroller.style.display   = DisplayStyle.None;
        sv.horizontalScroller.style.display = DisplayStyle.None;

        vp     = sv.contentContainer;
        dragId = -1;

        vp.RegisterCallback<PointerDownEvent>(OnPointerDown,  TrickleDown.TrickleDown);
        vp.RegisterCallback<PointerMoveEvent>(OnPointerMove,  TrickleDown.TrickleDown);
        vp.RegisterCallback<PointerUpEvent>(  OnPointerUp,    TrickleDown.TrickleDown);
        vp.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    void OnDisable()
    {
        // ←─────────② 保存当前位置 ─────────→
        if (sv != null) SavedOffset[scrollViewName] = sv.scrollOffset;
        // --------------------------------------

        if (vp != null)
        {
            if (dragId != -1) vp.ReleasePointer(dragId);

            vp.UnregisterCallback<PointerDownEvent>(OnPointerDown,  TrickleDown.TrickleDown);
            vp.UnregisterCallback<PointerMoveEvent>(OnPointerMove,  TrickleDown.TrickleDown);
            vp.UnregisterCallback<PointerUpEvent>(  OnPointerUp,    TrickleDown.TrickleDown);
            vp.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        sv = null; vp = null; dragId = -1;
    }

    /*──────── Pointer 事件保持不变 ────────*/
    void OnPointerDown(PointerDownEvent e)
    {
        if (e.button != 0) return;
        dragId  = e.pointerId;
        lastPos = (Vector2)e.position;
        vp.CapturePointer(dragId);
    }

    void OnPointerMove(PointerMoveEvent e)
    {
        if (e.pointerId != dragId) return;
        Vector2 delta = (Vector2)e.position - lastPos;
        lastPos = (Vector2)e.position;
        sv.scrollOffset -= delta;
    }

    void OnPointerUp(PointerUpEvent e)
    {
        if (e.pointerId != dragId) return;
        vp.ReleasePointer(dragId);
        dragId = -1;
    }

    void OnPointerCaptureOut(PointerCaptureOutEvent _) => dragId = -1;
}
