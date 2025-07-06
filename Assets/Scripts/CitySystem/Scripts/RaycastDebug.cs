using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UiRaycastDebug : MonoBehaviour
{
    GraphicRaycaster gr;
    void Awake() { gr = GetComponentInParent<GraphicRaycaster>(); }

    void Update()
    {
#if UNITY_EDITOR   // PC 也能验证
        if (!Input.GetMouseButtonDown(0)) return;
        Vector2 pos = Input.mousePosition;
        int id      = -1;
#else
        if (Input.touchCount == 0 || Input.GetTouch(0).phase != TouchPhase.Began) return;
        Vector2 pos = Input.GetTouch(0).position;
        int id      = Input.GetTouch(0).fingerId;
#endif
        var list = new System.Collections.Generic.List<RaycastResult>();
        PointerEventData ped = new PointerEventData(EventSystem.current) { position = pos };
        EventSystem.current.RaycastAll(ped, list);
        Debug.Log($"[UiRaycastDebug] fingerId={id}, screen={pos}, hits={list.Count}");
        foreach (var r in list)
            Debug.Log($"   ↳ {r.gameObject.name}  (dist {r.distance:F3}) layer={LayerMask.LayerToName(r.gameObject.layer)}");
    }
}
