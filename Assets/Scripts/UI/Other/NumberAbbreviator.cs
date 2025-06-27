// ────────────────────────────────────────────────────────────────────────────────
// Assets/Scripts/Utils/NumberAbbreviator.cs
// 把大数字格式化成 1.2K / 3.4M / 5.6B / 7.8T …
// ────────────────────────────────────────────────────────────────────────────────
using System;
using UnityEngine;

namespace Game.Utils
{
    public static class NumberAbbreviator
    {
        /// <summary>
        /// 将整数缩写成易读形式。
        /// </summary>
        /// <param name="value">原始值（支持负数与 long）。</param>
        /// <param name="decimals">保留小数位（默认 1 位；千以内自动转成 0 位）。</param>
        public static string Format(long value, int decimals = 1)
        {
            // ① 直接处理 0，避免被 TrimEnd 截成空串
            if (value == 0) return "0";

            long abs = Math.Abs(value);

            double shortNum;
            string suffix;

            if      (abs < 10_000)            { shortNum = abs;                   suffix = "";  decimals = 0; }
            else if (abs < 1_000_000)         { shortNum = abs / 1_000d;          suffix = "K"; }
            else if (abs < 1_000_000_000)     { shortNum = abs / 1_000_000d;      suffix = "M"; }
            else if (abs < 1_000_000_000_000) { shortNum = abs / 1_000_000_000d;  suffix = "B"; }
            else                              { shortNum = abs / 1_000_000_000_000d; suffix = "T"; }

            // 注意：N 格式受当前区域影响（千分位分隔符、逗号或空格等）；
            // 如需强制统一，可改成 InvariantCulture。
            string numberPart = shortNum
                                .ToString($"N{decimals}", System.Globalization.CultureInfo.InvariantCulture)
                                .TrimEnd('0')        // 去掉小数末尾 0
                                .TrimEnd('.');       // 去掉小数点

            return (value < 0 ? "-" : "") + numberPart + suffix;
        }
    }
}
