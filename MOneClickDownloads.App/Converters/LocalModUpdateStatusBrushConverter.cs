using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MOneClickDownloads.App.Models;

namespace MOneClickDownloads.App.Converters
{
    /// <summary>
    /// 根据 LocalModUpdateStatus 枚举返回对应的 Badge 背景色。
    /// UpToDate → 绿色系，UpdateAvailable → 蓝色系，Incompatible → 红色系，NotFound → 灰色系，Error → 红色系
    /// </summary>
    public class LocalModUpdateStatusBrushConverter : IValueConverter
    {
        public static readonly LocalModUpdateStatusBrushConverter Instance = new();

        private static readonly SolidColorBrush UpToDateBrush = new(Color.Parse("#3322AA55"));
        private static readonly SolidColorBrush UpdateAvailableBrush = new(Color.Parse("#332277DD"));
        private static readonly SolidColorBrush IncompatibleBrush = new(Color.Parse("#33DD3333"));
        private static readonly SolidColorBrush NotFoundBrush = new(Color.Parse("#33888888"));
        private static readonly SolidColorBrush ErrorBrush = new(Color.Parse("#33DD3333"));
        private static readonly SolidColorBrush UnknownBrush = new(Color.Parse("#00000000"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is LocalModUpdateStatus status)
            {
                return status switch
                {
                    LocalModUpdateStatus.UpToDate => UpToDateBrush,
                    LocalModUpdateStatus.UpdateAvailable => UpdateAvailableBrush,
                    LocalModUpdateStatus.Incompatible => IncompatibleBrush,
                    LocalModUpdateStatus.NotFound => NotFoundBrush,
                    LocalModUpdateStatus.Error => ErrorBrush,
                    _ => UnknownBrush
                };
            }

            return UnknownBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}