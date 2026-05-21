using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Enums;

namespace MOneClickDownloads.DataModel.Project
{
    /// <summary>
    /// 完整项目详情模型，表示 Modrinth 上一个项目的全部信息。
    /// 
    /// 用途：
    /// - 作为 Get a project API（GET /project/{id|slug}）的响应数据模型
    /// - 获取项目的基本信息（名称、描述、分类等）
    /// - 获取项目的所有版本ID列表，用于后续查询版本详情
    /// - 获取项目的加载器和游戏版本支持信息，用于筛选兼容版本
    /// - 在下载流程中：先通过搜索获取 project_id，再用此端点获取完整项目信息和版本列表
    /// 
    /// 数据流转：
    /// - 从 Get a project API 响应反序列化
    /// - 其 Versions 属性（版本ID列表）用于调用 Get a version / List project's versions API
    /// - 其 GameVersions 和 Loaders 属性用于过滤兼容指定MC版本的版本
    /// </summary>
    public class Project : IApiModel
    {
        /// <summary>
        /// 项目的唯一ID，base62编码的字符串（如 "P7dR8mSH"）
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 项目的URL友好标识符（如 "fabric-api"），用于构建访问链接
        /// 正则约束: ^[\w!@$()`.+,"\-']{3,64}$
        /// </summary>
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// 项目的显示名称（如 "Fabric API"）
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 项目的简短描述，显示在搜索结果和项目卡片中
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 项目的主要分类列表（如 ["technology", "adventure", "fabric"]）
        /// 在搜索中用作 facet 过滤条件
        /// </summary>
        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; } = new List<string>();

        /// <summary>
        /// 客户端侧支持情况，标识此模组在客户端是否必需/可选/不支持
        /// </summary>
        [JsonPropertyName("client_side")]
        public SideSupport ClientSide { get; set; }

        /// <summary>
        /// 服务端侧支持情况，标识此模组在服务端是否必需/可选/不支持
        /// </summary>
        [JsonPropertyName("server_side")]
        public SideSupport ServerSide { get; set; }

        /// <summary>
        /// 项目的详细描述（Markdown格式），显示在项目详情页的正文部分
        /// </summary>
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// 项目的审核状态，仅项目拥有者和管理员可见
        /// </summary>
        [JsonPropertyName("status")]
        public ProjectStatus Status { get; set; }

        /// <summary>
        /// 提交审核时请求的目标状态，可能为null
        /// </summary>
        [JsonPropertyName("requested_status")]
        public ProjectStatus? RequestedStatus { get; set; }

        /// <summary>
        /// 次要分类列表，可搜索但不在主要分类中显示
        /// </summary>
        [JsonPropertyName("additional_categories")]
        public List<string> AdditionalCategories { get; set; } = new List<string>();

        /// <summary>
        /// 问题追踪页面链接（如 GitHub Issues URL），可能为null
        /// </summary>
        [JsonPropertyName("issues_url")]
        public string? IssuesUrl { get; set; }

        /// <summary>
        /// 源代码仓库链接，可能为null
        /// </summary>
        [JsonPropertyName("source_url")]
        public string? SourceUrl { get; set; }

        /// <summary>
        /// Wiki页面链接，可能为null
        /// </summary>
        [JsonPropertyName("wiki_url")]
        public string? WikiUrl { get; set; }

        /// <summary>
        /// Discord 邀请链接，可能为null
        /// </summary>
        [JsonPropertyName("discord_url")]
        public string? DiscordUrl { get; set; }

        /// <summary>
        /// 捐赠平台链接列表
        /// </summary>
        [JsonPropertyName("donation_urls")]
        public List<DonationUrl> DonationUrls { get; set; } = new List<DonationUrl>();

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
        /// 项目的变现状态，仅项目拥有者和管理员可见
        /// </summary>
        [JsonPropertyName("monetization_status")]
        public MonetizationStatus? MonetizationStatus { get; set; }

        /// <summary>
        /// 拥有此项目的团队ID
        /// </summary>
        [JsonPropertyName("team")]
        public string Team { get; set; } = string.Empty;

        /// <summary>
        /// 详细描述的外部链接（始终为null，仅保留用于向后兼容）
        /// </summary>
        [JsonPropertyName("body_url")]
        public string? BodyUrl { get; set; }

        /// <summary>
        /// 管理员发送的消息，仅在管理员发过消息时存在
        /// </summary>
        [JsonPropertyName("moderator_message")]
        public ModeratorMessage? ModeratorMessage { get; set; }

        /// <summary>
        /// 项目发布时间（ISO-8601格式）
        /// </summary>
        [JsonPropertyName("published")]
        public DateTime Published { get; set; }

        /// <summary>
        /// 项目最后更新时间（ISO-8601格式）
        /// </summary>
        [JsonPropertyName("updated")]
        public DateTime Updated { get; set; }

        /// <summary>
        /// 项目状态被设为已审核的日期，可能为null
        /// </summary>
        [JsonPropertyName("approved")]
        public DateTime? Approved { get; set; }

        /// <summary>
        /// 项目提交审核的日期，可能为null
        /// </summary>
        [JsonPropertyName("queued")]
        public DateTime? Queued { get; set; }

        /// <summary>
        /// 关注此项目的用户总数
        /// </summary>
        [JsonPropertyName("followers")]
        public int Followers { get; set; }

        /// <summary>
        /// 项目的许可证信息
        /// </summary>
        [JsonPropertyName("license")]
        public LicenseInfo? License { get; set; }

        /// <summary>
        /// 项目所有版本的ID列表（base62编码）。
        /// 不会为空，除非项目处于 draft 状态。
        /// 
        /// 关键用途：获取版本ID后，可调用 Get a version API 获取具体版本详情，
        /// 或调用 List project's versions API 按 game_versions/loaders 过滤。
        /// </summary>
        [JsonPropertyName("versions")]
        public List<string> Versions { get; set; } = new List<string>();

        /// <summary>
        /// 项目支持的所有 Minecraft 游戏版本列表。
        /// 在搜索中用作 facet 过滤条件（如 versions:1.21.11）。
        /// </summary>
        [JsonPropertyName("game_versions")]
        public List<string> GameVersions { get; set; } = new List<string>();

        /// <summary>
        /// 项目支持的所有模组加载器列表（如 ["forge", "fabric", "quilt"]）。
        /// 在搜索中与 categories 一起作为 facet 过滤条件。
        /// </summary>
        [JsonPropertyName("loaders")]
        public List<string> Loaders { get; set; } = new List<string>();

        /// <summary>
        /// 项目画廊图片列表
        /// </summary>
        [JsonPropertyName("gallery")]
        public List<GalleryImage> Gallery { get; set; } = new List<GalleryImage>();

        /// <summary>
        /// 获取此模型对应的 API 端点路径。
        /// 使用时将 {id|slug} 替换为实际的项目ID或slug。
        /// </summary>
        public string GetEndpoint() => "/project/{id|slug}";
    }
}