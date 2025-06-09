using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIExt
{
    public static class BounceExtension
    {
        /*─────────────────────────────────────────
         * ① 自己收事件，自己缩放
         *────────────────────────────────────────*/
        public static VisualElement Bounce(this VisualElement ve,
                                           float pressScale = .9f,
                                           float duration   = .1f)
        {
            if (ve == null) return null;
            if (ve.userData is _BounceData) return ve;   // 已挂载

            var data = new _BounceData(pressScale, duration);
            ve.userData = data;

            ve.RegisterCallback<PointerDownEvent>(_ => data.Run(ve, pressScale), TrickleDown.TrickleDown);

            void Back(EventBase _) => data.Run(ve, 1f);
            ve.RegisterCallback<PointerUpEvent>(Back,    TrickleDown.TrickleDown);
            ve.RegisterCallback<PointerLeaveEvent>(Back, TrickleDown.TrickleDown);
            ve.RegisterCallback<PointerCancelEvent>(Back,TrickleDown.TrickleDown);

            return ve;
        }

        /*─────────────────────────────────────────
         * ② source 收事件，target 缩放
         *────────────────────────────────────────*/
        public static void Bounce(this VisualElement source,
                                  VisualElement     target,
                                  float pressScale = .9f,
                                  float duration   = .1f)
        {
            if (source == null || target == null) return;

            if (ReferenceEquals(source, target))
            {
                source.Bounce(pressScale, duration);
                return;
            }

            if (source.userData is string f && f == "__BounceLinked") return;
            source.userData = "__BounceLinked";

            source.RegisterCallback<PointerDownEvent>(
                _ => _Bounce(target, pressScale, duration), TrickleDown.TrickleDown);

            void Back(EventBase _) => _Bounce(target, 1f, duration);
            source.RegisterCallback<PointerUpEvent>(Back,    TrickleDown.TrickleDown);
            source.RegisterCallback<PointerLeaveEvent>(Back, TrickleDown.TrickleDown);
            source.RegisterCallback<PointerCancelEvent>(Back,TrickleDown.TrickleDown);
        }

        /*─────────────────────────────────────────
         * ③ 链式 OnClick
         *────────────────────────────────────────*/
        public static VisualElement OnClick(this VisualElement ve, Action act)
        {
            ve.RegisterCallback<ClickEvent>(_ => act?.Invoke());
            return ve;
        }

        /*─────────────────────────────────────────
         * ④ 内部 Tween：统一用 style.scale
         *────────────────────────────────────────*/
        static void _Bounce(VisualElement ve, float target, float dur)
        {
            if (ve == null) return;

            var runner = _Runner.Inst;
            if (ve.userData is Coroutine oldCo) runner.StopCoroutine(oldCo);

            ve.userData = runner.StartCoroutine(Tween());

            IEnumerator Tween()
            {
                const float start = 1f;                       // 每次动画从 1 开始
                float elapsed = 0f;
                while (elapsed < dur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float k = 1f - (1f - elapsed / dur) * (1f - elapsed / dur); // OutQuad
                    float s = Mathf.Lerp(start, target, k);
                    ve.style.scale = new Scale(new Vector3(s, s, 1));
                    yield return null;
                }
                ve.style.scale = new Scale(new Vector3(target, target, 1));
            }
        }

        /*─────────────────────────────────────────
         * ⑤ _BounceData：给“自己缩放”用
         *────────────────────────────────────────*/
        class _BounceData
        {
            readonly float press, time;
            Coroutine co;

            public _BounceData(float p, float t) { press = p; time = t; }

            public void Run(VisualElement ve, float target)
            {
                var runner = _Runner.Inst;
                if (co != null) runner.StopCoroutine(co);
                co = runner.StartCoroutine(Tween());

                IEnumerator Tween()
                {
                    const float start = 1f;
                    float elapsed = 0f;
                    while (elapsed < time)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float k = 1f - (1f - elapsed / time) * (1f - elapsed / time);
                        float s = Mathf.Lerp(start, target, k);
                        ve.style.scale = new Scale(new Vector3(s, s, 1));
                        yield return null;
                    }
                    ve.style.scale = new Scale(new Vector3(target, target, 1));
                }
            }
        }

        /*─────────────────────────────────────────
         * ⑥ 协程托管器（全局单例）
         *────────────────────────────────────────*/
        class _Runner : MonoBehaviour
        {
            static _Runner _inst;
            public static _Runner Inst
            {
                get
                {
                    if (_inst == null)
                    {
                        var go = new GameObject("[BounceRunner]");
                        DontDestroyOnLoad(go);
                        _inst = go.AddComponent<_Runner>();
                    }
                    return _inst;
                }
            }
        }
    }
}
