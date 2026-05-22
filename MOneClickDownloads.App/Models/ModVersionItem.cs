using MOneClickDownloads.DataModel.Enums;
using MOneClickDownloads.DataModel.Version;

namespace MOneClickDownloads.App.Models
{
    /// <summary>
    /// 用于UI展示的模组版本条目，包装 ModrinthVersion 提供显示用属性。
    /// </summary>
    public class ModVersionItem
    {
        /// <summary>
        /// 原始 Modrinth 版本对象
        /// </summary>
        public ModrinthVersion Version { get; set; } = null!;

        /// <summary>
        /// 显示名称（版本名 + 版本号）
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 版本类型文本（Release / Beta / Alpha）
        /// </summary>
        public string VersionTypeText { get; set; } = string.Empty;

        /// <summary>
        /// 发布状态标签文本：[发布] 或 [预览]
        /// </summary>
        public string DisplayTypeTag { get; set; } = string.Empty;

        /// <summary>
        /// 支持的加载器文本（如 "fabric, quilt"）
        /// </summary>
        public string LoadersText { get; set; } = string.Empty;

        /// <summary>
        /// 支持的游戏版本文本（如 "1.20.1, 1.20.2"）
        /// </summary>
        public string GameVersionsText { get; set; } = string.Empty;

        /// <summary>
        /// 发布日期文本
        /// </summary>
        public string DatePublishedText { get; set; } = string.Empty;

        /// <summary>
        /// 根据版本类型计算发布状态标签
        /// </summary>
        public static string GetTypeTag(VersionType versionType)
        {
            return versionType == VersionType.Release ? "[发布]" : "[预览]";
        }
    }
}
