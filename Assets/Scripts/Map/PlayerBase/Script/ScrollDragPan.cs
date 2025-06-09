// Assets/Scripts/Game/UI/ScrollViewPan.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 给 ScrollView 加“按住拖动画布”能力，并在页面隐藏 / 再显示时
/// 自动保存并恢复 scrollOffset（相当于记住镜头位置）。<br/>
/// ① 不抢 Button / 热区（class = "hot-zone"）的事件。<br/>
/// ② 自动 Clamp，不会拖出边界。<br/>
/// ③ 隐掉滚动条。<br/>
/// 用法：把脚本挂在带 UIDocument 的 GameObject 上，设置 scrollViewName。<br/>
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class ScrollViewPan : MonoBehaviour
{
    [SerializeField] string scrollViewName = "PlayerBaseScroll";
    [SerializeField] string hotZoneClass   = "hot-zone";

    ScrollView sv;

    // —— 静态：同名 ScrollView 共享一份偏移缓存 —— //
    static readonly Dictionary<string, Vector2> SavedOffset = new();

    void OnEnable()
    {
        sv = GetComponent<UIDocument>().rootVisualElement.Q<ScrollView>(scrollViewName);
        if (sv == null) { Debug.LogError($"[ScrollViewPan] 找不到 ScrollView: {scrollViewName}"); return; }

        // 1. 隐掉滚动条（千万别 display:none，会让布局失效）
        sv.verticalScrollerVisibility   = ScrollerVisibility.Hidden;
        sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        sv.verticalScroller.style.opacity   = 0;
        sv.horizontalScroller.style.opacity = 0;

        // 2. 恢复上一次保存的偏移
        if (SavedOffset.TryGetValue(scrollViewName, out var offset))
        {
            // 必须等到下一帧（布局已完成）再设置，否则 0,0 尺寸时会被 UI Toolkit 覆盖
            sv.schedule.Execute(() => sv.scrollOffset = offset).ExecuteLater(0);
        }

        // 3. 给 contentContainer 添加拖拽 Manipulator
        sv.contentContainer.AddManipulator(new PanManipulator(sv, hotZoneClass));
    }

    // 当 GameObject 被 SetActive(false) 或脚本被关闭时触发
    void OnDisable()
    {
        if (sv != null)
        {
            SavedOffset[scrollViewName] = sv.scrollOffset; // 记录当前镜头
        }
    }
}
