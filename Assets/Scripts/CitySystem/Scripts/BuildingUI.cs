// ─────────────────────────────────────────────────────────────
// FloatingBuildingUI.cs  (2025‑07‑05 braces‑fixed 版)
// 2D 贴屏世界空间 Canvas（固定角度）
// 单例、点空白隐藏、点其他建筑切换。
// ─────────────────────────────────────────────────────────────
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(GraphicRaycaster))]
public class FloatingBuildingUI : MonoBehaviour
{
    /* ---------- Static ---------- */
    /// <summary>场景中当前显示的 UI（保证单例效果）。</summary>
    public static FloatingBuildingUI ActiveInstance { get; private set; }

    /* ---------- Inspector ---------- */
    [Header("UI 组件")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button   actionBtn;
    [SerializeField] private Button   moveBtn;

    [Header("世界空间偏移 (米)")]
    [SerializeField] private Vector3 extraOffset = new(0f, 0.2f, 0f);

    [Header("缩放设置")]
    [SerializeField] private float scaleFactor = 0.01f;
    [SerializeField] private bool  maintainConstantSize = true;
    [SerializeField] private float referenceDistance   = 10f;

    /* ---------- Runtime ---------- */
    private Transform     target;
    private Camera        worldCam;
    private System.Action onAction;
    private System.Action onMove;
    private Canvas        canvas;
    private CanvasScaler  canvasScaler;
    private bool          initialized = false;

    #region Init
    public void Init(Transform     followTarget,
                     string        displayName,
                     Camera        cam,
                     System.Action onActionClick,
                     System.Action onMoveClick)
    {
        // 若已有 UI 显示，先隐藏它
        if (ActiveInstance != null && ActiveInstance != this)
            ActiveInstance.Hide();
        ActiveInstance = this;

        target     = followTarget;
        worldCam   = cam;
        onAction   = onActionClick;
        onMove     = onMoveClick;

        /* Canvas 设置 */
        canvas             = GetComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.worldCamera = worldCam;

        var rt = (RectTransform)canvas.transform;
        rt.localScale = Vector3.one * scaleFactor;
        //rt.sizeDelta  = new Vector2(200, 100);
        rt.pivot      = rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        canvas.overrideSorting = true;
        canvas.sortingOrder    = 999;

        /* CanvasScaler */
        if (!TryGetComponent(out canvasScaler))
        {
            canvasScaler             = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        }

        /* Raycaster / Layer */
        gameObject.layer = LayerMask.NameToLayer("UI");
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = LayerMask.NameToLayer("UI");
        if (!TryGetComponent(out GraphicRaycaster gr))
            gr = gameObject.AddComponent<GraphicRaycaster>();

        /* EventSystem */
        EnsureEventSystem();

        /* 文本 & 按钮 */
        if (nameText) nameText.text = displayName;
        if (actionBtn)
        {
            actionBtn.onClick.RemoveAllListeners();
            actionBtn.onClick.AddListener(() => onAction?.Invoke());
        }
        if (moveBtn)
        {
            moveBtn.onClick.RemoveAllListeners();
            moveBtn.onClick.AddListener(() => onMove?.Invoke());
        }

        initialized = true;
        UpdatePositionAndScale();
    }
    #endregion Init

    /* ---------- Unity Loop ---------- */
    private void LateUpdate()
    {
        if (!initialized) return;
        if (target == null || worldCam == null) { Hide(); return; }
        UpdatePositionAndScale();
    }

    private void Update()
    {
        if (!initialized || worldCam == null) return;

        // 点空白隐藏 / 切换
        if (Input.GetMouseButtonDown(0))
        {
            bool uiHit = EventSystem.current.IsPointerOverGameObject();
            if (!uiHit)
            {
                Ray ray = worldCam.ScreenPointToRay(Input.mousePosition);
                if (!Physics.Raycast(ray, out _))
                    Hide();
            }
        }
    }

    /* ---------- Position & Scale ---------- */
    private void UpdatePositionAndScale()
    {
        Bounds b = CalcBounds(target);
        transform.position = b.center + Vector3.up * b.extents.y + extraOffset;

        if (maintainConstantSize)
        {
            float dist   = Vector3.Distance(transform.position, worldCam.transform.position);
            float finalS = dist / referenceDistance * scaleFactor;
            transform.localScale = Vector3.one * finalS;
        }
        else transform.localScale = Vector3.one * scaleFactor;
        // 角度固定：脚本不改 rotation
    }

    /* ---------- Public ---------- */
    public void Hide()
    {
        Destroy(gameObject);
    }

    /* ---------- Utils ---------- */
    private Bounds CalcBounds(Transform root)
    {
        Bounds b = new Bounds(root.position, Vector3.zero);
        bool   has = false;
        foreach (var c in root.GetComponentsInChildren<Collider>())
        { if (has) b.Encapsulate(c.bounds); else { b = c.bounds; has = true; } }
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        { if (has) b.Encapsulate(r.bounds); else { b = r.bounds; has = true; } }
        if (!has) b = new Bounds(root.position, Vector3.one * 0.1f);
        return b;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private void OnDestroy()
    {
        if (ActiveInstance == this) ActiveInstance = null;
        actionBtn?.onClick.RemoveAllListeners();
        moveBtn  ?.onClick.RemoveAllListeners();
    }
}
