using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScrollViewPro
{
    public class DragMoveEvent : EventBase<DragMoveEvent>
    {
        public ScrollViewPro scrollView;
        public PointerMoveEvent pointerMoveEvent;

        public static void Dispatch(VisualElement target, ScrollViewPro scrollView, PointerMoveEvent pointerMoveEvent)
        {
            using (var evt = DragMoveEvent.GetPooled())
            {
                evt.scrollView = scrollView;
                evt.pointerMoveEvent = pointerMoveEvent;
                evt.target = target;
                evt.bubbles = true;

                target.SendEvent(evt);
            }
        }
    }
}