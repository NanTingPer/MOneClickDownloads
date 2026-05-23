using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Enums;

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

        /// <summary>
        /// 持久化的模组元数据列表。
        /// Key 为 modId（从 JAR 元数据提取），用于扫描时的匹配。
        /// </summary>
        [JsonPropertyName("mod_entries")]
        public List<LocalModEntry> ModEntries { get; set; } = new();
    }

    /// <summary>
    /// 持久化的单个本地模组元数据条目。
    /// 字段设计参考 <see cref="MOneClickDownloads.DataModel.Favorites.FavoriteItem"/>，
    /// 但以 ModId（JAR 元数据中的 modid）作为主键，而非 ProjectId。
    /// </summary>
    public class LocalModEntry
    {
        /// <summary>
        /// 模组 ID（从 JAR 元数据提取的 modid），用作持久化匹配的主键
        /// </summary>
        [JsonPropertyName("mod_id")]
        public string ModId { get; set; } = string.Empty;

        /// <summary>
        /// JAR 文件名，辅助匹配
        /// </summary>
        [JsonPropertyName("file_name")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 模组显示名称（从 JAR 元数据提取或 API 查询补充）
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Modrinth 项目 ID
        /// </summary>
        [JsonPropertyName("project_id")]
        public string? ProjectId { get; set; }

        /// <summary>
        /// 模组简短描述（从 API 获取，与 FavoriteItem.Description 对应）
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 项目 slug
        /// </summary>
        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        /// <summary>
        /// 项目图标原始 URL
        /// </summary>
        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        /// <summary>
        /// 项目类型
        /// </summary>
        [JsonPropertyName("project_type")]
        public ProjectType ProjectType { get; set; }

        /// <summary>
        /// 项目总下载量
        /// </summary>
        [JsonPropertyName("downloads")]
        public int Downloads { get; set; }

        /// <summary>
        /// 上次扫描时的本地版本号
        /// </summary>
        [JsonPropertyName("local_version")]
        public string? LocalVersion { get; set; }

        /// <summary>
        /// 上次从 API 更新元数据的时间
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}