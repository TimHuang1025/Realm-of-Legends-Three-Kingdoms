using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingClickDetectorMinimal : MonoBehaviour
{
    [Header("1. 基础设置")]
    [Tooltip("射线用的摄像机")]
    [SerializeField] private Camera cam;
    [Tooltip("建筑所在的 Layer")]
    [SerializeField] private LayerMask buildingMask;
    [Tooltip("只响应此类型的建筑 SO")]
    [SerializeField] private BuildingTypeSO townHallSO;

    [Header("2. 点击/拖动判定")]
    [Tooltip("点击到建筑中心的最大水平距离（米）")]
    [SerializeField] private float clickRadius   = 0.6f;
    [Tooltip("超过此像素距离算拖动")]
    [SerializeField] private float dragThreshold = 5f;
    [Tooltip("最大射线距离")]
    [SerializeField] private float maxRayDistance = 800f;

    [Header("3. World Space UI")]
    [Tooltip("FloatingBuildingUI Prefab，内部 Canvas RenderMode=World Space")]
    [SerializeField] private FloatingBuildingUI floatingUIPrefab;
    [Tooltip("UI 在建筑顶端的世界 Y 偏移（米）")]
    [SerializeField] private float uiVerticalOffset = 2f;

    [Header("4. 移动逻辑")]
    [Tooltip("场景中负责放置与移动的 BuildingPlacer 实例")]
    [SerializeField] private BuildingPlacer placer;

    [Header("5. 面板切换")]
    [SerializeField] private GameObject playerCardPage;
    [SerializeField] private GameObject mainUIPanel;

    // —— 运行时缓存 ——————————————————
    private FloatingBuildingUI currentUI;
    private Building           candidateBuilding;
    private Vector2            pressPos;
    private bool               isDragging;
    private int                activeFingerId = -1;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (playerCardPage) playerCardPage.SetActive(false);
        if (placer == null) placer = FindObjectOfType<BuildingPlacer>();
    }

    private void Update()
    {
        // 触摸优先
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)      OnPress(t.position, t.fingerId);
            if (t.phase == TouchPhase.Moved ||
                t.phase == TouchPhase.Stationary) OnDrag(t.position, t.fingerId);
            if (t.phase == TouchPhase.Ended   ||
                t.phase == TouchPhase.Canceled)  OnRelease();
        }
        else
        {
            if (Input.GetMouseButtonDown(0)) OnPress(Input.mousePosition, -1);
            if (Input.GetMouseButton(0))     OnDrag(Input.mousePosition, -1);
            if (Input.GetMouseButtonUp(0))   OnRelease();
        }

        // Esc 收起卡页
        if (Input.GetKeyDown(KeyCode.Escape)
            && playerCardPage != null
            && playerCardPage.activeSelf)
        {
            ClosePlayerCardPage();
        }
    }

    private void OnPress(Vector2 screenPos, int fingerId)
    {
        isDragging        = false;
        candidateBuilding = null;
        activeFingerId    = fingerId;
        pressPos          = screenPos;

        // 点在 UI 上就忽略（这样按钮才有机会接收事件）
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(fingerId))
            return;

        // 只有点击到场景时才删旧 UI
        DestroyCurrentUI();

        // 射线检测建筑
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, maxRayDistance, buildingMask))
            return;

        var b = hit.collider.GetComponentInParent<Building>();
        if (b == null || b.type != townHallSO) return;

        // 水平距离过滤
        Plane ground = new Plane(Vector3.up, b.transform.position);
        if (!ground.Raycast(ray, out float enter)) return;
        Vector3 gh = ray.GetPoint(enter);
        if (Vector2.Distance(
                new Vector2(gh.x, gh.z),
                new Vector2(b.transform.position.x, b.transform.position.z))
            > clickRadius)
            return;

        // 通过检测：弹 UI
        candidateBuilding = b;
        ShowUIAboveBuilding(b);
    }

    private void OnDrag(Vector2 pos, int fingerId)
    {
        if (fingerId != activeFingerId || candidateBuilding == null || isDragging)
            return;
        if ((pos - pressPos).sqrMagnitude > dragThreshold * dragThreshold)
        {
            isDragging = true;
            DestroyCurrentUI();
            candidateBuilding = null;
        }
    }

    private void OnRelease()
    {
        candidateBuilding = null;
        activeFingerId    = -1;
    }

    private void ShowUIAboveBuilding(Building b)
    {
        DestroyCurrentUI();
        if (floatingUIPrefab == null) return;

        // 1. 实例化
        currentUI = Instantiate(floatingUIPrefab);

        // 2. 设置位置与朝向
        Vector3 worldPos = b.transform.position + Vector3.up * uiVerticalOffset;
        RectTransform rt = currentUI.GetComponent<RectTransform>();
        rt.position = worldPos;
        rt.rotation = Quaternion.LookRotation(rt.position - cam.transform.position);

        // 3. 初始化回调：Action 打开卡页，Move 开始移动
        currentUI.Init(
            b.transform,
            b.type.buildingName,
            cam,
            OpenPlayerCardPage,
            () => placer.BeginMove(b)
        );
    }

    private void OpenPlayerCardPage()
    {
        Debug.Log("[ClickDetector] OpenPlayerCardPage called");
        DestroyCurrentUI();
        if (mainUIPanel)    mainUIPanel.SetActive(false);
        if (playerCardPage) playerCardPage.SetActive(true);
    }

    public void ClosePlayerCardPage()
    {
        if (playerCardPage) playerCardPage.SetActive(false);
        if (mainUIPanel)    mainUIPanel.SetActive(true);
        DestroyCurrentUI();
    }

    private void DestroyCurrentUI()
    {
        if (currentUI != null)
        {
            Destroy(currentUI.gameObject);
            currentUI = null;
        }
    }
}
