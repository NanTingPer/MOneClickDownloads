using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Enums;

namespace MOneClickDownloads.DataModel.Version
{
    /// <summary>
    /// 模组版本详情模型，表示 Modrinth 上一个模组项目的某个具体版本。
    /// 
    /// 用途：
    /// - 作为 Get a version API（GET /version/{id}）的响应数据模型
    /// - 作为 List project's versions API（GET /project/{id|slug}/version）的响应数组元素
    /// - 获取版本的文件下载链接（files[0].url）
    /// - 获取版本的依赖列表（dependencies），用于递归下载
    /// - 获取版本兼容的MC版本和加载器信息
    /// 
    /// 数据流转（核心下载流程）：
    /// 1. 通过 Search/GetProject 获取 project_id
    /// 2. 调用 List project's versions 并传入 game_versions 和 loaders 参数筛选
    /// 3. 从返回的版本列表中选取目标版本
    /// 4. 从 files[] 中获取下载链接并下载
    /// 5. 从 dependencies[] 中获取 required 依赖的 project_id/version_id，递归执行步骤2-5
    /// </summary>
    public class ModrinthVersion : IApiModel
    {
        /// <summary>
        /// 版本名称（如 "Version 1.0.0"）
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 版本号，理想情况下遵循语义化版本规范（如 "1.0.0"、"0.141.4+1.21.11"）
        /// </summary>
        [JsonPropertyName("version_number")]
        public string VersionNumber { get; set; } = string.Empty;

        /// <summary>
        /// 此版本的更新日志（Markdown格式），可能为null
        /// </summary>
        [JsonPropertyName("changelog")]
        public string? Changelog { get; set; }

        /// <summary>
        /// 此版本依赖的其他模组版本列表。
        /// 
        /// 关键用途：递归下载依赖。
        /// 遍历此列表，筛选 dependency_type == Required 的依赖，
        /// 若 version_id 有值则直接获取该版本的下载链接；
        /// 若 version_id 为 null，则通过 project_id 查询兼容当前MC版本的最新版本。
        /// </summary>
        [JsonPropertyName("dependencies")]
        public List<Dependency> Dependencies { get; set; } = new List<Dependency>();

        /// <summary>
        /// 此版本支持的 Minecraft 游戏版本列表（如 ["1.21.11", "1.21.10"]）。
        /// 
        /// 关键用途：在 List project's versions API 中作为查询参数过滤兼容版本，
        /// 以及在构造带参下载URL时使用。
        /// </summary>
        [JsonPropertyName("game_versions")]
        public List<string> GameVersions { get; set; } = new List<string>();

        /// <summary>
        /// 此版本支持的模组加载器列表（如 ["fabric"]、["forge"]、["fabric", "quilt"]）。
        /// 对于资源包，此值为 ["minecraft"]。
        /// 
        /// 关键用途：在 List project's versions API 中作为查询参数过滤兼容版本，
        /// 以及在构造带参下载URL时使用。
        /// </summary>
        [JsonPropertyName("loaders")]
        public List<string> Loaders { get; set; } = new List<string>();

        /// <summary>
        /// 此版本的发布频道类型（release / beta / alpha）
        /// </summary>
        [JsonPropertyName("version_type")]
        public VersionType VersionType { get; set; }

        /// <summary>
        /// 此版本是否为推荐版本（在项目页面突出显示）
        /// </summary>
        [JsonPropertyName("featured")]
        public bool Featured { get; set; }

        /// <summary>
        /// 版本的状态（listed / archived / draft / unlisted / scheduled）
        /// </summary>
        [JsonPropertyName("status")]
        public VersionStatus Status { get; set; }

        /// <summary>
        /// 版本计划的目标状态，可能为null
        /// </summary>
        [JsonPropertyName("requested_status")]
        public VersionStatus? RequestedStatus { get; set; }

        /// <summary>
        /// 版本的唯一ID，base62编码字符串（如 "5zJNhXV2"）。
        /// 
        /// 关键用途：
        /// - 作为 Get a version API 的路径参数
        /// - 在构造CDN下载URL时使用：https://cdn.modrinth.com/data/{project_id}/versions/{id}/...
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 此版本所属项目的ID，base62编码字符串（如 "P7dR8mSH"）。
        /// 
        /// 关键用途：在构造CDN下载URL时使用。
        /// </summary>
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// 发布此版本的作者ID
        /// </summary>
        [JsonPropertyName("author_id")]
        public string AuthorId { get; set; } = string.Empty;

        /// <summary>
        /// 版本发布时间（ISO-8601格式）
        /// </summary>
        [JsonPropertyName("date_published")]
        public DateTime DatePublished { get; set; }

        /// <summary>
        /// 此版本的总下载次数
        /// </summary>
        [JsonPropertyName("downloads")]
        public int Downloads { get; set; }

        /// <summary>
        /// 版本更新日志的外部链接（始终为null，仅保留用于向后兼容）
        /// </summary>
        [JsonPropertyName("changelog_url")]
        public string? ChangelogUrl { get; set; }

        /// <summary>
        /// 此版本的可下载文件列表。
        /// 
        /// 关键用途：获取实际下载链接。
        /// 通常 files[0] 即为主要下载文件，或查找 primary == true 的文件。
        /// 其 Url 字段直接用于HTTP下载。
        /// </summary>
        [JsonPropertyName("files")]
        public List<VersionFile> Files { get; set; } = new List<VersionFile>();

        /// <summary>
        /// 获取此模型对应的 API 端点路径。
        /// 使用时将 {id} 替换为实际的版本ID。
        /// </summary>
        public string GetEndpoint() => "/version/{id}";
    }
}