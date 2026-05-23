using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MOneClickDownloads.App.Models
{
    /// <summary>
    /// 表示一个已记录的本地 mods 文件夹（UI 层模型）。
    /// 由 Service 层的 LocalModFolderEntry 映射而来，支持数据绑定。
    /// </summary>
    public partial class LocalModFolder : ObservableObject
    {
        /// <summary>
        /// 文件夹完整路径
        /// </summary>
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>
        /// 自定义显示名称（用户通过右键重命名设置）。
        /// 为空时回退到路径最后一级目录名。
        /// </summary>
        [ObservableProperty]
        private string? _customName;

        /// <summary>
        /// 显示名称：优先返回自定义名称，无则取路径最后一级目录名
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(CustomName)
            ? CustomName
            : (string.IsNullOrEmpty(FolderPath)
                ? string.Empty
                : System.IO.Path.GetFileName(FolderPath.TrimEnd(
                    System.IO.Path.DirectorySeparatorChar,
                    System.IO.Path.AltDirectorySeparatorChar)));

        /// <summary>
        /// 已持久化的 MC 版本筛选列表
        /// </summary>
        public List<string> AvailableMcVersions { get; set; } = new();

        /// <summary>
        /// 已持久化的加载器筛选列表
        /// </summary>
        public List<string> AvailableLoaders { get; set; } = new();

        /// <summary>
        /// 用于获取版本元数据的参考模组 ProjectId
        /// </summary>
        public string? MetadataProjectId { get; set; }

        /// <summary>
        /// 参考模组名称
        /// </summary>
        public string? MetadataModName { get; set; }

        /// <summary>
        /// 扫描后的模组数量（不持久化）
        /// </summary>
        public int ModCount { get; set; }

        /// <summary>
        /// 当 CustomName 变更时，通知 DisplayName 也更新
        /// </summary>
        partial void OnCustomNameChanged(string? value)
        {
            OnPropertyChanged(nameof(DisplayName));
        }
    }
}