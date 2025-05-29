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
        const string DragIgnoreClassName = "svp-drag-ignore";
        const string DragAllowClassName = "svp-drag-allow";

        /// <summary>
        /// Whether the scroll view is currently being dragged or not.
        /// </summary>
        public bool isDragging { get => _dragState == DragState.Dragging; }

        public const int FallbackAnimationFps = 60;
        const int UndefinedAnimationFps = -1;

        protected int _animationFps = UndefinedAnimationFps;

        /// <summary>
        /// Defines how quickly the animations will be updated.<br />
        /// If set to -1 then Application.targetFrameRate will be used.<br />
        /// If Application.targetFrameRate is -1 too then FallbackAnimationFps (60) will be used.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("animation-fps")]
#endif
        public int animationFps
        {
            get => _animationFps;
            set
            {
                if (_animationFps != value)
                {
                    _animationFps = value;
                    int fps = value;
                    if (fps <= 0)
                        fps = Application.targetFrameRate;
                    if (fps <= 0)
                        fps = FallbackAnimationFps;

                    _animationFrameDurationInMS = 1000 / fps;
                }
            }
        }

        protected int _animationFrameDurationInMS = 16;


        public const bool DefaultDragEnabled = true;
        /// <summary>
        /// Specifies whether the scrollbars should have buttons or not.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("drag-enabled")]
#endif
        public bool dragEnabled { get; set; } = DefaultDragEnabled;


        public const float DefaultDragThreshold = 20f;
        /// <summary>
        /// If the mouse moves more than the sqrt of this distance then it is treated as a drag/scroll event.
        /// Otherwise it is treated as a click. 
        /// This threshold is also used for snap activation. Only after a drag has been started the onSnap will be triggered.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("drag-threshold")]
