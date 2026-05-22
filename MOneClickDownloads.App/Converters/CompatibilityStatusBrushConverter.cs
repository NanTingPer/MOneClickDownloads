using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MOneClickDownloads.App.Models;

namespace MOneClickDownloads.App.Converters
{
    /// <summary>
    /// 根据 ModCompatibilityStatus 枚举返回对应的 Badge 背景色。
    /// Compatible → 绿色系，PreviewOnly → 橙色系，Incompatible → 红色系
    /// </summary>
    public class CompatibilityStatusBrushConverter : IValueConverter
    {
        public static readonly CompatibilityStatusBrushConverter Instance = new();

        private static readonly SolidColorBrush CompatibleBrush = new(Color.Parse("#3322AA55"));
        private static readonly SolidColorBrush PreviewOnlyBrush = new(Color.Parse("#33DD8800"));
        private static readonly SolidColorBrush IncompatibleBrush = new(Color.Parse("#33DD3333"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ModCompatibilityStatus status)
            {
                return status switch
                {
                    ModCompatibilityStatus.Compatible => CompatibleBrush,
                    ModCompatibilityStatus.PreviewOnly => PreviewOnlyBrush,
                    ModCompatibilityStatus.Incompatible => IncompatibleBrush,
                    _ => CompatibleBrush
                };
            }

            return CompatibleBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}