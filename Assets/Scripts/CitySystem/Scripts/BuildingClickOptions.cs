// Assets/Scripts/Game/Interaction/BuildingClickOptions.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 在场景中点击（或触摸）建筑后，弹出“移动 / 执行动作”面板的控制脚本。
/// 兼容 PC 鼠标与移动端多指触摸。
/// </summary>
public class BuildingClickOptions : MonoBehaviour
{
    #region Inspector

    [Header("1. 放置/移动控制器引用")]
    [Tooltip("场景中负责放置与移动的 BuildingPlacer 实例")]
    public BuildingPlacer placer;

    [Header("2. 射线检测设置")]
    public Camera     cam;             // 主摄像机
    public LayerMask  buildingMask;    // 包含“Building”层
    public BuildingTypeSO filterSO;    // 只对这种类型的建筑弹菜单
    public float      clickRadius = 0.6f;
    public float      maxRayDist  = 800f;

    [Header("3. UI 引用（都不能留空）")]
    public GameObject optionsPanel;    // Canvas 下的面板
    public TMP_Text   NameText;
    public Button     ActionBtn;
    public Button     MoveBtn;

    #endregion

    //—— 运行时状态 ——————————————————————
    private Building selectedBuilding;

    #region Unity Lifecycle
    private void Awake()
    {
        if (placer == null) placer = FindObjectOfType<BuildingPlacer>();
        if (cam    == null) cam    = Camera.main;

        if (optionsPanel) optionsPanel.SetActive(false);
    }

    private void Update()
    {
        // 1) 捕获一次“指针按下”事件（鼠标左键 or TouchPhase.Began）
        if (!TryGetPointerDown(out Vector2 pointerPos, out int fingerId))
            return;

        Debug.Log("[ClickOptions] PointerDown detected");

        // 2) 若按下处位于 UI，直接忽略
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId))
        {
            Debug.Log("[ClickOptions] Pointer over UI — ignored");
            return;
        }

        // 3) 物理射线检测
        Ray ray = cam.ScreenPointToRay(pointerPos);
        if (!Physics.Raycast(ray, out var hit, maxRayDist, buildingMask))
        {
            Debug.Log("[ClickOptions] Raycast missed any building");
            optionsPanel.SetActive(false);
            return;
        }

        // 4) 获取 Building 组件
        var b = hit.collider.GetComponentInParent<Building>();
        if (b == null)
        {
            Debug.Log("[ClickOptions] No Building component found on hit");
            optionsPanel.SetActive(false);
            return;
        }

        // 5) 类型过滤
        if (b.type != filterSO)
        {
            Debug.Log($"[ClickOptions] Building type {b.type?.name} != filter {filterSO?.name}");
            optionsPanel.SetActive(false);
            return;
        }

        // 6) 水平距离过滤
        Plane ground = new Plane(Vector3.up, b.transform.position);
        if (!ground.Raycast(ray, out float enter))
        {
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
            Debug.Log($"[ClickOptions] Click too far: {dist:F2} m");
            optionsPanel.SetActive(false);
            return;
        }

        // 7) 满足所有条件 → 弹出选项
        ShowOptions(b, pointerPos);
    }
    #endregion

    #region Pointer Helpers
    /// <summary>
    /// 同时兼容鼠标左键与触摸点击。
    /// 返回 true 表示本帧有一次“按下”事件，并输出屏幕坐标 & fingerId。
    /// 鼠标的 fingerId 统一使用 -1。
    /// </summary>
    private static bool TryGetPointerDown(out Vector2 pos, out int fingerId)
    {
        // 移动端：遍历触摸
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began)
            {
                pos      = t.position;
                fingerId = 0;           // 0,1,2…
                return true;
            }
        }

        // PC / Device Simulator：鼠标左键
        if (Input.GetMouseButtonDown(0))
        {
            pos      = Input.mousePosition;
            fingerId = 0;                       // ← 关键：用 0 代替 -1
            return true;
        }

        pos      = default;
        fingerId = -999;
        return false;
    }
    #endregion

    #region UI Display
    private void ShowOptions(Building b, Vector2 screenPos)
    {
        selectedBuilding = b;
        Debug.Log($"[ClickOptions] Showing options for {b.name}");

        // 定位面板到指针位置
        var rt = optionsPanel.GetComponent<RectTransform>();
        rt.position = screenPos;

        // 更新名称
        NameText.text = b.type.buildingName;

        // 绑定按钮事件
        MoveBtn.onClick.RemoveAllListeners();
        MoveBtn.onClick.AddListener(OnMoveClicked);

        ActionBtn.onClick.RemoveAllListeners();
        ActionBtn.onClick.AddListener(OnActionClicked);

        optionsPanel.SetActive(true);
    }
    #endregion

    #region Button Callbacks
    private void OnMoveClicked()
    {
        Debug.Log($"[ClickOptions] Move clicked for {selectedBuilding?.name}");
        optionsPanel.SetActive(false);

        if (selectedBuilding != null && placer != null)
        {
            placer.BeginMove(selectedBuilding);
            selectedBuilding = null;
        }
    }

    private void OnActionClicked()
    {
        Debug.Log($"[ClickOptions] Action clicked for {selectedBuilding?.name}");
        optionsPanel.SetActive(false);
        // TODO: 自定义 Action 逻辑
    }
    #endregion
}
