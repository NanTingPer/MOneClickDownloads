using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MOneClickDownloads.Service.Models
{
    /// <summary>
    /// 本地模组文件夹的持久化条目。
    /// Service 层的数据传输对象，不依赖 UI 框架。
    /// </summary>
    public class LocalModFolderEntry
    {
        /// <summary>
        /// 文件夹完整路径
        /// </summary>
        [JsonPropertyName("folder_path")]
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>
        /// 自定义显示名称，为空时回退到路径最后一级目录名
        /// </summary>
        [JsonPropertyName("custom_name")]
        public string? CustomName { get; set; }

        /// <summary>
        /// 已持久化的 MC 版本筛选列表
        /// </summary>
        [JsonPropertyName("available_mc_versions")]
        public List<string> AvailableMcVersions { get; set; } = new();

        /// <summary>
        /// 已持久化的加载器筛选列表
        /// </summary>
        [JsonPropertyName("available_loaders")]
        public List<string> AvailableLoaders { get; set; } = new();

        /// <summary>
        /// 用于获取版本元数据的参考模组 ProjectId
        /// </summary>
        [JsonPropertyName("metadata_project_id")]
        public string? MetadataProjectId { get; set; }

        /// <summary>
        /// 参考模组名称
        /// </summary>
        [JsonPropertyName("metadata_mod_name")]
        public string? MetadataModName { get; set; }
    }
}