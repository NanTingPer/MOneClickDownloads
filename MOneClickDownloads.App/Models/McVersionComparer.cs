using System;
using System.Collections.Generic;

namespace MOneClickDownloads.App.Models
{
    /// <summary>
    /// MC版本号比较器，按版本号降序排列。
    /// 支持 "1.21.4"、"1.20"、"24w10a" 等格式。
    /// </summary>
    public class McVersionComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return 1;
            if (y == null) return -1;

            var xParts = x.Split('.');
            var yParts = y.Split('.');

            // 尝试按数字比较
            var maxLength = Math.Max(xParts.Length, yParts.Length);
            for (int i = 0; i < maxLength; i++)
            {
                var xPart = i < xParts.Length ? xParts[i] : "0";
                var yPart = i < yParts.Length ? yParts[i] : "0";

                if (int.TryParse(xPart, out var xNum) && int.TryParse(yPart, out var yNum))
                {
                    var cmp = xNum.CompareTo(yNum);
                    if (cmp != 0) return cmp;
                }
                else
                {
                    // 非数字部分按字符串比较
                    var cmp = string.Compare(xPart, yPart, StringComparison.OrdinalIgnoreCase);
                    if (cmp != 0) return cmp;
                }
            }

            return 0;
        }
    }
}