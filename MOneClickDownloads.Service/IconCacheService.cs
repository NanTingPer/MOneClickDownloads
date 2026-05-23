using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 图标缓存服务实现，将模组图标从网络下载并持久化到本地磁盘。
    /// <br />
    /// 缓存目录：{AppBaseDirectory}/icon_cache/<br />
    /// 文件命名：{modId}_{iconFileName}<br />
    /// <br />
    /// 工作流程：<br />
    /// 1. 调用方传入 modId + iconUrl<br />
    /// 2. 从 URL 提取图标文件名（如 icon.png）<br />
    /// 3. 构建缓存文件名 {modId}_{iconFileName}<br />
    /// 4. 如果缓存文件已存在且大小 > 0，直接返回路径<br />
    /// 5. 否则从网络下载并保存到缓存目录<br />
    /// </summary>
    public class IconCacheService : IIconCacheService
    {
        private static readonly ILogger Logger = Log.ForContext<IconCacheService>();
        private static readonly HttpClient HttpClient = new();

        private readonly string _cacheDirectory;

        public IconCacheService(string appBaseDirectory)
        {
            _cacheDirectory = Path.Combine(appBaseDirectory, "icon_cache");

            // 确保缓存目录存在
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
                Logger.Information("已创建图标缓存目录: {Path}", _cacheDirectory);
            }
        }

        /// <inheritdoc />
        public string? GetCachedIconPath(string modId, string iconUrl)
        {
            if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(iconUrl))
                return null;

            var fileName = BuildCacheFileName(modId, iconUrl);
            var filePath = Path.Combine(_cacheDirectory, fileName);

            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
                return filePath;

            return null;
        }

        /// <inheritdoc />
        public async Task<string?> CacheIconAsync(string modId, string iconUrl)
        {
            if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(iconUrl))
                return null;

            var fileName = BuildCacheFileName(modId, iconUrl);
            var filePath = Path.Combine(_cacheDirectory, fileName);

            try
            {
                // 使用临时文件，下载完成后重命名，避免写入中断导致损坏文件
                var tempPath = filePath + ".tmp";

                var bytes = await HttpClient.GetByteArrayAsync(iconUrl);
                await File.WriteAllBytesAsync(tempPath, bytes);

                // 删除旧缓存（如果有），然后重命名
                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.Move(tempPath, filePath);

                Logger.Debug("图标缓存成功: {ModId} -> {Path}", modId, filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "图标缓存失败: {ModId}, Url={Url}", modId, iconUrl);

                // 清理临时文件
                var tempPath = filePath + ".tmp";
                TryDeleteFile(tempPath);

                return null;
            }
        }

        /// <inheritdoc />
        public async Task CacheIconsAsync(IEnumerable<(string ModId, string IconUrl)> items)
        {
            var itemList = items
                .Where(i => !string.IsNullOrWhiteSpace(i.ModId) && !string.IsNullOrWhiteSpace(i.IconUrl))
                .ToList();

            if (itemList.Count == 0)
                return;

            Logger.Information("开始批量缓存图标: {Count} 个", itemList.Count);

            var cached = 0;
            var skipped = 0;

            foreach (var (modId, iconUrl) in itemList)
            {
                // 如果已缓存则跳过
                if (GetCachedIconPath(modId, iconUrl) != null)
                {
                    skipped++;
                    continue;
                }

                var result = await CacheIconAsync(modId, iconUrl);
                if (result != null)
                    cached++;
            }

            Logger.Information("批量缓存图标完成: 新缓存={Cached}, 已存在跳过={Skipped}", cached, skipped);
        }

        /// <summary>
        /// 构建缓存文件名：{modId}_{iconFileName}
        /// 从 URL 中提取图标文件名部分，去除查询参数。
        /// </summary>
        private static string BuildCacheFileName(string modId, string iconUrl)
        {
            var iconFileName = ExtractIconFileName(iconUrl);
            // 清理 modId 中的非法文件名字符
            var safeModId = SanitizeFileName(modId);
            return $"{safeModId}_{iconFileName}";
        }

        /// <summary>
        /// 从 URL 提取图标文件名，去除查询参数和片段。
        /// 例如：https://cdn.modrinth.com/data/icon.png?hash=abc -> icon.png
        /// </summary>
        private static string ExtractIconFileName(string url)
        {
            try
            {
                var uri = new Uri(url);
                var lastSegment = uri.Segments.LastOrDefault() ?? "icon.png";

                // URL decode
                lastSegment = Uri.UnescapeDataString(lastSegment);

                // 清理非法文件名字符
                lastSegment = SanitizeFileName(lastSegment);

                if (string.IsNullOrWhiteSpace(lastSegment))
                    return "icon.png";

                return lastSegment;
            }
            catch
            {
                return "icon.png";
            }
        }

        /// <summary>
        /// 清理字符串中的非法文件名字符
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
        }

        private static void TryDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {
                // 忽略删除失败
            }
        }
    }
}