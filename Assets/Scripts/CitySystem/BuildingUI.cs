using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(GraphicRaycaster))]
public class FloatingBuildingUI : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button actionBtn;
    [SerializeField] private Button moveBtn;

    [Header("世界空间偏移 (米)，在顶部再往上挪一点")]
    [SerializeField] private Vector3 extraOffset = new Vector3(0f, 0.2f, 0f);

    private Transform target;
    private Camera   worldCam;
    private System.Action onAction;
    private System.Action onMove;
    private Canvas   canvas;

    /// <summary>
    /// 由 Detector 调用，设置跟随目标、显示名字、按钮回调
    /// </summary>
    public void Init(Transform followTarget,
                     string displayName,
                     Camera cam,
                     System.Action onActionClick,
                     System.Action onMoveClick)
    {
        target    = followTarget;
        worldCam  = cam;
        onAction  = onActionClick;
        onMove    = onMoveClick;

        // —— World Space UI 必备设置 ——
        canvas = GetComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.worldCamera = worldCam;
        // 确保有 GraphicRaycaster
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        nameText.text = displayName;

        actionBtn.onClick.RemoveAllListeners();
        actionBtn.onClick.AddListener(() =>
        {
            Debug.Log($"[FloatingBuildingUI] ActionBtn clicked for {displayName}");
            onAction?.Invoke();
        });

        moveBtn.onClick.RemoveAllListeners();
        moveBtn.onClick.AddListener(() =>
        {
            Debug.Log($"[FloatingBuildingUI] MoveBtn clicked for {displayName}");
            onMove?.Invoke();
        });
    }

    private void LateUpdate()
    {
        if (target == null || worldCam == null)
        {
            Destroy(gameObject);
            return;
        }

        // 1) 计算并设置世界位置到建筑顶端
        Bounds b = CalcBounds(target);
        Vector3 worldTop = b.center + Vector3.up * b.extents.y;
        transform.position = worldTop + extraOffset;

        // 2) 保持面向摄像机
        transform.rotation = worldCam.transform.rotation;
    }

    private Bounds CalcBounds(Transform t)
    {
        if (t.TryGetComponent<Collider>(out var col))
            return col.bounds;
        if (t.TryGetComponent<Renderer>(out var rend))
            return rend.bounds;
        // 回退一个小 Bounds 避免 zero 大小导致偏移出错
        return new Bounds(t.position, Vector3.one * 0.1f);
    }

    private void OnDestroy()
    {
        actionBtn.onClick.RemoveAllListeners();
        moveBtn.onClick.RemoveAllListeners();
    }
}
