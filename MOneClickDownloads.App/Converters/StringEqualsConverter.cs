using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MOneClickDownloads.App.Converters
{
    /// <summary>
    /// 比较两个字符串是否相等，返回对应的背景色。
    /// 用于加载器过滤标签的选中/未选中样式切换。
    /// </summary>
    public class StringEqualsConverter : IMultiValueConverter
    {
        public static readonly StringEqualsConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2) return new SolidColorBrush(Color.Parse("#33FFFFFF"));

            var value = values[0] as string;
            var comparer = values[1] as string;

            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(comparer))
                return new SolidColorBrush(Color.Parse("#33FFFFFF"));

            return string.Equals(value, comparer, StringComparison.OrdinalIgnoreCase)
                ? new SolidColorBrush(Color.Parse("#5588CCFF"))
                : new SolidColorBrush(Color.Parse("#33FFFFFF"));
        }
    }
}