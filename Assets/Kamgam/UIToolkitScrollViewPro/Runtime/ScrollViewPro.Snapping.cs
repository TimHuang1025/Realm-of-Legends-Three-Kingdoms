using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScrollViewPro
{
    public partial class ScrollViewPro
    {


        const bool DefaultSnapValue = false;
        /// <summary>
        /// Is snapping enabled or disabled.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("snap")]
#endif
        public bool snap { get; set; } = DefaultSnapValue;

        const float DefaultSnapDurationSec = 0.4f;
        /// <summary>
        /// The duration of the snap animation in seconds.<br />
        /// The snap animation uses ScrollToAnimated() internally so snapping
        /// can be cancelled the same was ScrollToAnimated is cancelled.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("snap-duration-sec")]
#endif
        public float snapDurationSec { get; set; } = DefaultSnapDurationSec;

        const ScrollToAlign DefaultSnapAlignX = ScrollToAlign.Center;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("snap-align-x")]
#endif
        public ScrollToAlign snapAlignX { get; set; } = DefaultSnapAlignX;

        const ScrollToAlign DefaultSnapAlignY = ScrollToAlign.Center;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("snap-align-y")]
#endif
        public ScrollToAlign snapAlignY { get; set; } = DefaultSnapAlignY;

        const Easing DefaultSnapEase = Easing.Ease;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("snap-ease")]
#endif
        public Easing snapEase { get; set; } = DefaultSnapEase;

        #if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("snap-edge-clamp-x")]
        #endif
        public bool snapEdgeClampX { get; set; } = false;

        #if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("snap-edge-clamp-y")]
        #endif
        public bool snapEdgeClampY { get; set; } = false;


        static Vector4 DefaultSnapMargin = Vector4.zero;
        /// <summary>
        /// Extra margin to add around the snap target. Order: top, right, bottom, left, (positive = outwards, negative = inwards).<br />
        /// If snapIncludeMargin is true then these are ADDED to the snap targets default margins.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("snap-margin")]
#endif
        public Vector4 snapMargin { get; set; } = DefaultSnapMargin;

        static bool DefaultSnapIncludeMargin = false;
        /// <summary>
        /// Whether the targets default marings should be used when snapping.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("snap-include-margin")]
#endif
        public bool snapIncludeMargin { get; set; } = DefaultSnapIncludeMargin;

        static string DefaultSnapTargetClasses = "";
        protected string[] _snapTargetClassNameList = null;
        /// <summary>
        /// Limit the snap targets to these class names.
        /// </summary>
        protected string _snapTargetClasses = DefaultSnapTargetClasses;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("snap-target-classes")]
#endif
        public string snapTargetClasses
        {
            get
            {
                return _snapTargetClasses;
            }

            set
            {
                if (_snapTargetClasses != value)
                {
                    _snapTargetClasses = value;
                    if (string.IsNullOrEmpty(value))
                    {
                        _snapTargetClassNameList = null;
                    }
                    else
                    {
                        _snapTargetClassNameList = value.Split(',');
                        for (int i = 0; i < _snapTargetClassNameList.Length; i++)
                        {
                            _snapTargetClassNameList[i] = _snapTargetClassNameList[i].Trim();
                        }
                    }
                }
            }
        }

        static bool DefaultSnapTargetFocusables = false;
        /// <summary>
        /// If enabled then the snap targets will be limited to only focusable children
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("snap-target-focusables")]
#endif
        public bool snapTargetFocusables { get; set; } = DefaultSnapTargetFocusables;

        const float DefaultSnapVelocityThreshold = 1.25f;
        /// <summary>
        /// If the velocity falls below this then it will start snapping.<br />
        /// This is not in pixels. It's in panel side-lengths per second.<br />
        /// The ratio is multiplied by the bigger side of panel to get the
        /// final threshold in pixels per second.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("snap-velocity-threshold")]
