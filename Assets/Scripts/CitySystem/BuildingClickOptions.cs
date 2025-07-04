// Assets/Scripts/Game/Interaction/BuildingClickOptions.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingClickOptions : MonoBehaviour
{
    [Header("1. 放置/移动控制器引用")]
    [Tooltip("场景中负责放置与移动的 BuildingPlacer 实例")]
    public BuildingPlacer placer;

    [Header("2. 射线检测设置")]
    public Camera         cam;             // 主摄像机
    public LayerMask      buildingMask;    // 包含“Building”层
    public BuildingTypeSO filterSO;        // 只对这类建筑弹菜单
    public float          clickRadius = 0.6f;
    public float          maxRayDist  = 800f;

    [Header("3. UI 引用（都不能留空）")]
    public GameObject optionsPanel;  // Canvas 下的面板，包含 NameText, ActionBtn, MoveBtn
    public TMP_Text       NameText;
    public Button     ActionBtn;
    public Button     MoveBtn;

    //—— 运行时状态 ——————————————————————
    private Building selectedBuilding;

    void Awake()
    {
        // 自动找引用（如果忘了拖）
        if (placer == null) placer = FindObjectOfType<BuildingPlacer>();
        if (cam     == null) cam     = Camera.main;

        // 一开始隐藏
        if (optionsPanel) optionsPanel.SetActive(false);
    }

    void Update()
    {
        // 只响应鼠标左键点下
        if (!Input.GetMouseButtonDown(0)) return;

        Debug.Log("[ClickOptions] MouseButtonDown");

        // 如果点在 UI 上，就不做射线检测
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("[ClickOptions] Click over UI, ignoring");
            return;
        }

        Vector2 mp = Input.mousePosition;
        Ray ray = cam.ScreenPointToRay(mp);
        if (!Physics.Raycast(ray, out var hit, maxRayDist, buildingMask))
        {
            Debug.Log("[ClickOptions] Raycast missed any building");
            optionsPanel.SetActive(false);
            return;
        }

        Debug.Log($"[ClickOptions] Raycast hit: {hit.collider.name}");

        // 找 Building 脚本
        var b = hit.collider.GetComponentInParent<Building>();
        if (b == null)
        {
            Debug.Log("[ClickOptions] No Building component found on hit");
            optionsPanel.SetActive(false);
            return;
        }

        // 过滤类型
        if (b.type != filterSO)
        {
            Debug.Log($"[ClickOptions] Building type {b.type?.name} != filter {filterSO?.name}");
            optionsPanel.SetActive(false);
            return;
        }

        // 水平距离过滤
        Plane ground = new Plane(Vector3.up, b.transform.position);
        if (!ground.Raycast(ray, out float enter))
        {
            Debug.Log("[ClickOptions] Ground plane raycast failed");
            optionsPanel.SetActive(false);
            return;
        }
        Vector3 gh = ray.GetPoint(enter);
        float dist = Vector2.Distance(
            new Vector2(gh.x, gh.z),
            new Vector2(b.transform.position.x, b.transform.position.z)
        );
        if (dist > clickRadius)
        {
            Debug.Log($"[ClickOptions] Click too far: {dist}m");
            optionsPanel.SetActive(false);
            return;
        }

        // 一切通过：弹出选项
        ShowOptions(b, mp);
    }

    private void ShowOptions(Building b, Vector2 screenPos)
    {
        selectedBuilding = b;
        Debug.Log($"[ClickOptions] Showing options for {b.name}");

        // 定位面板到鼠标位置
        var rt = optionsPanel.GetComponent<RectTransform>();
        rt.position = screenPos;

        // 设置名称
        NameText.text = b.type.buildingName;

        // 按钮绑定
        MoveBtn.onClick.RemoveAllListeners();
        MoveBtn.onClick.AddListener(OnMoveClicked);

        ActionBtn.onClick.RemoveAllListeners();
        ActionBtn.onClick.AddListener(OnActionClicked);

        optionsPanel.SetActive(true);
    }

    private void OnMoveClicked()
    {
        Debug.Log($"[ClickOptions] Move clicked for {selectedBuilding.name}");
        optionsPanel.SetActive(false);

        if (selectedBuilding != null && placer != null)
        {
            placer.BeginMove(selectedBuilding);
            selectedBuilding = null;
        }
    }

    private void OnActionClicked()
    {
        Debug.Log($"[ClickOptions] Action clicked for {selectedBuilding.name}");
        optionsPanel.SetActive(false);
        // TODO: 自定义 Action 逻辑
    }
}
