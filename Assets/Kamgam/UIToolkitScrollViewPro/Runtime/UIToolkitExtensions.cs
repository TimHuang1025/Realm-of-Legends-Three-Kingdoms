using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScrollViewPro
{
    public static class UIToolkitExtensions
    {
        public static bool IsChildOf(this VisualElement element, VisualElement parent)
        {
            return parent.Contains(element);
        }

        public static bool IsChildOfClass(this VisualElement element, string className, bool includeSelf = true)
        {
            if (element == null)
                return false;

            if (includeSelf && element.ClassListContains(className))
            {
                return true;
            }

            var parent = element.parent;
            while (parent != null)
            {
                if (parent.ClassListContains(className))
                    return true;

                parent = parent.parent;
            }

            return false;
        }

        /// <summary>
        /// Returns true only if the element is a child (or self) of classNamePositive with classNameNegative negating the positive class.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="classNamePositive"></param>
        /// <param name="classNameNegative"></param>
        /// <param name="includeSelf"></param>
        /// <param name="preferNegative"></param>
        /// <returns></returns>
        public static bool IsChildOfClass(this VisualElement element, string classNamePositive, string classNameNegative, bool includeSelf = true, bool preferNegative = true)
        {
            if (element == null)
                return false;

            if (includeSelf)
            {
                bool containsNegative = element.ClassListContains(classNameNegative);
                if (containsNegative && preferNegative)
                    return false;

                if (element.ClassListContains(classNamePositive))
                    return true;

                if (containsNegative)
                    return false;
            }

            var parent = element.parent;
            while (parent != null)
            {
                // Stop at the first found positive or negative class.
                bool containsNegative = parent.ClassListContains(classNameNegative);
                if (containsNegative && preferNegative)
                    return false;

                if (parent.ClassListContains(classNamePositive))
                    return true;

                if (containsNegative)
                    return false;

                parent = parent.parent;
            }

            return false;
        }
    }
}
