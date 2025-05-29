using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class VhSizer : MonoBehaviour
{
    /*──────── 公共参数 ────────*/
    [Header("配置文件（拖入）")]
    public VhSizerConfig config;

    [Header("缩放参数 (短边像素基准)")]
    public float referenceShort = 1179f;        // iPhone 15 竖屏短边
    [Range(0.1f, 1f)] public float minScale = 0.6f;

    /*──────── 内部字段 ────────*/
    UIDocument doc;
    int lastW, lastH;

    /*──────── 生命周期 ────────*/
    void Awake()  { doc = GetComponent<UIDocument>(); Apply(); }
    void Update() { if (Screen.width != lastW || Screen.height != lastH) Apply(); }

    /*──────── 主逻辑 ────────*/
    public void Apply()
    {
        if (config == null) return;
        lastW = Screen.width;
        lastH = Screen.height;

        /*① 全局缩放 (≤1)*/
        float shortEdge   = Mathf.Min(Screen.width, Screen.height);
        float rawScale    = referenceShort / shortEdge;
        float globalScale = Mathf.Clamp(rawScale, minScale, 1f);

        /*② 单位换算*/
        float vh     = doc.rootVisualElement.layout.height / 100f;      // 1 vh 像素
        float aspect = (float)Screen.width / Screen.height;            // 宽高比

        /*③ 遍历规则并应用尺寸*/
        foreach (var r in config.rules)
        foreach (var ve in Query(r))
        {
            float s = r.applyScale ? globalScale : 1f;                 // 每元素是否全局缩放

            /*—— 宽度 ——*/
            if (r.widthVh > 0)
            {
                float w = r.widthVh * vh * s;
                if (r.aspectWidth) w *= aspect / 1.4f;                 // 额外按(宽/高)/1.4
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
        Debug.Log($"[VhSizer] {Screen.width}×{Screen.height}  aspect={aspect:F2}  short={shortEdge}  scale={globalScale:F3}");
    }

    /*──────── 工具：查询元素 ────────*/
    System.Collections.Generic.IEnumerable<VisualElement> Query(VhSizerConfig.Rule r) =>
        r.queryType == VhSizerConfig.Rule.QueryType.ByClass
            ? doc.rootVisualElement.Query<VisualElement>(className: r.queryValue).ToList()
            : doc.rootVisualElement.Query<VisualElement>(name: r.queryValue).ToList();
}
