using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Enums;

namespace MOneClickDownloads.DataModel.Favorites
{
    /// <summary>
    /// 收藏夹中的单个模组条目，存储从 <see cref="Project.Project"/> 提取的精简字段。
    /// <br />
    /// 用途：
    /// - 作为 <see cref="FavoriteCollection.Items"/> 数组中的元素
    /// - 在收藏夹列表中展示模组卡片信息
    /// - 其 ProjectId 可用于跳转到模组详情页或 API 查询
    /// </summary>
    public class FavoriteItem
    {
        /// <summary>
        /// 模组唯一ID（Project.Id），base62编码字符串
        /// </summary>
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// 模组显示名称（Project.Title）
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 模组简短描述（Project.Description）
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 项目的URL友好标识符（Project.Slug），用于构建访问链接和导航
        /// </summary>
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// 项目图标URL（Project.IconUrl），可能为null
        /// </summary>
        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        /// <summary>
        /// 项目类型（mod / modpack / resourcepack / shader）
        /// </summary>
        [JsonPropertyName("project_type")]
        public ProjectType ProjectType { get; set; }

        /// <summary>
        /// 项目的主要分类列表（如 ["technology", "adventure"]）
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
        /// 项目总下载量
        /// </summary>
        [JsonPropertyName("downloads")]
        public int Downloads { get; set; }

        /// <summary>
        /// 收藏时间（本地生成，ISO-8601格式）
        /// </summary>
        [JsonPropertyName("favorited_at")]
        public DateTime FavoritedAt { get; set; } = DateTime.UtcNow;
    }
}