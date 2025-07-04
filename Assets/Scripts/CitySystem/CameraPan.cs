// ──────────────────────────────────────────────────────────────
// Assets/Scripts/Game/CameraPanOrbit.cs
// ──────────────────────────────────────────────────────────────
using UnityEngine;

/// <summary>
/// 2.5 D 透视相机 —— 拖拽平移 + 滚轮缩放（放大→角度降低，缩小→角度升高）。
/// ─────────────────────────────────────────────────────────
/// 左键按下   → 记录鼠标落点；拖动 → 相机随地面平移；松开 → 结束拖拽。  
/// 鼠标滚轮   → 缩放：调整相机高度，同时按比例改变俯仰角。  
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraPanOrbit : MonoBehaviour
{
    [Header("拖拽速度 (世界单位/像素)")]
    public float panSpeed = 0.08f;

    [Header("缩放 (高度)")]
    public float minHeight = 12f;
    public float maxHeight = 60f;
    public float zoomSpeed = 10f;

    [Header("俯仰角 (°)")]
    public float minPitchDeg = 28f;
    public float maxPitchDeg = 75f;

    private Camera cam;
    private Plane  ground         = new Plane(Vector3.up, Vector3.zero);
    private bool   dragging       = false;
    private Vector3 dragStartWorld;
    private float  yawFixed;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = false;
        yawFixed = transform.eulerAngles.y;
        ClampHeightAndPitch();
    }

    void Update()
    {
        HandleDrag();
        HandleZoom();
    }

    // ---------------- 拖拽平移 ----------------
    private void HandleDrag()
    {
        // 鼠标左键按下：记录起点世界坐标
        if (Input.GetMouseButtonDown(0) && RayToGround(out dragStartWorld))
            dragging = true;

        // 拖动中：相机移动差值
        if (Input.GetMouseButton(0) && dragging && RayToGround(out var now))
            transform.position += (dragStartWorld - now) * panSpeed;

        // 松开结束拖拽
        if (Input.GetMouseButtonUp(0))
            dragging = false;
    }

    // ---------------- 缩放 + 角度联动 ----------------
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 1e-5f) return;

        // 高度调整
        float height = transform.position.y;
        height -= scroll * zoomSpeed * height;
        height = Mathf.Clamp(height, minHeight, maxHeight);

        // 俯仰角插值
        float t = (height - minHeight) / (maxHeight - minHeight);
        float pitch = Mathf.Lerp(minPitchDeg, maxPitchDeg, t);

        // 应用
        Vector3 pos = transform.position;
        pos.y = height;
        transform.position = pos;
        transform.rotation = Quaternion.Euler(pitch, yawFixed, 0f);
    }

    // ---------------- 工具：屏幕坐标 → 地面世界坐标 ----------------
    private bool RayToGround(out Vector3 hitPoint)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (ground.Raycast(ray, out float enter))
        {
            hitPoint = ray.GetPoint(enter);
            return true;
        }
        hitPoint = Vector3.zero;
        return false;
    }

    // ---------------- 工具：初始时校准高度和俯仰 ----------------
    private void ClampHeightAndPitch()
    {
        Vector3 p = transform.position;
        p.y = Mathf.Clamp(p.y, minHeight, maxHeight);
        transform.position = p;

        float t = (p.y - minHeight) / (maxHeight - minHeight);
        float pitch = Mathf.Lerp(minPitchDeg, maxPitchDeg, t);
        transform.rotation = Quaternion.Euler(pitch, yawFixed, 0f);
    }
}
