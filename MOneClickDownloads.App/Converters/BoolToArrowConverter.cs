using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MOneClickDownloads.App.Converters
{
    /// <summary>
    /// 将布尔值转换为箭头符号。
    /// true → "▼"（展开状态），false → "▲"（折叠状态）。
    /// </summary>
    public class BoolToArrowConverter : IValueConverter
    {
        public static readonly BoolToArrowConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isChecked)
            {
                return isChecked ? "▼" : "▲";
            }
            return "▲";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}