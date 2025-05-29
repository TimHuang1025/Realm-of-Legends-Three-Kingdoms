using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScrollViewPro
{
    public class DragStartEvent : EventBase<DragStartEvent>
    {
        public ScrollViewPro scrollView;
        public Vector3 pointerPosition;
        public int pointerId;

        public static void Dispatch(VisualElement target, ScrollViewPro scrollView, Vector3 pointerPosition, int pointerId)
        {
            using (var evt = DragStartEvent.GetPooled())
            {
                evt.scrollView = scrollView;
                evt.pointerPosition = pointerPosition;
                evt.pointerId = pointerId;
                evt.target = target;
                evt.bubbles = true;

                target.SendEvent(evt);
            }
        }
    }
}