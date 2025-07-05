using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingClickDetectorMinimal : MonoBehaviour
{
    [Header("1. 基础设置")]
    [SerializeField] private Camera        cam;
    [SerializeField] private LayerMask     buildingMask;
    [SerializeField] private BuildingTypeSO townHallSO;

    [Header("2. 点击/拖动判定")]
    [SerializeField] private float clickRadius    = 0.6f;
    [SerializeField] private float dragThreshold  = 5f;
    [SerializeField] private float maxRayDistance = 800f;

    [Header("3. World-Space UI Prefab")]
    [SerializeField] private FloatingBuildingUI floatingUIPrefab;
    [SerializeField] private float               uiVerticalOffset = 2f;

    [Header("4. 建筑移动逻辑")]
    [SerializeField] private BuildingPlacer placer;

    [Header("5. 面板切换")]
    [SerializeField] private GameObject playerCardPage;
    [SerializeField] private GameObject mainUIPanel;

    // —— 运行时字段 ——————————————————
    private FloatingBuildingUI currentUI;
    private Building           candidateBuilding;
    private Vector2            pressPos;
    private bool               isDragging;
    private int                activeFingerId = -1;

    void Awake()
    {
        if (cam == null)   cam    = Camera.main;
        if (placer == null) placer = FindObjectOfType<BuildingPlacer>();
        if (playerCardPage) playerCardPage.SetActive(false);
    }

    void Update()
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
            // 鼠标
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

        // 如果点在 UI 上就忽略，保证面板按钮可点
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(fingerId))
            return;

        // 只有在场景点击时才销毁旧 UI
        DestroyCurrentUI();

        // 发射射线检测建筑
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
                new Vector2(b.transform.position.x, b.transform.position.z)
            ) > clickRadius) return;

        // 通过检测，弹出世界空间 UI
        candidateBuilding = b;
        ShowUIAboveBuilding(b);
    }

    private void OnDrag(Vector2 pos, int fingerId)
    {
        if (fingerId != activeFingerId ||
            candidateBuilding == null ||
            isDragging) return;

        if ((pos - pressPos).sqrMagnitude >
            dragThreshold * dragThreshold)
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

        // 1) 实例化 World-Space UI
        currentUI = Instantiate(floatingUIPrefab);

        // 2) 绑定 Canvas 的 worldCamera，以接收按钮事件
        var canvas = currentUI.GetComponent<Canvas>();
        if (canvas != null) canvas.worldCamera = cam;

        // 3) 放到建筑顶部 + 偏移 （世界坐标）
        currentUI.transform.position = b.transform.position
                                      + Vector3.up * uiVerticalOffset;

        // 4) 初始化名字、按钮回调
        currentUI.Init(
            b.transform,
            b.type.buildingName,
            cam,
            // Action 按钮：打开卡页
            () =>
            {
                DestroyCurrentUI();
                if (mainUIPanel)    mainUIPanel.SetActive(false);
                if (playerCardPage) playerCardPage.SetActive(true);
            },
            // Move 按钮：开始移动
            () => placer.BeginMove(b)
        );
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
