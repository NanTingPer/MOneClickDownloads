using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MOneClickDownloads.App.Converters
{
    /// <summary>
    /// 根据发布状态标签文本（[发布]/[预览]）返回对应的背景色。
    /// [发布] → 绿色系，[预览] → 橙色系
    /// </summary>
    public class TypeTagBrushConverter : IValueConverter
    {
        public static readonly TypeTagBrushConverter Instance = new();

        private static readonly SolidColorBrush ReleaseBrush = new(Color.Parse("#3322AA55"));
        private static readonly SolidColorBrush PreviewBrush = new(Color.Parse("#33DD8800"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string tag)
            {
                return tag == "[发布]" ? ReleaseBrush : PreviewBrush;
            }
            return PreviewBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}