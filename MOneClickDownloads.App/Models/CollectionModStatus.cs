using MOneClickDownloads.DataModel.Version;

namespace MOneClickDownloads.App.Models
{
    /// <summary>
    /// 合集中单个模组的兼容性状态，用于在合集下载页展示每个模组的版本兼容情况。
    /// </summary>
    public class CollectionModStatus
    {
        /// <summary>
        /// 模组名称
        /// </summary>
        public string ModName { get; set; } = string.Empty;

        /// <summary>
        /// 模组项目ID
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// 模组 slug，用于下载前冲突预检
        /// </summary>
        public string? Slug { get; set; }

        /// <summary>
        /// 兼容性状态
        /// </summary>
        public ModCompatibilityStatus Status { get; set; }

        /// <summary>
        /// 筛选后的最佳版本（优先 Release，否则最佳可用版本）
        /// </summary>
        public ModrinthVersion? BestVersion { get; set; }

        /// <summary>
        /// 状态描述文本（用于 UI 显示）
        /// </summary>
        public string StatusText { get; set; } = string.Empty;
    }

    /// <summary>
    /// 模组兼容性状态枚举
    /// </summary>
    public enum ModCompatibilityStatus
    {
        /// <summary>
        /// 有兼容的 Release 版本
        /// </summary>
        Compatible,

        /// <summary>
        /// 仅有 Beta/Alpha 版本
        /// </summary>
        PreviewOnly,

        /// <summary>
        /// 无任何兼容版本
        /// </summary>
        Incompatible
    }
}