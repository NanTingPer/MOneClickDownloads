using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Enums;

namespace MOneClickDownloads.DataModel.Search
{
    /// <summary>
    /// 搜索命中项目模型，表示 Search projects API 返回的单个搜索结果项。
    /// 这是 Project 模型的精简版，仅包含搜索结果展示所需的字段。
    /// 
    /// 用途：
    /// - 作为 SearchResponse.Hits 数组中的元素
    /// - 展示搜索结果列表（标题、描述、下载量、图标等）
    /// - 获取 project_id 后可调用 Get a project API 获取完整信息
    /// 
    /// 数据流转：
    /// - 从 Search projects API（GET /search）响应的 hits[] 数组反序列化
    /// - 其 ProjectId 属性用于后续调用 Get a project / List project's versions API
    /// </summary>
    public class ProjectHit
    {
        /// <summary>
        /// 项目的URL友好标识符（如 "fabric-api"）
        /// </summary>
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// 项目的显示名称
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 项目的简短描述
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 项目的主要分类列表
        /// </summary>
        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; } = new List<string>();

        /// <summary>
        /// 客户端侧支持情况
        /// </summary>
        [JsonPropertyName("client_side")]
        public SideSupport ClientSide { get; set; }

        /// <summary>
        /// 服务端侧支持情况
        /// </summary>
        [JsonPropertyName("server_side")]
        public SideSupport ServerSide { get; set; }

        /// <summary>
        /// 项目类型（mod / modpack / resourcepack / shader）
        /// </summary>
        [JsonPropertyName("project_type")]
        public ProjectType ProjectType { get; set; }

        /// <summary>
        /// 项目总下载量
        /// </summary>
        [JsonPropertyName("downloads")]
        public int Downloads { get; set; }

        /// <summary>
        /// 项目图标URL，可能为null
        /// </summary>
        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        /// <summary>
        /// 从项目图标自动生成的RGB颜色值，可能为null
        /// </summary>
        [JsonPropertyName("color")]
        public int? Color { get; set; }

        /// <summary>
        /// 关联的审核线程ID
        /// </summary>
        [JsonPropertyName("thread_id")]
        public string ThreadId { get; set; } = string.Empty;

        /// <summary>
        /// 项目的变现状态
        /// </summary>
        [JsonPropertyName("monetization_status")]
        public MonetizationStatus? MonetizationStatus { get; set; }

        /// <summary>
        /// 项目的唯一ID，base62编码字符串。
        /// 
        /// 关键用途：搜索到模组后，使用此 ID 调用 Get a project / List project's versions API。
        /// </summary>
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// 项目作者的用户名
        /// </summary>
        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// 用于展示的主要分类列表（不含次要分类）
        /// </summary>
        [JsonPropertyName("display_categories")]
        public List<string> DisplayCategories { get; set; } = new List<string>();

        /// <summary>
        /// 项目支持的 Minecraft 游戏版本列表。
        /// 在搜索结果中用于快速判断MC版本兼容性。
        /// </summary>
        [JsonPropertyName("versions")]
        public List<string> Versions { get; set; } = new List<string>();

        /// <summary>
        /// 关注此项目的用户总数
        /// </summary>
        [JsonPropertyName("follows")]
        public int Follows { get; set; }

        /// <summary>
        /// 项目添加到搜索索引的日期（ISO-8601格式）
        /// </summary>
        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// 项目最后修改日期（ISO-8601格式）
        /// </summary>
        [JsonPropertyName("date_modified")]
        public DateTime DateModified { get; set; }

        /// <summary>
        /// 项目支持的最新 Minecraft 版本号
        /// </summary>
        [JsonPropertyName("latest_version")]
        public string LatestVersion { get; set; } = string.Empty;

        /// <summary>
        /// 项目的 SPDX 许可证标识符
        /// </summary>
        [JsonPropertyName("license")]
        public string License { get; set; } = string.Empty;

        /// <summary>
        /// 项目画廊图片URL列表
        /// </summary>
        [JsonPropertyName("gallery")]
        public List<string> Gallery { get; set; } = new List<string>();

        /// <summary>
        /// 项目推荐画廊图片URL，可能为null
        /// </summary>
        [JsonPropertyName("featured_gallery")]
        public string? FeaturedGallery { get; set; }
    }
}