#endif
        [Tooltip("If the mouse moves more than the sqrt of this distance then it is treated as a drag/scroll event. Otherwise it is treated as a click.\n" +
            "This threshold is also used for snap activation. Only after a drag has been started the onSnap will be triggered.")]
        public float dragThreshold { get; set; } = DefaultDragThreshold;

        float _lastVelocityLerpTime;
        protected Vector2 _velocity;
        protected IVisualElementScheduledItem _inertiaAndElasticityAnimation;

        // Mouse pos an offset are used for dragging AND child threshold detection.
        public enum DragState
        {
            Idle,
            CheckingThreshold,
            Dragging
        }

        [System.NonSerialized]
        protected DragState _dragState = DragState.Idle;
        protected int _dragPointerId;
        protected Vector2 _dragPointerDownPos;
        protected Vector2 _dragPointerDownScrollOffset;
        protected VisualElement _dragPropagationTarget;
        protected IEventHandler _dragCaptureTarget;

        // Drag pointer id is only set if the contentContainer has captured the pointer for dragging.
        protected int _capturedDragPointerId = PointerId.invalidPointerId;
        protected IEventHandler _capturedDragHandler = null;

        protected IVisualElementScheduledItem _startDraggingTask;

        protected void captureDragPointer(IEventHandler handler, int pointerId)
        {
            if (pointerId == PointerId.invalidPointerId)
                return;

            if (_capturedDragHandler != null)
                releaseDragPointer();

            if (!PointerCaptureHelper.HasPointerCapture(handler, pointerId))
            {
                PointerCaptureHelper.CapturePointer(handler, pointerId);
            }
            _capturedDragHandler = handler;
            _capturedDragPointerId = pointerId;
        }

        protected bool isCapturedDragPointerId(int pointerId)
        {
            return pointerId != PointerId.invalidPointerId && pointerId == _capturedDragPointerId;
        }

        protected bool hasCapturedDragPointer()
        {
            return _capturedDragPointerId != PointerId.invalidPointerId
                && _capturedDragHandler != null
                && PointerCaptureHelper.HasPointerCapture(_capturedDragHandler, _capturedDragPointerId);
        }

        protected void releaseDragPointer()
        {
            if (_capturedDragPointerId == PointerId.invalidPointerId)
                return;

            if (_capturedDragHandler == null)
                return;

            if (PointerCaptureHelper.HasPointerCapture(_capturedDragHandler, _capturedDragPointerId))
            {
                PointerCaptureHelper.ReleasePointer(_capturedDragHandler, _capturedDragPointerId);
            }

            _capturedDragPointerId = PointerId.invalidPointerId;
            _capturedDragHandler = null;
        }

        protected void startDragThresholdTracking(PointerDownEvent evt, IEventHandler captureTarget)
        {
            if (!dragEnabled)
                return;

            if (_dragState != DragState.Idle)
                return;

            _dragState = DragState.CheckingThreshold;
            _dragPointerDownPos = evt.position;
            _dragPointerId = evt.pointerId;
            _dragPropagationTarget = evt.target as VisualElement;
            _dragCaptureTarget = captureTarget;
        }

        protected bool hasSurpassedDragThreshold(Vector3 mousePostion)
        {
            if (!dragEnabled)
                return false;

            var movementSinceDown = (Vector2)mousePostion - _dragPointerDownPos;
            return movementSinceDown.sqrMagnitude > dragThreshold * dragThreshold;
        }

        protected void startDragging(Vector3 pointerPosition, int pointerId, bool capturePointer, bool bubbleDragEvents, IEventHandler captureTarget, VisualElement propagationTarget)
        {
            if (!dragEnabled || isDragging || (capturePointer && hasCapturedDragPointer()))
                return;

            calculateBounds();

            if (capturePointer)
                captureDragPointer(captureTarget, pointerId);

            _dragPointerDownPos = pointerPosition;
            _dragPointerDownScrollOffset = scrollOffset;

            _dragState = DragState.Dragging;

            if (bubbleDragEvents)
                DragStartEvent.Dispatch(propagationTarget, this, pointerPosition, pointerId);
        }

        // This is called if a child scroll view has started dragging and it was configured to forward (bubble) the drag events to its parents.
        protected void onBubbledDragStartReceived(DragStartEvent evt)
        {
            startDragging(evt.pointerPosition, evt.pointerId, capturePointer: false, bubbleDragEvents: false, captureTarget: null, propagationTarget: null);
        }

        protected void stopDragging(bool startAnimation, bool releasePointer, bool bubbleDragEvents)
        {
            if (!dragEnabled)
                return;

            if (!isDragging)
                return;

            if (releasePointer)
                releaseDragPointer();

            if (hasInertia || touchScrollBehavior == ScrollView.TouchScrollBehavior.Elastic)
            {
                if (startAnimation)
                {
                    startInertiaAndElasticityAnimation();
                }
            }

            _dragState = DragState.Idle;

            if (bubbleDragEvents)
                DragStopEvent.Dispatch(this, this);
        }

        // This is called if a child scroll view has stopped dragging and it was configured to forward (bubble) the drag events to its parents.
        protected void onBubbledDragStopReceived(DragStopEvent evt)
        {
            stopDragging(startAnimation: true, releasePointer: false, bubbleDragEvents: false);
        }

        /// <summary>
        /// Returns true if the event was cancelled.
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        protected bool ignoreDragAndCancelEvent(EventBase evt, bool cancelEvent, bool ignoreIfInOtherScrollView = true)
        {
            if (evt.target == null)
                return false;

            if (ignoreIfInOtherScrollView && (evt.target as VisualElement).GetFirstAncestorOfType<ScrollViewPro>() != this)
                return true;

            // If the element or a parent has the DragIgnoreClassName class then it is considered ignored UNLESS
            // the element itself also has the DragAllowClassName name.
            var ve = evt.target as VisualElement;
            if (ve != null && ve.IsChildOfClass(DragIgnoreClassName, DragAllowClassName, includeSelf: true, preferNegative: true))
            {
                if (cancelEvent)
                    evt.StopImmediatePropagation();

                return true;
            }

            return false;
        }

        protected void onPointerDown(PointerDownEvent evt)
        {
            if (ignoreDragAndCancelEvent(evt, cancelEvent: false))
            {
                return;
            }

            StopAnimations();
            startDragThresholdTracking(evt, contentContainer);
        }

        // Necessary because in infinite scroll views the contentContainer (which we
        // normally subscribe our events to) will be outside the viewport rather quickly.
        protected void onPointerDownOnViewport(PointerDownEvent evt)
        {
            if (ignoreDragAndCancelEvent(evt, cancelEvent: false))
                return;

            if (infinite)
                startDragThresholdTracking(evt, contentContainer);
        }

        public void StopAnimations()
        {
            _inertiaAndElasticityAnimation?.Pause();
            _scrollWheelScheduledAnimation?.Pause();

            StopScrollToAnimation(); // See "ScrollToAnimated".
        }

        protected void onPointerMove(PointerMoveEvent evt)
        {
            onPointerMove(evt, checkPointerCapture: true, bubbleDragEvents: true);
        }

        protected void onPointerMove(PointerMoveEvent evt, bool checkPointerCapture, bool bubbleDragEvents, ScrollViewMode movementConstraint = ScrollViewMode.VerticalAndHorizontal, bool ignoreIfInOtherScrollView = true)
        {
            if (!dragEnabled)
                return;

            if (_dragState == DragState.Idle)
                return;

            if (ignoreDragAndCancelEvent(evt, cancelEvent: false, ignoreIfInOtherScrollView))
                return;

            if (_dragState == DragState.CheckingThreshold)
            {
                // If nothing is pressed but _dragState is still checking then we have a case of missing PointerUp event (it happens, TODO: investigte, report bug).
                if (evt.isPrimary && evt.pressedButtons == 0)
                {
                    _pointerDownOnChild = false;
                    _dragState = DragState.Idle;
                }
                else
                {
                    if (hasSurpassedDragThreshold(evt.position))
                    {
                        _pointerDownOnChild = false;

                        // We could use _dragPointerDownPos here but that would make the drag a bit jumpy at the start.
                        startDragging(evt.position, evt.pointerId, checkPointerCapture, bubbleDragEvents, _dragCaptureTarget, _dragPropagationTarget);
                    }
                }
            }
            else if (_dragState == DragState.Dragging)
            {
                // If nothing is pressed but _dragState is still checking then we have a case of missing PointerUp event (it happens, TODO: investigte, report bug).
                if (evt.isPrimary && evt.pressedButtons == 0)
                {
                    // We hav to delay because if there will be an UP event it may come AFTER wards (and we
                    // don't want to double stop dragging, especially with startAnimation=false).
                    schedule.Execute(() =>
                    {
                        if (isDragging) // Still dragging? Okay, then no UP event was fired and we force stop dragging.
                        {
                            stopDragging(startAnimation: false, releasePointer: true, bubbleDragEvents: true);
                            _pointerDownOnChild = false;
                        }
                    });
                }
                else
                {
                    if (!checkPointerCapture || hasCapturedDragPointer())
                    {
                        handleDrag(evt, movementConstraint);

                        if (bubbleDragEvents)
                            DragMoveEvent.Dispatch(evt.target as VisualElement, this, evt);
                    }
                }
            }
        }

        // This is called if a child scroll view has stopped dragging and it was configured to forward (bubble) the drag events to its parents.
        private void onBubbledMoveReceived(DragMoveEvent evt)
        {
            // We automatically constrain the parent scroll view direction to the inverse of the scroll view.
            // This means that is a child scroll view is scrolling only horizontally then the parent will only scroll vertically and vice versa.
            ScrollViewMode constraint = ScrollViewMode.VerticalAndHorizontal; // No constraint by default.
            if (evt.scrollView.mode != ScrollViewMode.VerticalAndHorizontal)
            {
                constraint = evt.scrollView.mode == ScrollViewMode.Horizontal ? ScrollViewMode.Vertical : ScrollViewMode.Horizontal;
            }
            onPointerMove(evt.pointerMoveEvent, checkPointerCapture: false, bubbleDragEvents: false, constraint, ignoreIfInOtherScrollView: false);
        }

        protected void handleDrag(PointerMoveEvent evt, ScrollViewMode movementConstraint = ScrollViewMode.VerticalAndHorizontal)
        {
            Vector2 deltaPos = (Vector2)evt.position - _dragPointerDownPos;

            // Scroll offset is the inverse of position, thus we have to subtract deltaPos.
            var newScrollOffset = _dragPointerDownScrollOffset - deltaPos;

            // Clamp based on scroll behaviour
            if (touchScrollBehavior == ScrollView.TouchScrollBehavior.Clamped)
            {
                newScrollOffset = Vector2.Max(newScrollOffset, _lowBounds);
                newScrollOffset = Vector2.Min(newScrollOffset, _highBounds);
            }
            else if (touchScrollBehavior == ScrollView.TouchScrollBehavior.Elastic)
            {
                newScrollOffset.x = computeElasticOffset(
                    scrollOffset.x,
                    deltaPos.x, _dragPointerDownScrollOffset.x,
                    _lowBounds.x, _lowBounds.x - contentViewport.resolvedStyle.width,
                    _highBounds.x, _highBounds.x + contentViewport.resolvedStyle.width);

                newScrollOffset.y = computeElasticOffset(
                    scrollOffset.y,
                    deltaPos.y, _dragPointerDownScrollOffset.y,
                    _lowBounds.y, _lowBounds.y - contentViewport.resolvedStyle.height,
                    _highBounds.y, _highBounds.y + contentViewport.resolvedStyle.height);
            }

            // Reset x or y based on scroll mode.
            switch (mode)
            {
                case ScrollViewMode.Vertical:
                    newScrollOffset.x = scrollOffset.x;
                    break;
                case ScrollViewMode.Horizontal:
                    newScrollOffset.y = scrollOffset.y;
                    break;
                default:
                    break;
            }

            // Reset x or y based on movement Constraint
            switch (movementConstraint)
            {
                case ScrollViewMode.Vertical:
                    newScrollOffset.x = scrollOffset.x;
                    break;
                case ScrollViewMode.Horizontal:
                    newScrollOffset.y = scrollOffset.y;
                    break;
                default:
                    break;
            }

            // Calculate velocity
            // Velocity is updated just like in Unitys own ScrollView.
            if (hasInertia)
            {
                if (scrollOffset == _lowBounds || scrollOffset == _highBounds)
                {
                    _velocity = Vector2.zero;
                }
                else
                {
                    if (_lastVelocityLerpTime > 0f)
                    {
                        float dT = Time.unscaledTime - _lastVelocityLerpTime;
                        _velocity = Vector2.Lerp(_velocity, Vector2.zero, dT * 10f);
                    }

                    _lastVelocityLerpTime = Time.unscaledTime;
                    float unscaledDeltaTime = Time.unscaledDeltaTime;
                    Vector2 b = (newScrollOffset - scrollOffset) / unscaledDeltaTime;
                    _velocity = Vector2.Lerp(_velocity, b, unscaledDeltaTime * 10f);
                }
            }

            // Finally set new scroll offset.
            scrollOffset = newScrollOffset;
        }

        protected void onPointerCancel(PointerCancelEvent evt)
        {
            Debug.Log("cancel");

            if (_dragState == DragState.CheckingThreshold)
            {
                _dragState = DragState.Idle;
            }
            else if (_dragState == DragState.Dragging)
            {
                if (!isCapturedDragPointerId(evt.pointerId))
                    return;

                if (ignoreDragAndCancelEvent(evt, cancelEvent: false))
                    return;

                stopDragging(startAnimation: false, releasePointer: true, bubbleDragEvents: true);
            }
        }

        protected void onPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            // Cancel drag thresholdchecking if the capture changes
            // (i.e. if a button or some other interactive element is clicked).
            if (_dragState == DragState.CheckingThreshold)
            {
                _dragState = DragState.Idle;
            }
        }

        protected void onPointerUp(PointerUpEvent evt)
        {
            if (_dragState == DragState.CheckingThreshold)
            {
                _dragState = DragState.Idle;
            }
            else if (_dragState == DragState.Dragging)
            {
                if (!isCapturedDragPointerId(evt.pointerId))
                    return;

                if (ignoreDragAndCancelEvent(evt, cancelEvent: false))
                    return;

                stopDragging(startAnimation: true, releasePointer: true, bubbleDragEvents: true);

                if (snap && !hasInertia)
                    Snap();
            }
        }

        protected bool _pointerDownOnChild = false;

        protected void onPointerDownOnChild(PointerDownEvent evt)
        {
            if (!dragEnabled)
                return;

            if (ignoreDragAndCancelEvent(evt, cancelEvent: false))
                return;

            if (isDragging)
                stopDragging(startAnimation: false, releasePointer: true, bubbleDragEvents: true);

            _pointerDownOnChild = true;

            startDragThresholdTracking(evt, evt.target);
        }

        protected void onPointerMoveOnChild(PointerMoveEvent evt)
        {
            onPointerMove(evt);
        }

        protected void onPointerCancelOnChild(PointerCancelEvent evt)
        {
            if (_dragState == DragState.CheckingThreshold)
            {
                _dragState = DragState.Idle;
            }
            else if (_dragState == DragState.Dragging)
            {
                if (isCapturedDragPointerId(evt.pointerId) && !ignoreDragAndCancelEvent(evt, cancelEvent: false))
                    stopDragging(startAnimation: false, releasePointer: true, bubbleDragEvents: true);
            }

            _pointerDownOnChild = false;
        }

        protected void onPointerUpOnChild(PointerUpEvent evt)
        {
            if (_dragState == DragState.CheckingThreshold)
            {
                _dragState = DragState.Idle;
            }
            else if (_dragState == DragState.Dragging)
            {
                if (isCapturedDragPointerId(evt.pointerId) && !ignoreDragAndCancelEvent(evt, cancelEvent: false))
                    stopDragging(startAnimation: true, releasePointer: true, bubbleDragEvents: true);

                //if (PointerCaptureHelper.HasPointerCapture(evt.target, evt.pointerId))
                //{
                //    PointerCaptureHelper.ReleasePointer(evt.target, evt.pointerId);
                //
                //    // The pointer down event is not triggered in nested scroll views if the element had the pointer captured, thus we trigger it here manually.
                //    // TODO: Investigate if there is a better solution.
                //    var ve = evt.target as VisualElement;
                //    if (ve.parent != this)
                //    {
                //        // Sadly this causes double ClickEvents, thus we cancel double click events.
                //        var evtCopy = PointerUpEvent.GetPooled(evt);
                //        evt.StopImmediatePropagation();
                //        evt.target.SendEvent(evtCopy);
                //    }
                //}
            }

            _pointerDownOnChild = false;
        }

        protected int _lastClickEventFrame = -1;
        protected HashSet<IEventHandler> _frameClickEvents = new HashSet<IEventHandler>(5);

        protected void onPointerClickOnChild(ClickEvent evt)
        {
            // Tracks all clicked elements within a single frame and if a click event is triggered twice it will be cancelled.
            // This fixes some double event triggering issues.

            if (Time.frameCount != _lastClickEventFrame)
            {
                _frameClickEvents.Clear();
                _lastClickEventFrame = Time.frameCount;
            }

            if (_frameClickEvents.Contains(evt.currentTarget))
            {
                evt.StopImmediatePropagation();
            }
            _frameClickEvents.Add(evt.currentTarget);
        }

        /// <summary>
        /// Compute the new scroll view offset from a pointer delta, taking elasticity into account.
        /// Low and high limits are the values beyond which the scrollview starts to show resistance to scrolling (elasticity).
        /// Low and high hard limits are the values beyond which it is infinitely hard to scroll.
        /// </summary>
        /// <param name="currentOffset"></param>
        /// <param name="deltaPointer"></param>
        /// <param name="initialScrollOffset"></param>
        /// <param name="lowLimit"></param>
        /// <param name="hardLowLimit"></param>
        /// <param name="highLimit"></param>
        /// <param name="hardHighLimit"></param>
        /// <returns></returns>
        protected static float computeElasticOffset(
            float currentOffset,
            float deltaPointer, float initialScrollOffset,
            float lowLimit, float hardLowLimit,
            float highLimit, float hardHighLimit)
        {
            // Short circuit if inside all limits.
            float targetOffset = initialScrollOffset - deltaPointer;
            // Extra margins for equal state (with if the initial state of the scroll view).
            if (targetOffset > lowLimit - 0.001f && targetOffset < highLimit + 0.001f)
            {
                return targetOffset;
            }

            // Here it is between the limit and the hard limit.
            float limit = targetOffset < lowLimit ? lowLimit : highLimit;
            float hardLimit = targetOffset < lowLimit ? hardLowLimit : hardHighLimit;
            float span = hardLimit - limit;
            float delta = targetOffset - limit;
            float normalizedDelta = delta / span;
            // 0.3f = the content will stop at 30% of the scroll view size.
            float ratio = (1f - (normalizedDelta - 1) * (normalizedDelta - 1)) * 0.3f;
            if (normalizedDelta < 1f)
            {
                return limit + span * ratio;
            }
            else
            {
                return currentOffset;
            }
        }


        protected void startInertiaAndElasticityAnimation()
        {
            calcInitialSpringBackVelocity();

            // Reset if not moved for a while. Done to avoid inertia
            // animation in case the pointer was not moved for some time.
            if (Time.unscaledTime - _lastVelocityLerpTime > 0.2f)
                _velocity = Vector2.zero;

            if (_inertiaAndElasticityAnimation == null)
            {
                _inertiaAndElasticityAnimation = base.schedule.Execute(inertiaAndElasticityAnimationStep).Every(_animationFrameDurationInMS);
            }
            else
            {
                _inertiaAndElasticityAnimation.Resume();
            }
        }

        protected Vector2 _springBackVelocity;

        protected void calcInitialSpringBackVelocity()
        {
            if (touchScrollBehavior != ScrollView.TouchScrollBehavior.Elastic)
            {
                _springBackVelocity = Vector2.zero;
                return;
            }

            if (scrollOffset.x < _lowBounds.x)
            {
                _springBackVelocity.x = _lowBounds.x - scrollOffset.x;
            }
            else if (scrollOffset.x > _highBounds.x)
            {
                _springBackVelocity.x = _highBounds.x - scrollOffset.x;
            }
            else
            {
                _springBackVelocity.x = 0f;
            }

            if (scrollOffset.y < _lowBounds.y)
            {
                _springBackVelocity.y = _lowBounds.y - scrollOffset.y;
            }
            else if (scrollOffset.y > _highBounds.y)
            {
                _springBackVelocity.y = _highBounds.y - scrollOffset.y;
            }
            else
            {
                _springBackVelocity.y = 0f;
            }
        }

        protected void inertiaAndElasticityAnimationStep()
        {
            inertiaAnimationStep();
            elasticityAnimationStep();

            // If none of the animations needs updating then pause.
            if (_springBackVelocity == Vector2.zero && _velocity == Vector2.zero)
            {
                _inertiaAndElasticityAnimation.Pause();
            }
        }

        protected void elasticityAnimationStep()
        {
            if (touchScrollBehavior != ScrollView.TouchScrollBehavior.Elastic)
            {
                _springBackVelocity = Vector2.zero;
                return;
            }

            // Unity ScrollView uses Time.unscaledDeltaTime internally which makes the spring back
            // animation very slow on high fps (like in the Editor). To avoid that we use the delay
            // of the animation as delta time.
            float deltaTime = _animationFrameDurationInMS / 1000f;

            Vector2 vector = scrollOffset;
            if (vector.x < _lowBounds.x)
            {
                vector.x = Mathf.SmoothDamp(vector.x, _lowBounds.x, ref _springBackVelocity.x, elasticity, float.PositiveInfinity, deltaTime);
                if (Mathf.Abs(_springBackVelocity.x) < 1f)
                {
                    _springBackVelocity.x = 0f;
                }
            }
            else if (vector.x > _highBounds.x)
            {
                vector.x = Mathf.SmoothDamp(vector.x, _highBounds.x, ref _springBackVelocity.x, elasticity, float.PositiveInfinity, deltaTime);
                if (Mathf.Abs(_springBackVelocity.x) < 1f)
                {
                    _springBackVelocity.x = 0f;
                }
            }
            else
            {
                _springBackVelocity.x = 0f;
            }

            if (vector.y < _lowBounds.y)
            {
                vector.y = Mathf.SmoothDamp(vector.y, _lowBounds.y, ref _springBackVelocity.y, elasticity, float.PositiveInfinity, deltaTime);
                if (Mathf.Abs(_springBackVelocity.y) < 1f)
                {
                    _springBackVelocity.y = 0f;
                }
            }
            else if (vector.y > _highBounds.y)
            {
                vector.y = Mathf.SmoothDamp(vector.y, _highBounds.y, ref _springBackVelocity.y, elasticity, float.PositiveInfinity, deltaTime);
                if (Mathf.Abs(_springBackVelocity.y) < 1f)
                {
                    _springBackVelocity.y = 0f;
                }
            }
            else
            {
                _springBackVelocity.y = 0f;
            }

            scrollOffset = vector;
        }

        protected void inertiaAnimationStep()
        {
            // Unity ScrollView uses Time.unscaledDeltaTime internally which makes the spring back
            // animation very slow on high fps (like in the Editor). To avoid that we use the delay
            // of the animation as delta time.
            float deltaTime = _animationFrameDurationInMS / 1000f;

            if (hasInertia && _velocity != Vector2.zero)
            {
                _velocity *= Mathf.Pow(scrollDecelerationRate, deltaTime);

                // Set to 0 if close to zero or if out of bounds and behaviour is elastic.
                if (Mathf.Abs(_velocity.x) < 1f || (touchScrollBehavior == ScrollView.TouchScrollBehavior.Elastic && (scrollOffset.x < _lowBounds.x || scrollOffset.x > _highBounds.x)))
                {
                    _velocity.x = 0f;
                }

                if (Mathf.Abs(_velocity.y) < 1f || (touchScrollBehavior == ScrollView.TouchScrollBehavior.Elastic && (scrollOffset.y < _lowBounds.y || scrollOffset.y > _highBounds.y)))
                {
                    _velocity.y = 0f;
                }

                scrollOffset += _velocity * deltaTime;
            }
            else
            {
                _velocity = Vector2.zero;
            }

            handleSnappingWhileInteriaAnimation();
        }
    }
}
