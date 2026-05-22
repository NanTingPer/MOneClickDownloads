using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Favorites
{
    /// <summary>
    /// 收藏夹（合集）模型，表示一个用户自定义的模组集合。
    /// <br />
    /// 用途：
    /// - 作为 <see cref="Service.IFavoriteService"/> 管理的基本单元
    /// - 一个合集拥有名称和多个 <see cref="FavoriteItem"/> 条目
    /// - 可创建多个合集分类管理模组
    /// <br />
    /// 存储：
    /// - 以 JSON 文件形式持久化，多个合集存储在同一个数组中
    /// </summary>
    public class FavoriteCollection
    {
        /// <summary>
        /// 合集唯一标识（GUID），创建时自动生成
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 合集名称，用户可自定义修改
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 合集中的模组列表
        /// </summary>
        [JsonPropertyName("items")]
        public List<FavoriteItem> Items { get; set; } = new List<FavoriteItem>();

        /// <summary>
        /// 合集创建时间（ISO-8601格式）
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 合集最后更新时间（ISO-8601格式），添加/移除项目或重命名时更新
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}