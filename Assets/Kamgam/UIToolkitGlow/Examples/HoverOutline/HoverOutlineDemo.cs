using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitGlow
{
    public class HoverOutlineDemo : MonoBehaviour
    {
        [Header("Toggle Glow")]

        [Tooltip("The class that will be toggled with hover.")]
        public string HoverGlowClass = "g-hover-outline";

        protected UIDocument _document;
        public UIDocument Document
        {
            get
            {
                if (_document == null)
                {
                    _document = this.GetComponent<UIDocument>();
                }
                return _document;
            }
        }

        public void OnEnable()
        {
            // Register glow on hover
            var hoverElementsQuery = Document.rootVisualElement.Query<Button>();
            GlowUtils.RegisterToggleClassOnHover(hoverElementsQuery, HoverGlowClass);
        }
    }

}