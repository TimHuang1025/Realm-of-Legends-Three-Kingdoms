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
        /// <param name="decimals">保留小数位，默认 1 位。</param>
            public static string Format(long value, int decimals = 1)
            {
                long abs = Math.Abs(value);

                double shortNum;
                string suffix;

                if      (abs < 10_000)                  { shortNum = abs;                suffix = "";  decimals = 0; }
                else if (abs < 1_000_000)               { shortNum = abs / 1_000d;       suffix = "K"; }
                else if (abs < 1_000_000_000)           { shortNum = abs / 1_000_000d;   suffix = "M"; }
                else if (abs < 1_000_000_000_000)       { shortNum = abs / 1_000_000_000d; suffix = "B"; }
                else                                    { shortNum = abs / 1_000_000_000_000d; suffix = "T"; }

                string numberPart = shortNum.ToString($"N{decimals}")
                                            .TrimEnd('0').TrimEnd(',', '.');

                return (value < 0 ? "-" : "") + numberPart + suffix;
            }

    }
}
