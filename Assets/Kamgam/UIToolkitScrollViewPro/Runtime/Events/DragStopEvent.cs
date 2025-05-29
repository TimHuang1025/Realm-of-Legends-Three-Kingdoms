using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScrollViewPro
{
    public class DragStopEvent : EventBase<DragStopEvent>
    {
        public ScrollViewPro scrollView;

        public static void Dispatch(VisualElement target, ScrollViewPro scrollView)
        {
            using (var evt = DragStopEvent.GetPooled())
            {
                evt.scrollView = scrollView;
                evt.target = target;
                evt.bubbles = true;

                target.SendEvent(evt);
            }
        }
    }
}