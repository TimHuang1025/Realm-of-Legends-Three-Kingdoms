using System;
using System.Globalization;

namespace Game.Utils
{
    public static class NumberAbbreviator
    {
        public static string Format(long value, int decimals = 1)
        {
            if (value == 0) return "0";

            long    abs     = Math.Abs(value);
            double  shortNum;
            string  suffix;

            if      (abs < 10_000)            { shortNum = abs;                     suffix = "";  decimals = 0; }
            else if (abs < 1_000_000)         { shortNum = abs / 1_000d;            suffix = "K"; }
            else if (abs < 1_000_000_000)     { shortNum = abs / 1_000_000d;        suffix = "M"; }
            else if (abs < 1_000_000_000_000) { shortNum = abs / 1_000_000_000d;    suffix = "B"; }
            else                              { shortNum = abs / 1_000_000_000_000d; suffix = "T"; }

            string numberPart;   // ★ 修正区开始
            if (decimals > 0)
            {
                numberPart = shortNum
                    .ToString($"F{decimals}", CultureInfo.InvariantCulture)
                    .TrimEnd('0')
                    .TrimEnd('.');
            }
            else
            {
                numberPart = shortNum.ToString("F0", CultureInfo.InvariantCulture);
            }                    // ★ 修正区结束

            return (value < 0 ? "-" : "") + numberPart + suffix;
        }
    }
}
