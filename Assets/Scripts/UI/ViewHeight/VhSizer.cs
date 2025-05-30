using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class VhSizer : MonoBehaviour
{
    /*──────── 公共参数 ────────*/
    [Header("配置文件（拖入）")]
    public VhSizerConfig config;

    [Header("缩放参数 (短边像素基准)")]
    public float referenceShort = 1179f;          // iPhone 15 竖屏短边
    [Range(0.1f, 1f)] public float minScale = 0.6f;

    /*──────── 内部字段 ────────*/
    UIDocument doc;
    int lastW, lastH;

    /*──────── 生命周期 ────────*/
    void Awake()  => doc = GetComponent<UIDocument>();

    /// <summary>Start 时再 Apply，确保 UIDocument 已完成克隆</summary>
    IEnumerator Start()
    {
        // 等到 root 有非零高度，说明布局 OK
        yield return new WaitUntil(() =>
        {
            return doc != null &&
                   doc.rootVisualElement != null &&
                   doc.rootVisualElement.layout.height > 1f;
        });

        Apply();   // 首次刷新
    }

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH)
            Apply();                                // 分辨率变化时刷新
    }

    /*──────── 主逻辑 ────────*/
    public void Apply()
    {
        // 0. 防呆：引用检测
        if (config == null)
        {
            Debug.LogWarning("[VhSizer] 未设置 config，跳过");
            return;
        }

        if (doc == null)            doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogError("[VhSizer] 找不到 UIDocument！");
            return;
        }

        var root = doc.rootVisualElement;
        if (root == null || root.layout.height <= 0f)
        {
            // 布局还没完成 → 1 帧后重试
            StartCoroutine(RetryNextFrame());
            return;
        }

        lastW = Screen.width;
        lastH = Screen.height;

        /*① 全局缩放 (≤1)*/
        float shortEdge   = Mathf.Min(Screen.width, Screen.height);
        float rawScale    = referenceShort / shortEdge;
        float globalScale = Mathf.Clamp(rawScale, minScale, 1f);

        /*② 单位换算*/
        float vh     = root.layout.height / 100f;           // 1 vh 像素
        float aspect = (float)Screen.width / Screen.height; // 宽高比

        /*③ 遍历规则并应用尺寸*/
        foreach (var r in config.rules)
        foreach (var ve in Query(r))
        {
            float s = r.applyScale ? globalScale : 1f;

            /*—— 宽度 ——*/
            if (r.widthVh > 0)
            {
                float w = r.widthVh * vh * s;
                if (r.aspectWidth) w *= aspect / 1.4f;      // 额外按(宽/高)/1.4
                ve.style.width = w;
            }

            /*—— 高度 ——*/
            if (r.heightVh > 0)
                ve.style.height = r.heightVh * vh * s;

            /*—— 字号 ——*/
            if (r.fontVh > 0)
                ve.style.fontSize = r.fontVh * vh * s;
        }

        /*④ 调试输出*/
        // Debug.Log($"[VhSizer] {Screen.width}×{Screen.height}  aspect={aspect:F2}  scale={globalScale:F3}");
    }

    IEnumerator RetryNextFrame()
    {
        yield return null;
        Apply();
    }

    /*──────── 工具：查询元素 ────────*/
    IEnumerable<VisualElement> Query(VhSizerConfig.Rule r) =>
        r.queryType == VhSizerConfig.Rule.QueryType.ByClass
            ? doc.rootVisualElement.Query<VisualElement>(className: r.queryValue).ToList()
            : doc.rootVisualElement.Query<VisualElement>(name: r.queryValue).ToList();
}
