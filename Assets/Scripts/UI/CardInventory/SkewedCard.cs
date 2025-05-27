using UnityEngine;
using UnityEngine.UIElements;

/// <summary>白底 / 向右倾斜 / 可调圆角+描边+颜色 的梯形卡片</summary>
public class SkewedCard : VisualElement
{
    /// <summary>向右倾斜角度(°)</summary>
    public float SkewX { get; set; } = 18f;

    /// <summary>四个角的圆角半径</summary>
    public float CornerRadius { get; set; } = 10f;

    /// <summary>描边线宽(px)</summary>
    public float BorderWidth { get; set; } = 1f;

    /// <summary>描边颜色</summary>
    public Color BorderColor { get; set; } = Color.black;

    /// <summary>填充颜色</summary>
    public Color FillColor { get; set; } = Color.white;

    public SkewedCard(float skewX = 18f)
    {
        SkewX = skewX;
        generateVisualContent += OnGenerate;
    }

    void OnGenerate(MeshGenerationContext ctx)
    {
        // 宽高太小，不绘制
        if (contentRect.width < 0.1f || contentRect.height < 0.1f)
            return;

        // ① 原矩形四点 (左下, 左上, 右上, 右下)
        Vector2 v0 = new Vector2(0,                 contentRect.height); // bottom-left
        Vector2 v1 = new Vector2(0,                 0);                  // top-left
        Vector2 v2 = new Vector2(contentRect.width, 0);                  // top-right
        Vector2 v3 = new Vector2(contentRect.width, contentRect.height); // bottom-right

        // ② 计算倾斜偏移量（顶边偏移量最大, 底边不偏移）
        float dx = contentRect.height * Mathf.Tan(SkewX * Mathf.Deg2Rad);

        // 简单处理：只让上边(v1,v2)往右移dx
        v1.x += dx;
        v2.x += dx;

        // ③ Painter2D 绘制梯形 + 圆角
        var p = ctx.painter2D;
        p.lineWidth   = BorderWidth;   // 由外部属性决定
        p.strokeColor = BorderColor;   // 由外部属性决定
        p.fillColor   = FillColor;     // 由外部属性决定

        p.BeginPath();

        Vector2 tl = v1;  // top-left
        Vector2 tr = v2;  // top-right
        Vector2 br = v3;  // bottom-right
        Vector2 bl = v0;  // bottom-left
        float   r  = CornerRadius;

        // 顺时针：top-left → top-right → bottom-right → bottom-left
        p.MoveTo(new Vector2(tl.x + r, tl.y));

        // top 边到 top-right 弧角
        p.LineTo(new Vector2(tr.x - r, tr.y));
        p.ArcTo(tr, new Vector2(tr.x, tr.y + r), r);

        // right 边到 bottom-right 弧角
        p.LineTo(new Vector2(br.x, br.y - r));
        p.ArcTo(br, new Vector2(br.x - r, br.y), r);

        // bottom 边到 bottom-left 弧角
        p.LineTo(new Vector2(bl.x + r, bl.y));
        p.ArcTo(bl, new Vector2(bl.x, bl.y - r), r);

        // left 边到 top-left 弧角
        p.LineTo(new Vector2(tl.x, tl.y + r));
        p.ArcTo(tl, new Vector2(tl.x + r, tl.y), r);

        p.ClosePath();

        // ④ 填充 + 描边
        p.Fill();
        p.Stroke();
    }
}
