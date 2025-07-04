using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FloatingBuildingUI : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button   actionBtn;
    [SerializeField] private Button   moveBtn;          // 新增：移动按钮

    [Header("位置偏移(米)")]
    [SerializeField] private Vector3 extraOffset = new Vector3(0, 0.6f, 0);

    // 运行时缓存
    private Transform     target;   // 跟随的建筑
    private Camera        cam;
    private System.Action onActionClick;
    private System.Action onMoveClick;

    /// <summary>
    /// 初始化，由 Detector 调用
    /// </summary>
    public void Init(Transform followTarget, string displayName,
                     Camera worldCam,
                     System.Action onActionClick,
                     System.Action onMoveClick)
    {
        target         = followTarget;
        cam            = worldCam;
        this.onActionClick = onActionClick;
        this.onMoveClick   = onMoveClick;

        // 名称设置
        nameText.text = displayName;

        // Action 按钮
        actionBtn.onClick.RemoveAllListeners();
        actionBtn.onClick.AddListener(() =>
        {
            Debug.Log($"[FloatingBuildingUI] ActionBtn clicked for {displayName}");
            onActionClick?.Invoke();
        });

        // Move 按钮
        moveBtn.onClick.RemoveAllListeners();
        moveBtn.onClick.AddListener(() =>
        {
            Debug.Log($"[FloatingBuildingUI] MoveBtn clicked for {displayName}");
            onMoveClick?.Invoke();
        });
    }

    void LateUpdate()
    {
        if (target == null || cam == null)
        {
            Destroy(gameObject);
            return;
        }

        // 1) 计算建筑顶部位置
        Vector3 top = target.position;
        if (target.TryGetComponent<Collider>(out var col))
            top = col.bounds.center + Vector3.up * col.bounds.extents.y;
        else if (target.TryGetComponent<Renderer>(out var rend))
            top = rend.bounds.center + Vector3.up * rend.bounds.extents.y;

        transform.position = top + extraOffset;

        // 2) 面向摄像机 (Billboard)
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }

    void OnDestroy()
    {
        actionBtn.onClick.RemoveAllListeners();
        moveBtn.onClick.RemoveAllListeners();
    }
}
