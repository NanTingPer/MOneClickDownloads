using System;
using MOneClickDownloads.DataModel.Enums;
using MOneClickDownloads.DataModel.Favorites;

namespace MOneClickDownloads.App.Models
{
    /// <summary>
    /// 用于UI展示的收藏模组条目，包装 FavoriteItem 提供显示用属性。
    /// 对应 ModVersionItem 在版本列表中的角色。
    /// </summary>
    public class FavoriteDisplayItem
    {
        /// <summary>
        /// 原始收藏条目
        /// </summary>
        public FavoriteItem Item { get; set; } = null!;

        /// <summary>
        /// 模组显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 模组简短描述
        /// </summary>
        public string DisplayDescription { get; set; } = string.Empty;

        /// <summary>
        /// 项目类型标签文本（mod / modpack / resourcepack / shader）
        /// </summary>
        public string DisplayTypeTag { get; set; } = string.Empty;

        /// <summary>
        /// 项目类型枚举，用于颜色转换
        /// </summary>
        public ProjectType ProjectType { get; set; }

        /// <summary>
        /// 格式化的下载量文本（如 "5.2M" / "123.4K"）
        /// </summary>
        public string FormattedDownloads { get; set; } = string.Empty;

        /// <summary>
        /// 格式化的收藏日期文本
        /// </summary>
        public string FormattedDate { get; set; } = string.Empty;

        /// <summary>
        /// 根据 ProjectType 枚举获取标签文本
        /// </summary>
        public static string GetTypeTag(ProjectType projectType)
        {
            return projectType switch
            {
                ProjectType.Mod => "mod",
                ProjectType.Modpack => "modpack",
                ProjectType.ResourcePack => "resourcepack",
                ProjectType.Shader => "shader",
                _ => projectType.ToString().ToLowerInvariant()
            };
        }

        /// <summary>
        /// 将下载量格式化为易读文本
        /// </summary>
        public static string FormatDownloads(int downloads)
        {
            return downloads switch
            {
                >= 1_000_000 => $"{downloads / 1_000_000.0:F1}M",
                >= 1_000 => $"{downloads / 1_000.0:F1}K",
                _ => downloads.ToString("N0")
            };
        }
    }
}