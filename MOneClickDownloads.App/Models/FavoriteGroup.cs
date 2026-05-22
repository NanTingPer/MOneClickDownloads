using System.Collections.Generic;

namespace MOneClickDownloads.App.Models
{
    /// <summary>
    /// 收藏夹合集分组，用于在UI中按合集名称分组展示收藏列表。
    /// 对应 McVersionGroup 在版本列表中的角色。
    /// </summary>
    public class FavoriteGroup
    {
        /// <summary>
        /// 合集唯一标识
        /// </summary>
        public string CollectionId { get; set; } = string.Empty;

        /// <summary>
        /// 合集名称（如 "生存必备", "红石科技"）
        /// </summary>
        public string CollectionName { get; set; } = string.Empty;

        /// <summary>
        /// 合集中的模组条目列表
        /// </summary>
        public List<FavoriteDisplayItem> Items { get; set; } = new List<FavoriteDisplayItem>();

        /// <summary>
        /// 合集中的模组数量
        /// </summary>
        public int ItemCount => Items.Count;
    }
}