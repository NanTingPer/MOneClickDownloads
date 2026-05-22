using System;
using System.Collections.Generic;
using MOneClickDownloads.DataModel.Favorites;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 收藏夹（合集）服务接口，负责管理用户的模组合集。<br />
    /// <br />
    /// 职责：<br />
    /// - 合集的增删改查（创建、重命名、删除）<br />
    /// - 合集内模组项目的添加和移除<br />
    /// - 收藏状态查询（判断某模组是否在任意合集中）<br />
    /// - 变更事件通知（供 UI 层响应更新）<br />
    /// <br />
    /// 存储：<br />
    /// - 使用单个 JSON 文件存储所有合集数据（<c>package/favorites.json</c>）<br />
    /// - 线程安全，所有读写操作加锁<br />
    /// <br />
    /// 使用示例：<br />
    /// <code>
    /// // 创建合集
    /// var collection = favoriteService.CreateCollection("生存必备");
    /// 
    /// // 添加模组到合集
    /// favoriteService.AddItem(collection.Id, new FavoriteItem { ProjectId = "P7dR8mSH", Title = "JEI" });
    /// 
    /// // 检查是否已收藏
    /// bool favorited = favoriteService.IsInAnyCollection("P7dR8mSH");
    /// </code>
    /// </summary>
    public interface IFavoriteService
    {
        /// <summary>
        /// 收藏数据发生变更时触发（添加/移除模组、创建/删除/重命名合集）。
        /// UI 层可订阅此事件刷新列表。
        /// </summary>
        event EventHandler? Changed;

        // ===== 合集管理 =====

        /// <summary>
        /// 获取所有合集列表。
        /// </summary>
        /// <returns>所有合集的只读列表</returns>
        List<FavoriteCollection> GetAllCollections();

        /// <summary>
        /// 按 ID 获取单个合集。
        /// </summary>
        /// <param name="collectionId">合集 ID</param>
        /// <returns>合集对象；未找到返回 null</returns>
        FavoriteCollection? GetCollection(string collectionId);

        /// <summary>
        /// 创建新合集。
        /// </summary>
        /// <param name="name">合集名称</param>
        /// <returns>新创建的合集对象</returns>
        FavoriteCollection CreateCollection(string name);

        /// <summary>
        /// 重命名合集。
        /// </summary>
        /// <param name="collectionId">合集 ID</param>
        /// <param name="newName">新的合集名称</param>
        /// <exception cref="ArgumentException">合集不存在时抛出</exception>
        void RenameCollection(string collectionId, string newName);

        /// <summary>
        /// 删除合集及其所有包含的模组条目。
        /// </summary>
        /// <param name="collectionId">合集 ID</param>
        /// <returns>是否成功删除（合集不存在时返回 false）</returns>
        bool DeleteCollection(string collectionId);

        // ===== 项目管理 =====

        /// <summary>
        /// 向指定合集添加模组项目。
        /// </summary>
        /// <param name="collectionId">目标合集 ID</param>
        /// <param name="item">要添加的模组条目</param>
        /// <exception cref="ArgumentException">合集不存在时抛出</exception>
        void AddItem(string collectionId, FavoriteItem item);

        /// <summary>
        /// 从指定合集中移除模组项目。
        /// </summary>
        /// <param name="collectionId">目标合集 ID</param>
        /// <param name="projectId">要移除的模组 ProjectId</param>
        /// <returns>是否成功移除（合集不存在或项目不在合集中时返回 false）</returns>
        bool RemoveItem(string collectionId, string projectId);

        /// <summary>
        /// 判断指定模组是否存在于任意合集中。
        /// </summary>
        /// <param name="projectId">模组 ProjectId</param>
        /// <returns>是否已收藏</returns>
        bool IsInAnyCollection(string projectId);

        /// <summary>
        /// 获取包含指定模组的所有合集 ID 列表。
        /// </summary>
        /// <param name="projectId">模组 ProjectId</param>
        /// <returns>包含该模组的合集 ID 列表</returns>
        List<string> GetCollectionIdsContaining(string projectId);
    }
}