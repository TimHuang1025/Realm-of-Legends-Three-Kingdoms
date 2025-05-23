using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// 把 “vh”(当前可视高度百分比) 转换为像素并应用到指定 UI 元素。<br/>
/// • 支持按 Name 精确单个，或按 Class 批量匹配。<br/>
/// • 可同时修改宽、高、字号、内边距、描边厚度 (0 表示该项不修改)。<br/>
/// 逻辑基准：rootVisualElement.layout.height，<br/>
/// 因此在 Device Simulator 被缩放时也能得到正确结果。<br/>
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class VhSizer : MonoBehaviour
{
    public enum QueryType { ByName, ByClass }

    [System.Serializable]
    public struct Target
    {
        public QueryType queryType;   // Name / Class
        public string    queryValue;  // 对应值
        public float     widthVh;     // 0 = 不改宽
        public float     heightVh;    // 0 = 不改高
        public float     fontVh;      // 0 = 不改字号
        public float     paddingVh;   // 0 = 不改 padding
        public float     borderVh;    // 0 = 不改 border-width
    }

    [Header("需要用 vh 调整的元素")]
    public Target[] targets;

    UIDocument doc;
    float lastRootHeight;

    void Awake()
    {
        doc = GetComponent<UIDocument>();

        /* 等到第一次布局完成再应用尺寸，
           避免 root.layout.height 仍为 0。 */
        doc.rootVisualElement.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            ApplySizes();
        }, TrickleDown.NoTrickleDown);

        /* 之后定时检查屏幕/窗口尺寸变化 */
        doc.rootVisualElement.schedule
            .Execute(CheckAndRefresh)
            .Every(300);
    }

    void CheckAndRefresh()
    {
        float h = doc.rootVisualElement.layout.height;
        if (Mathf.Abs(h - lastRootHeight) > 0.1f)
            ApplySizes();
    }

    void ApplySizes()
    {
        float rootH = doc.rootVisualElement.layout.height;
        if (rootH <= 0.1f) return;                    // 防御
        float vh = rootH / 100f;                      // 1 vh 对应像素
        lastRootHeight = rootH;

        Debug.Log($"[VhSizer] 1vh = {vh:F2}px  (rootH = {rootH})");

        foreach (var t in targets)
        {
            foreach (var ve in GetElements(t))
            {
                // 尺寸
                if (t.widthVh  > 0) ve.style.width  = t.widthVh  * vh;
                if (t.heightVh > 0) ve.style.height = t.heightVh * vh;

                // 字号
                if (t.fontVh   > 0) ve.style.fontSize = t.fontVh * vh;

                // 内边距
                if (t.paddingVh > 0)
                {
                    float pad = t.paddingVh * vh;
                    ve.style.paddingLeft   = pad;
                    ve.style.paddingRight  = pad;
                    ve.style.paddingTop    = pad;
                    ve.style.paddingBottom = pad;
                }

                // 描边
                if (t.borderVh > 0)
                {
                    float bw = t.borderVh * vh;
                    ve.style.borderLeftWidth   = bw;
                    ve.style.borderRightWidth  = bw;
                    ve.style.borderTopWidth    = bw;
                    ve.style.borderBottomWidth = bw;
                }
            }
        }
    }

    IEnumerable<VisualElement> GetElements(Target t)
    {
        if (t.queryType == QueryType.ByName)
        {
            var ve = doc.rootVisualElement.Q<VisualElement>(t.queryValue);
            if (ve != null) yield return ve;
        }
        else // ByClass
        {
            foreach (var ve in doc.rootVisualElement
                                   .Query<VisualElement>(className: t.queryValue).ToList())
                yield return ve;
        }
    }
}
