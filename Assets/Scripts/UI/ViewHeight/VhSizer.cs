using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class VhSizer : MonoBehaviour
{
    [Header("配置文件（拖入）")]
    public VhSizerConfig config;

    [Header("缩放参数 (短边像素基准)")]
    public float referenceShort = 1179f;      // iPhone 15 短边
    [Range(0.1f, 1f)] public float minScale = 0.6f;

    UIDocument doc;
    int lastW, lastH;

    void Awake()  { doc = GetComponent<UIDocument>(); Apply(); }
    void Update() { if (Screen.width != lastW || Screen.height != lastH) Apply(); }

    void Apply()
    {
        if (config == null) return;
        lastW = Screen.width; lastH = Screen.height;

        /* 1) 计算全局 scale (≤1) */
        float shortEdge = Mathf.Min(Screen.width, Screen.height);
        float rawScale = referenceShort / shortEdge;
        float globalScale = Mathf.Clamp(rawScale, minScale, 1f);

        /* 2) 1vh 像素 */
        float vh = doc.rootVisualElement.layout.height / 100f;

        /* 3) 遍历规则 */
        foreach (var r in config.rules)
            foreach (var ve in Query(r))
            {
                float s = r.applyScale ? globalScale : 1f;   // ← 逐元素开关

                if (r.widthVh > 0) ve.style.width = r.widthVh * vh * s;
                if (r.heightVh > 0) ve.style.height = r.heightVh * vh * s;
                if (r.fontVh > 0) ve.style.fontSize = r.fontVh * vh * s;
            }

        Debug.Log($"[VhSizer] {Screen.width}×{Screen.height}  short={shortEdge}  scale={globalScale:F3}");
    }

    System.Collections.Generic.IEnumerable<VisualElement> Query(VhSizerConfig.Rule r) =>
        r.queryType == VhSizerConfig.Rule.QueryType.ByClass
            ? doc.rootVisualElement.Query<VisualElement>(className: r.queryValue).ToList()
            : doc.rootVisualElement.Query<VisualElement>(name: r.queryValue).ToList();
}
