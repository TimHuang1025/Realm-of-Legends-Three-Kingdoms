using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 根据 vh / vw 规则自动调整 UI 元素尺寸。<br/>
/// 通过监听 <see cref="GeometryChangedEvent"/>，在布局完成当帧立即刷新。<br/>
/// 不再出现“切分辨率要点两次才生效”的现象。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class VhSizer : MonoBehaviour
{
    /*──────── 公共参数 ────────*/
    [Header("配置文件（拖入）")]
    public VhSizerConfig config;

    [Header("缩放参数 (短边像素基准)")]
    public float referenceShort = 1179f;
    [Range(0.1f, 1f)] public float minScale = 0.6f;

    /*──────── 内部字段 ────────*/
    UIDocument doc;

    /*──────── 生命周期 ────────*/
    void OnEnable()
    {
        doc = GetComponent<UIDocument>();

        if (doc.rootVisualElement != null)
        {
            RegisterGeometryChanged(doc.rootVisualElement);   // 运行时大概率直接有 root
        }
        else
        {
            // 部分情况下（热重载、编辑器即时播放）root 会延迟 1 帧才克隆好
            StartCoroutine(WaitForRoot());
        }
    }

    void OnDisable()
    {
        if (doc != null && doc.rootVisualElement != null)
            doc.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    IEnumerator WaitForRoot()
    {
        // 等到 rootVisualElement 可用
        while (doc != null && doc.rootVisualElement == null)
            yield return null;

        if (doc != null && doc.rootVisualElement != null)
            RegisterGeometryChanged(doc.rootVisualElement);
    }

    /*──────── 事件回调 ────────*/
    void RegisterGeometryChanged(VisualElement root)
    {
        root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        ApplyInternal();                 // 首次立即刷新一次
    }

    void OnGeometryChanged(GeometryChangedEvent e)
    {
        ApplyInternal();                 // 每次布局尺寸变动都会调用
    }

    /*──────── 对外 API ────────*/
    public void Apply() => ApplyInternal();

    /*──────── 主逻辑（与之前一致） ────────*/
    void ApplyInternal()
    {
        if (config == null)
        {
            Debug.LogWarning("[VhSizer] 未设置 config，跳过");
            return;
        }

        if (doc == null) doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogError("[VhSizer] 找不到 UIDocument！");
            return;
        }

        var root = doc.rootVisualElement;
        if (root == null || root.layout.height <= 0f) return;

        /*① 全局缩放 (≤1)*/
        float shortEdge   = Mathf.Min(Screen.width, Screen.height);
        float rawScale    = referenceShort / shortEdge;
        float globalScale = Mathf.Clamp(rawScale, minScale, 1f);

        /*② 单位换算*/
        float vh     = root.layout.height / 100f;   // 1 vh
        float vw     = root.layout.width  / 100f;   // 1 vw
        float aspect = (float)Screen.width / Screen.height;

        /*③ 遍历规则并应用尺寸*/
        foreach (var r in config.rules)
        foreach (var ve in Query(r))
        {
            /* —— 宽度 —— */
            if (r.widthVh > 0 || (r.addWidthVw && r.widthVw > 0))
            {
                float w = 0f;

                // (a) vh 部分
                if (r.widthVh > 0)
                {
                    float wVh = r.widthVh * vh;
                    if (r.applyScale)  wVh *= globalScale;
                    if (r.aspectWidth) wVh *= aspect / 1.4f;
                    w += wVh;
                }

                // (b) vw 部分（永远不乘 globalScale）
                if (r.addWidthVw && r.widthVw > 0)
                    w += r.widthVw * vw;

                ve.style.width = w;
            }

            /* —— 高度 —— */
            if (r.heightVh > 0)
            {
                float h = r.heightVh * vh;
                if (r.applyScale) h *= globalScale;
                ve.style.height = h;
            }

            /* —— 字号 —— */
            if (r.fontVh > 0)
            {
                float f = r.fontVh * vh;
                if (r.applyScale) f *= globalScale;
                ve.style.fontSize = f;
            }
        }

        // Debug.Log($"[VhSizer] refreshed  scale={globalScale:F3}");
    }

    /*──────── 工具：查询元素 ────────*/
    IEnumerable<VisualElement> Query(VhSizerConfig.Rule r) =>
        r.queryType == VhSizerConfig.Rule.QueryType.ByClass
            ? doc.rootVisualElement.Query<VisualElement>(className: r.queryValue).ToList()
            : doc.rootVisualElement.Query<VisualElement>(name: r.queryValue).ToList();
}
