using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MOneClickDownloads.DataModel.Enums;

namespace MOneClickDownloads.App.Converters
{
    /// <summary>
    /// 根据 ProjectType 枚举返回对应的 Badge 背景色。
    /// mod → 绿色系，modpack → 蓝色系，resourcepack → 紫色系，shader → 橙色系
    /// </summary>
    public class ProjectTypeBrushConverter : IValueConverter
    {
        public static readonly ProjectTypeBrushConverter Instance = new();

        private static readonly SolidColorBrush ModBrush = new(Color.Parse("#3322AA55"));
        private static readonly SolidColorBrush ModpackBrush = new(Color.Parse("#332277FF"));
        private static readonly SolidColorBrush ResourcePackBrush = new(Color.Parse("#33AA55DD"));
        private static readonly SolidColorBrush ShaderBrush = new(Color.Parse("#33DD8800"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ProjectType type)
            {
                return type switch
                {
                    ProjectType.Mod => ModBrush,
                    ProjectType.Modpack => ModpackBrush,
                    ProjectType.ResourcePack => ResourcePackBrush,
                    ProjectType.Shader => ShaderBrush,
                    _ => ModBrush
                };
            }

            // 支持从字符串标签转换
            if (value is string tag)
            {
                return tag switch
                {
                    "mod" => ModBrush,
                    "modpack" => ModpackBrush,
                    "resourcepack" => ResourcePackBrush,
                    "shader" => ShaderBrush,
                    _ => ModBrush
                };
            }

            return ModBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}