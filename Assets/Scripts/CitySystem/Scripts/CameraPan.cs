using UnityEngine;

/// <summary>
/// 2.5 D 透视相机 —— 拖拽平移 + 滚轮/双指缩放（放大→角度降低，缩小→角度升高）。
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraPanOrbit : MonoBehaviour
{
    [Header("拖拽速度 (世界单位/像素)")]
    public float panSpeed = 0.08f;

    [Header("缩放 (高度)")]
    public float minHeight = 12f;
    public float maxHeight = 60f;
    public float zoomSpeed = 10f;        // 鼠标滚轮缩放速度
    public float pinchZoomSpeed = 0.005f; // 双指捏合缩放速度（可根据机型微调）

    [Header("俯仰角 (°)")]
    public float minPitchDeg = 28f;
    public float maxPitchDeg = 75f;

    private Camera cam;
    private Plane  ground = new Plane(Vector3.up, Vector3.zero);

    // 拖拽
    private bool   dragging = false;
    private Vector3 dragStartWorld;

    // 固定 yaw
    private float yawFixed;

    // 双指捏合
    private bool  isPinching = false;
    private float lastPinchDistance;

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
        HandlePinchZoom();
        HandleMouseZoom();
    }

    // ---------------- 拖拽平移 ----------------
    private void HandleDrag()
    {
        // 记录起点
        if (Input.GetMouseButtonDown(0) && RayToGround(out dragStartWorld))
            dragging = true;

        // 拖拽时移动
        if (Input.GetMouseButton(0) && dragging && RayToGround(out var now))
            transform.position += (dragStartWorld - now) * panSpeed;

        // 松开结束拖拽
        if (Input.GetMouseButtonUp(0))
            dragging = false;
    }

    // ---------------- 双指捏合缩放 ----------------
    private void HandlePinchZoom()
    {
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            // 刚开始捏合，记录初始距离
            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                lastPinchDistance = Vector2.Distance(t0.position, t1.position);
                isPinching = true;
            }
            // 捏合进行中
            else if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
            {
                float currentDistance = Vector2.Distance(t0.position, t1.position);
                float delta = currentDistance - lastPinchDistance;

                // 按原滚轮逻辑：height -= scroll * speed * height
                float height = transform.position.y;
                height -= delta * pinchZoomSpeed * height;
                height = Mathf.Clamp(height, minHeight, maxHeight);

                // 计算插值并应用 pitch
                float t = (height - minHeight) / (maxHeight - minHeight);
                float pitch = Mathf.Lerp(minPitchDeg, maxPitchDeg, t);

                var pos = transform.position;
                pos.y = height;
                transform.position = pos;
                transform.rotation = Quaternion.Euler(pitch, yawFixed, 0f);

                lastPinchDistance = currentDistance;
            }
        }
        else
        {
            // 只有两指时才拦截滚轮缩放
            isPinching = false;
        }
    }

    // ---------------- 鼠标滚轮缩放 ----------------
    private void HandleMouseZoom()
    {
        if (isPinching) 
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 1e-5f) 
            return;

        float height = transform.position.y;
        height -= scroll * zoomSpeed * height;
        height = Mathf.Clamp(height, minHeight, maxHeight);

        float t = (height - minHeight) / (maxHeight - minHeight);
        float pitch = Mathf.Lerp(minPitchDeg, maxPitchDeg, t);

        var pos = transform.position;
        pos.y = height;
        transform.position = pos;
        transform.rotation = Quaternion.Euler(pitch, yawFixed, 0f);
    }

    // ---------------- 屏幕坐标 → 地面世界坐标 ----------------
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

    // ---------------- 初始时校准高度和俯仰 ----------------
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
