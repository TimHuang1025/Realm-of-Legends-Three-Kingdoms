// Assets/Scripts/Game/UI/ScrollViewPan.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem.EnhancedTouch;  // ★ ①

[RequireComponent(typeof(UIDocument))]
public sealed class ScrollViewPan : MonoBehaviour
{
    [Header("UI 元素名字 / class")]
    [SerializeField] string scrollViewName = "PlayerBaseScroll";
    [SerializeField] string hotZoneClass   = "hot-zone";

    [Header("缩放参数")]
    [SerializeField] float minZoom       = 0.5f;
    [SerializeField] float maxZoom       = 3f;
    [SerializeField] float wheelStep     = 0.05f;

    [Header("拖拽参数")]
    [Tooltip("PointerMove 隔帧处理：0 每帧，1 隔 1 帧")]
    [SerializeField] int   moveSkipFrame = 1;

    ScrollView sv;
    static readonly Dictionary<string, Vector2> SavedOffset = new();

    void Awake()
    {
        /* ★ ① ：开启多点触控，捏合才能生效 */
        EnhancedTouchSupport.Enable();
    }

    void OnEnable()
    {
        sv = GetComponent<UIDocument>().rootVisualElement.Q<ScrollView>(scrollViewName);
        if (sv == null)
        {
            Debug.LogError($"[ScrollViewPan] 找不到 ScrollView: {scrollViewName}");
            return;
        }

        /* 1. 隐掉滚动条（勿 display:none） */
        sv.verticalScrollerVisibility   = ScrollerVisibility.Hidden;
        sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        sv.verticalScroller.style.opacity   = 0;
        sv.horizontalScroller.style.opacity = 0;

        /* 2. 恢复上一次保存的偏移 */
        if (SavedOffset.TryGetValue(scrollViewName, out var offset))
            sv.schedule.Execute(() => sv.scrollOffset = offset).ExecuteLater(0);

        /* 3. ★ 先确保 scale = 1，再挂新版操控器 */
        sv.contentContainer.transform.scale = Vector3.one;   // ★ ②

        var manip = new PanManipulator(sv, hotZoneClass)     // ★ ③
        {
            minZoom       = minZoom,
            maxZoom       = maxZoom,
            wheelStep     = wheelStep,
            moveSkipFrame = moveSkipFrame
        };
        sv.contentContainer.AddManipulator(manip);
    }

    void OnDisable()
    {
        if (sv != null)
            SavedOffset[scrollViewName] = sv.scrollOffset;   // 记录镜头
    }
}
