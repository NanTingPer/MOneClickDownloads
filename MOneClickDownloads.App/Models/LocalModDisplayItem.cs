using MOneClickDownloads.DataModel.Enums;
using MOneClickDownloads.DataModel.Version;

namespace MOneClickDownloads.App.Models
{
    /// <summary>
    /// 本地模组的 UI 展示模型，包装 LocalModInventory 扫描结果提供显示用属性。
    /// 样式与 FavoriteDisplayItem 一致。
    /// </summary>
    public class LocalModDisplayItem
    {
        /// <summary>
        /// 模组 ID（从 JAR 元数据提取）
        /// </summary>
        public string ModId { get; set; } = string.Empty;

        /// <summary>
        /// 模组显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 模组简短描述
        /// </summary>
        public string DisplayDescription { get; set; } = string.Empty;

        /// <summary>
        /// 模组当前本地版本号
        /// </summary>
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>
        /// 项目图标URL，可能为null（需通过API补充）
        /// </summary>
        public string? IconUrl { get; set; }

        /// <summary>
        /// 项目ID（Modrinth ProjectId），通过 API 查询补充
        /// </summary>
        public string? ProjectId { get; set; }

        /// <summary>
        /// 项目 slug，用于冲突检测等
        /// </summary>
        public string? Slug { get; set; }

        /// <summary>
        /// 项目类型（mod / modpack 等）
        /// </summary>
        public ProjectType ProjectType { get; set; }

        /// <summary>
        /// 本地文件完整路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 项目总下载量
        /// </summary>
        public int Downloads { get; set; }

        /// <summary>
        /// 格式化的下载量文本
        /// </summary>
        public string FormattedDownloads => FavoriteDisplayItem.FormatDownloads(Downloads);

        /// <summary>
        /// 更新状态
        /// </summary>
        public LocalModUpdateStatus UpdateStatus { get; set; } = LocalModUpdateStatus.Unknown;

        /// <summary>
        /// 最新版本信息（API 查询后填充）
        /// </summary>
        public ModrinthVersion? LatestVersion { get; set; }

        /// <summary>
        /// 更新状态文本（如 "已是最新" / "v1.2 → v1.3" / "无兼容版本"）
        /// </summary>
        public string UpdateStatusText { get; set; } = string.Empty;
    }

    /// <summary>
    /// 本地模组的更新状态枚举
    /// </summary>
    public enum LocalModUpdateStatus
    {
        /// <summary>
        /// 未知（未检查）
        /// </summary>
        Unknown,

        /// <summary>
        /// 已是最新版
        /// </summary>
        UpToDate,

        /// <summary>
        /// 有可用更新
        /// </summary>
        UpdateAvailable,

        /// <summary>
        /// 无兼容版本
        /// </summary>
        Incompatible,

        /// <summary>
        /// API 中未找到此模组
        /// </summary>
        NotFound,

        /// <summary>
        /// 检查时出错
        /// </summary>
        Error
    }
}