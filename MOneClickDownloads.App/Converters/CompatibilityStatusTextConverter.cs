using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MOneClickDownloads.App.Models;

namespace MOneClickDownloads.App.Converters
{
    /// <summary>
    /// 根据 ModCompatibilityStatus 枚举返回对应的标签文本。
    /// Compatible → "✓ 兼容"，PreviewOnly → "⚠ 预览"，Incompatible → "✗ 不兼容"
    /// </summary>
    public class CompatibilityStatusTextConverter : IValueConverter
    {
        public static readonly CompatibilityStatusTextConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ModCompatibilityStatus status)
            {
                return status switch
                {
                    ModCompatibilityStatus.Compatible => "✓ 兼容",
                    ModCompatibilityStatus.PreviewOnly => "⚠ 预览",
                    ModCompatibilityStatus.Incompatible => "✗ 不兼容",
                    _ => "未知"
                };
            }

            return "未知";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}