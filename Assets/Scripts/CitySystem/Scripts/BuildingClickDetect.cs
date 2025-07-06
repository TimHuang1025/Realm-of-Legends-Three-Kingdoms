// ─────────────────────────────────────────────────────────────
// BuildingClickDetectorMinimal.cs   (2025‑07‑05 页面映射版)
// 点击建筑 → 弹 FloatingBuildingUI；Action 按钮跳到各自专属页面。
// 支持：多个允许建筑类型 / 点空白关闭 / 拖动移动。
// ─────────────────────────────────────────────────────────────
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BuildingClickDetectorMinimal : MonoBehaviour
{
    /*──────────────────────── 1. 基础设置 ────────────────────────*/
    [Header("1. 基础设置")]
    [SerializeField] private Camera                cam;
    [SerializeField] private LayerMask             buildingMask;
    [Tooltip("哪些建筑类型会弹出 UI；若为空，则所有 Building 都可点击")]
    [SerializeField] private List<BuildingTypeSO>   allowedTypes = new();

    /*──────────────────────── 2. 点击/拖动判定 ───────────────────*/
    [Header("2. 点击/拖动判定")]
    [SerializeField] private float clickRadius    = 0.6f;
    [SerializeField] private float dragThreshold  = 5f;
    [SerializeField] private float maxRayDistance = 800f;

    /*──────────────────────── 3. World‑Space UI Prefab ───────────*/
    [Header("3. World‑Space UI Prefab")]
    [SerializeField] private FloatingBuildingUI floatingUIPrefab;
    [SerializeField] private float               uiVerticalOffset = 2f; // 备用，目前未用

    /*──────────────────────── 4. 建筑移动逻辑 ───────────────────*/
    [Header("4. 建筑移动逻辑")]
    [SerializeField] private BuildingPlacer placer;

    /*──────────────────────── 5. 页面切换 ───────────────────────*/
    [Header("5. 页面切换 (Main UI, 各建筑专属 Page)")]
    [SerializeField] private GameObject mainUIPanel;

    [System.Serializable]
    public struct PageEntry
    {
        public BuildingTypeSO type;   // 建筑类型
        public GameObject     page;   // 对应 UI Page (屏幕空间 Canvas 下)
    }
    [Tooltip("每种建筑跳转到哪个页面")]
    [SerializeField] private List<PageEntry> pageMappings = new();

    // —— 运行时字段 ——————————————————
    private FloatingBuildingUI currentUI;
    private Building           candidateBuilding;
    private Vector2            pressPos;
    private bool               isDragging;
    private bool               isPressed = false;


    /*──────────────────────── Unity Loop ───────────────────────*/
    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (placer == null) placer = FindObjectOfType<BuildingPlacer>();

        // 先全部隐藏页面
        foreach (var entry in pageMappings)
            if (entry.page) entry.page.SetActive(false);
    }

    private void Update()
    {
        /* 统一处理触摸与鼠标 */
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            switch (t.phase)
            {
                case TouchPhase.Began:      OnPress(t.position);  break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary: if (isPressed) OnDrag(t.position); break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:   OnRelease();          break;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))          OnPress(Input.mousePosition);
            else if (Input.GetMouseButton(0) && isPressed) OnDrag(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0))        OnRelease();
        }

        // Esc 关闭任何页面
        if (Input.GetKeyDown(KeyCode.Escape)) CloseAllPages();
    }

    /*──────────────────────── Pointer Events ───────────────────*/
    private void OnPress(Vector2 screenPos)
    {
        isPressed  = true;
        isDragging = false;
        candidateBuilding = null;
        pressPos   = screenPos;

        if (IsPointerOverUI()) return; // 点到 UI

        DestroyCurrentUI();            // 点场景：先关旧 UI
        CloseAllPages();               // 以及任何屏幕页面

        // Raycast 场景
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, maxRayDistance, buildingMask)) return;
        Building b = hit.collider.GetComponentInParent<Building>();
        if (b == null || !IsAllowedBuilding(b)) return;

        // 距离过滤，避免点到屋檐
        Plane g = new Plane(Vector3.up, b.transform.position);
        if (!g.Raycast(ray, out float enter)) return;
        Vector3 gh = ray.GetPoint(enter);
        float dist = Vector2.Distance(new Vector2(gh.x, gh.z), new Vector2(b.transform.position.x, b.transform.position.z));
        if (dist > clickRadius) return;

        candidateBuilding = b;
        ShowUIAboveBuilding(b);
    }

    private void OnDrag(Vector2 pos)
    {
        if (!isPressed || candidateBuilding == null || isDragging) return;
        if ((pos - pressPos).sqrMagnitude > dragThreshold * dragThreshold)
        {
            isDragging = true;
            DestroyCurrentUI();
            candidateBuilding = null;
        }
    }

    private void OnRelease()
    {
        isPressed = false;
        candidateBuilding = null;
    }

    /*──────────────────────── UI Helpers ───────────────────────*/
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        PointerEventData data = new PointerEventData(EventSystem.current)
        {
            position = (Input.touchCount > 0) ? (Vector2)Input.GetTouch(0).position : (Vector2)Input.mousePosition
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        return hits.Count > 0;
    }

    private bool IsAllowedBuilding(Building b)
    {
        return (allowedTypes == null || allowedTypes.Count == 0) || allowedTypes.Contains(b.type);
    }

    private GameObject GetPageFor(BuildingTypeSO type)
    {
        foreach (var entry in pageMappings)
            if (entry.type == type) return entry.page;
        return null;
    }

    private void ShowUIAboveBuilding(Building b)
    {
        DestroyCurrentUI();
        if (floatingUIPrefab == null) { Debug.LogError("[BuildingClick] UI Prefab 未赋值"); return; }

        currentUI = Instantiate(floatingUIPrefab);
        currentUI.Init(
            b.transform,
            b.type.buildingName,
            cam,
            // Action → 打开专属页面
            () =>
            {
                DestroyCurrentUI();
                mainUIPanel?.SetActive(false);
                GameObject page = GetPageFor(b.type);
                if (page)
                {
                    page.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"[BuildingClick] No page mapping for {b.type.name}");
                }
            },
            // Move → 拖动
            () =>
            {
                DestroyCurrentUI();
                placer?.BeginMove(b);
            }
        );
    }

    public void CloseAllPages()
    {
        foreach (var entry in pageMappings)
            if (entry.page) entry.page.SetActive(false);
        mainUIPanel?.SetActive(true);
        DestroyCurrentUI();
    }

    private void DestroyCurrentUI()
    {
        if (currentUI)
        {
            Destroy(currentUI.gameObject);
            currentUI = null;
        }
    }
}