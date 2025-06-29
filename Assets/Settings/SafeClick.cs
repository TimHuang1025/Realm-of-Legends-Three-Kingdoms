// -----------------------------------------------------------------------------
// UIExt.SafeClickFocus.cs   2025-06-29
// 在 ScrollView 中避免误触：
//   PointerUp 时若位移 > dragThreshold(px)   => 认定滚动，啥都不做
//   否则                                     => 真点击 → Focus + onClick
// -----------------------------------------------------------------------------
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIExt
{
    public static class SafeClickFocusExtension
    {
        public static void MakeSafeClickable(this VisualElement ve,
                                             Action onClick,
                                             float dragThreshold = 40f) // ← ☆调到 40 px（或你想要的）
        {
            // 把 Button 自带 Clickable 去掉，彻底接管点击判断
            if (ve is Button btn && btn.clickable != null)
                btn.RemoveManipulator(btn.clickable);

            AttachSafeClick(ve, onClick, dragThreshold);
        }

        // ---------------- 内部实现 ----------------
        private static void AttachSafeClick(VisualElement ve,
                                            Action onClick,
                                            float dragThreshold)
        {
            const int LEFT_BTN = 0;

            bool    pressed  = false;
            int     pointer  = -1;
            Vector3 pressPos = Vector3.zero;
            float   thrSq    = dragThreshold * dragThreshold;

            // PointerDown —— 记录初始位置，并 **马上 Blur** 防止闪一下焦点
            ve.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != LEFT_BTN || pressed) return;

                ve.Blur();                      // 清掉默认焦点
                pressed  = true;
                pointer  = e.pointerId;
                pressPos = e.position;

                // 不捕获指针，让 ScrollView 正常滚动
                e.StopPropagation();            // 阻止再冒泡给父元素 Focus
            }, TrickleDown.TrickleDown);

            // PointerUp —— 根据移动距离决定“点击”还是“拖动”
            ve.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!pressed || e.pointerId != pointer) return;
                pressed = false;
                pointer = -1;

                e.StopPropagation();

                // 位移检查
                if ((e.position - pressPos).sqrMagnitude > thrSq)
                    return;                     // 认定滚动 ⇒ 不触发点击

                // 真·点击：先 Focus，再调用业务逻辑
                ve.Focus();
                onClick?.Invoke();
            }, TrickleDown.TrickleDown);

            // PointerCancel —— 意外打断时清状态
            ve.RegisterCallback<PointerCancelEvent>(_ =>
            {
                pressed = false;
                pointer = -1;
            });
        }
    }
}
