using UnityEngine;
using DG.Tweening; // 如果你有DOTween

/// <summary>
/// COC风格的UI动画增强
/// 添加到FloatingBuildingUI同一个GameObject上
/// </summary>
public class COCStyleUIAnimator : MonoBehaviour
{
    [Header("动画设置")]
    [SerializeField] private float bounceScale = 1.2f;
    [SerializeField] private float bounceDuration = 0.3f;
    [SerializeField] private float floatAmount = 0.1f;
    [SerializeField] private float floatSpeed = 2f;
    
    private Vector3 originalScale;
    private float floatOffset;
    
    void Start()
    {
        originalScale = transform.localScale;
        
        // 出现时的弹跳动画
        PlayBounceAnimation();
    }
    
    void Update()
    {
        // 轻微上下浮动
        floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
    }
    
    public void PlayBounceAnimation()
    {
        // 如果有DOTween
        #if DOTWEEN
        transform.localScale = originalScale * 0.5f;
        transform.DOScale(originalScale * bounceScale, bounceDuration * 0.5f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOScale(originalScale, bounceDuration * 0.5f)
                    .SetEase(Ease.InOutQuad);
            });
        #else
        // 没有DOTween的简单实现
        StartCoroutine(BounceCoroutine());
        #endif
    }
    
    System.Collections.IEnumerator BounceCoroutine()
    {
        float elapsed = 0;
        Vector3 startScale = originalScale * 0.5f;
        
        // 放大阶段
        while (elapsed < bounceDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (bounceDuration * 0.5f);
            transform.localScale = Vector3.Lerp(startScale, originalScale * bounceScale, t);
            yield return null;
        }
        
        // 缩小到正常大小
        elapsed = 0;
        while (elapsed < bounceDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (bounceDuration * 0.5f);
            transform.localScale = Vector3.Lerp(originalScale * bounceScale, originalScale, t);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    public float GetFloatOffset()
    {
        return floatOffset;
    }
}

/// <summary>
/// COC风格的UI外观调整建议
/// </summary>
public static class COCStyleGuide
{
    // UI设计建议：
    // 1. 使用圆角矩形背景
    // 2. 按钮使用明亮的绿色/蓝色
    // 3. 文字使用粗体白色，带黑色描边
    // 4. 背景使用半透明黑色(0,0,0,0.8)
    // 5. 添加轻微的阴影效果
    
    public static Color ActionButtonColor = new Color(0.2f, 0.8f, 0.2f); // 绿色
    public static Color MoveButtonColor = new Color(0.2f, 0.6f, 1f);     // 蓝色
    public static Color BackgroundColor = new Color(0, 0, 0, 0.8f);      // 半透明黑
}