#endif
        public float snapVelocityThreshold { get; set; } = DefaultSnapVelocityThreshold;

        // This is the velocity is in pixels per second and depends on the panel resolution.
        protected float _snapInertiaVelocityThresholdInPixels = -1f;

        protected void updateSnapInertiaThreshold()
        {
            if (_snapInertiaVelocityThresholdInPixels > 0)
                return;

            float panelBiggerSide = 1200f;
            if (panel != null && panel.contextType == ContextType.Player && !float.IsNaN(panel.visualTree.layout.width))
            {
                panelBiggerSide = Mathf.Max(panel.visualTree.layout.width, panel.visualTree.layout.height);
            }

            _snapInertiaVelocityThresholdInPixels = snapVelocityThreshold * panelBiggerSide;
        }

        public void SnapDelayed()
        {
            schedule.Execute(Snap);
        }

        float _lastSnapDueToMouseWheelTime = 0f;

        private void onMouseWheelUsed(WheelEvent evt)
        {
            // If snapping then using the mouse wheel will trigger scroll next/previous
            // event. However, using the scroll wheel triggers this multiple times so we
            // only act on the first and ignore the rest within a certain timeframe or
            // else the little stutter would occur.
            if (snap)
            {
                float deltaTime = Time.realtimeSinceStartup - _lastSnapDueToMouseWheelTime;
                if (deltaTime > 0.2f)
                {
                    _lastSnapDueToMouseWheelTime = Time.realtimeSinceStartup;

                    // Fake velocity to make it jump to the next element.
                    float velocity = Mathf.Sign(evt.delta.y) * 100f * Mathf.Sign(mouseWheelScrollSize);
                    SnapDirection(velocity, delayed: true);
                }
                else
                {
                    // Stop the wheel event from propagating or else these would override
                    // the scroll position immediately, effevtively stopping any snap.
                    evt.StopPropagation();
                }
            }
        }

        public void SnapDirection(float velocity, bool delayed = false)
        {
            if (_mode == ScrollViewMode.Horizontal)
                _velocity = new Vector2(velocity, 0f);
            else
                _velocity = new Vector2(0f, -velocity);

            if (delayed)
                SnapDelayed();
            else
                Snap();
        }

        public void SnapNext()
        {
            SnapDirection(100f);
        }

        public void SnapPrevious()
        {
            SnapDirection(-100f);
        }
        
        public void SnapToItem(VisualElement target, bool animate = true)
        {
            if (target == null)
                return;

            // ① 先滚到可见（跟 ScrollTo 一样）
            ScrollTo(target);

            // ② 组装边距 = 额外 Margin + (可选) 目标自身 Margin
            var margins = snapMargin;
            if (snapIncludeMargin)
            {
                margins.x += target.resolvedStyle.marginLeft;
                margins.y += target.resolvedStyle.marginTop;
                margins.z += target.resolvedStyle.marginRight;
                margins.w += target.resolvedStyle.marginBottom;
            }

            // ③ 调用 ScrollToAnimated 做真正对齐
            float duration = animate ? snapDurationSec : 0f;
            var   ease     = animate ? snapEase       : Easing.BackOut;

            ScrollToAnimated(
                target,
                duration,
                ease,
                snapAlignX,
                snapAlignY,
                margins
            );

            // ④ 触发与内置 Snap 相同的事件
            SnapEvent.Dispatch(target, this);
        }

        public void Snap()
        {
            // ① 找到本次要对齐的目标元素
            var target = findSnapTarget(_velocity, true, snapAlignX, snapAlignY);
            if (target == null)
                return;

            // ② 组装 margin
            var margin = snapMargin;
            if (snapIncludeMargin)
            {
                margin.x += target.resolvedStyle.marginLeft;   // top
                margin.y += target.resolvedStyle.marginTop;    // right
                margin.z += target.resolvedStyle.marginRight;  // bottom
                margin.w += target.resolvedStyle.marginBottom; // left
            }

            // ③ 决定本次 X / Y 对齐方式
            ScrollToAlign alignX = snapAlignX;   // 默认用全局
            ScrollToAlign alignY = snapAlignY;

            const float eps = 1f;                // 容差像素

            if (snapEdgeClampX)
            {
                if (scrollOffset.x <= eps)
                    alignX = ScrollToAlign.Start;                      // 首列
                else if (scrollOffset.x >= scrollableWidth - eps)
                    alignX = ScrollToAlign.End;                        // 末列
            }

            if (snapEdgeClampY)
            {
                if (scrollOffset.y <= eps)
                {
                    alignY  = ScrollToAlign.Start;                     // 第一行
                    margin.x = 0f;                                     // 去掉 top margin
                }
                else if (scrollOffset.y >= scrollableHeight - eps)
                {
                    alignY  = ScrollToAlign.End;                       // 最后一行
                    margin.z = 0f;                                     // 去掉 bottom margin
                }
            }

            // ④ 执行补间对齐
            var dist = ScrollToAnimated(
                target,
                snapDurationSec,
                snapEase,
                alignX,
                alignY,
                margin
            );

            // ⑤ 触发 SnapEvent（保持官方行为）
            if (dist.sqrMagnitude > dragThreshold * dragThreshold)
                SnapEvent.Dispatch(target, this);
        }


        // Called while interia animation at the end of the seach step.
        protected void handleSnappingWhileInteriaAnimation()
        {
            if (!snap)
                return;

            // If unrestricted then check if outside bounds. If yes, then start snapping.
            if (touchScrollBehavior == ScrollView.TouchScrollBehavior.Unrestricted)
            {
                if (scrollOffset.x < 0f || scrollOffset.x > scrollableWidth || scrollOffset.y < 0f || scrollOffset.y > scrollableHeight)
                {
                    StopAnimations();
                    Snap();
                    return;
                }
            }

            updateSnapInertiaThreshold();

            // Start snapping if below a certain velocity
            if (_velocity.sqrMagnitude < _snapInertiaVelocityThresholdInPixels * _snapInertiaVelocityThresholdInPixels)
            {
                StopAnimations();
                Snap();
            }
        }

        protected List<VisualElement> _tmpSnapTargets = new List<VisualElement>(10);

        /// <summary>
        /// Find the closes child to the center of the viewport OR if a child
        /// is overlapping the center then that child is used.
        /// </summary>
        /// <returns></returns>
        protected VisualElement findSnapTarget(
            Vector2 velocity, bool takeVelocityIntoAccount = true,
            ScrollToAlign snapAlignX = ScrollToAlign.Center,
            ScrollToAlign snapAlignY = ScrollToAlign.Center)
        {
            var viewBounds = contentViewport.worldBound;

            Vector2 referencePoint = viewBounds.center;
            if (snapAlignX == ScrollToAlign.Start)
                referencePoint.x = viewBounds.xMin;
            else if (snapAlignX == ScrollToAlign.End)
                referencePoint.x = viewBounds.xMax;
            if (snapAlignY == ScrollToAlign.Start)
                referencePoint.y = viewBounds.yMin;
            else if (snapAlignY == ScrollToAlign.End)
                referencePoint.y = viewBounds.yMax;

            VisualElement closestChild = null;
            float sqrMinDistance = float.MaxValue;
            Vector2 distance;

            _tmpSnapTargets.Clear();
            if (snapTargetClasses != null && snapTargetClasses.Length > 0)
            {
                foreach (var className in _snapTargetClassNameList)
                {
                    foreach (var child in contentContainer.Query<VisualElement>(classes: className).Build())
                    {
                        _tmpSnapTargets.Add(child);
                    }
                }
            }
            else
            {
                foreach (var child in contentContainer.Children())
                {
                    _tmpSnapTargets.Add(child);
                }
            }
            

            foreach (var child in _tmpSnapTargets)
            {
                if (!child.focusable && snapTargetFocusables)
                    continue;

                var childBounds = child.worldBound;

                distance = (childBounds.center - referencePoint);

                if (takeVelocityIntoAccount)
                {
                    // Skip elements "behind" the current velocity direction.
                    // We do this to skip elements that would make it jump backwards.
                    // It also leads to skipping big elements if we are beyond their center.
                    // This gives a much more predictable snap behaviour.
                    // CAVEAT: It may skip ALL children so we need an extra run afterwards.
                    if (_velocity.sqrMagnitude > 10f)
                    {
                        if (Vector2.Dot(_velocity, distance) < 0f)
                        {
                            continue;
                        }
                    }
                }

                // Special case: View bound center is INSIDE the child.
                // In that case always use this child.
                if (childBounds.Contains(referencePoint))
                {
                    closestChild = child;
                    break;
                }

                // Otherwise compare by center distance
                float sqrDistance;
                switch (mode)
                {
                    case ScrollViewMode.Vertical:
                        sqrDistance = distance.y * distance.y;
                        break;
                    case ScrollViewMode.Horizontal:
                        sqrDistance = distance.x * distance.x;
                        break;
                    case ScrollViewMode.VerticalAndHorizontal:
                    default:
                        sqrDistance = distance.sqrMagnitude;
                        break;
                }
                if (sqrDistance < sqrMinDistance)
                {
                    sqrMinDistance = sqrDistance;
                    closestChild = child;
                }
            }

            _tmpSnapTargets.Clear();

            // None found? Repeat but without the velocity part
            if (closestChild == null && takeVelocityIntoAccount)
            {
                closestChild = findSnapTarget(velocity, takeVelocityIntoAccount: false, snapAlignX, snapAlignY);
            }

            return closestChild;
        }
    }
}
