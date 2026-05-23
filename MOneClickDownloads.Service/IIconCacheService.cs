using System.Collections.Generic;
using System.Threading.Tasks;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 图标缓存服务接口，负责将模组图标从网络下载并持久化到本地磁盘缓存。
    /// 缓存目录位于 App 根目录下的 icon_cache 文件夹。
    /// 文件命名规则：{modId}_{iconFileName}
    /// </summary>
    public interface IIconCacheService
    {
        /// <summary>
        /// 获取缓存的图标本地路径（如果存在），否则返回 null。
        /// </summary>
        /// <param name="modId">本地模组 ID（非 base62 ProjectId）</param>
        /// <param name="iconUrl">图标网络 URL</param>
        /// <returns>缓存文件的本地路径，或 null</returns>
        string? GetCachedIconPath(string modId, string iconUrl);

        /// <summary>
        /// 下载并缓存图标到本地磁盘。如果已存在则更新缓存。
        /// </summary>
        /// <param name="modId">本地模组 ID</param>
        /// <param name="iconUrl">图标网络 URL</param>
        /// <returns>缓存文件的本地路径，失败时返回 null</returns>
        Task<string?> CacheIconAsync(string modId, string iconUrl);

        /// <summary>
        /// 批量缓存图标（用于搜索结果、刷新元数据等场景）。
        /// </summary>
        /// <param name="items">模组 ID 与图标 URL 的元组列表</param>
        /// <returns></returns>
        Task CacheIconsAsync(IEnumerable<(string ModId, string IconUrl)> items);
    }
}