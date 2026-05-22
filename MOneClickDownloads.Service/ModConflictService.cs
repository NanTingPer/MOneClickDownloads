using System.Text.RegularExpressions;
using MOneClickDownloads.DataModel.Mod;
using MOneClickDownloads.DataModel.Version;
using MOneClickDownloads.Service.Models;
using Serilog;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 模组冲突检测服务实现，负责检测待下载模组与本地已安装模组之间的冲突。<br />
    /// <br />
    /// 无状态服务：所有方法均为纯函数，线程安全。<br />
    /// <br />
    /// 检测策略（三级检测）：<br />
    /// 1. <see cref="DetectBySlugOrTitle"/> — 下载前预检，通过 slug/title 匹配本地模组<br />
    /// 2. <see cref="DetectByModId"/> — JAR 分析后检测，通过提取的 ModId 匹配<br />
    /// 3. <see cref="DetectByFileName"/> — 最终回退，通过文件名精确匹配<br />
    /// <br />
    /// 使用示例：<br />
    /// <code>
    /// var analysisService = new ModAnalysisService();
    /// var conflictService = new ModConflictService();
    /// var inventory = new LocalModInventory(@"C:\mods", analysisService);
    /// await inventory.ScanAsync();
    /// 
    /// // 下载前预检
    /// var conflict = conflictService.DetectBySlugOrTitle(inventory, version, file, "fabric-api", "Fabric API");
    /// if (conflict != null) { /* 处理冲突 */ }
    /// </code>
    /// </summary>
    public class ModConflictService : IModConflictService
    {
        private readonly ILogger _logger;

        public ModConflictService()
        {
            _logger = Log.ForContext<ModConflictService>();
        }

        /// <inheritdoc />
        public ModConflictInfo? DetectBySlugOrTitle(
            LocalModInventory inventory,
            ModrinthVersion version,
            VersionFile file,
            string? projectSlug,
            string? projectTitle)
        {
            ModAnalysisResult? existing = null;

            // 第一优先：按 slug 匹配本地 ModId
            if (!string.IsNullOrEmpty(projectSlug))
            {
                existing = inventory.FindByModId(projectSlug);
                if (existing != null)
                {
                    _logger.Debug("slug 预检命中: {Slug} → ModId={ModId}", projectSlug, existing.ModId);
                }
            }

            // 第二优先：按 title 匹配本地 Name
            if (existing == null && !string.IsNullOrEmpty(projectTitle))
            {
                foreach (var installed in inventory.InstalledMods)
                {
                    if (string.Equals(installed.Name, projectTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        existing = installed;
                        _logger.Debug("title 预检命中: {Title} → ModId={ModId}", projectTitle, existing.ModId);
                        break;
                    }
                }
            }

            if (existing == null)
            {
                return null; // 预检未命中
            }

            return BuildConflictInfo(inventory, existing, version, file);
        }

        /// <inheritdoc />
        public ModConflictInfo? DetectByModId(
            LocalModInventory inventory,
            ModAnalysisResult downloadedAnalysis,
            ModrinthVersion version,
            VersionFile file)
        {
            var existing = inventory.FindByModId(downloadedAnalysis.ModId);
            if (existing == null)
            {
                return null; // 本地无同 ModId 模组
            }

            return BuildConflictInfo(inventory, existing, version, file);
        }

        /// <inheritdoc />
        public ModConflictInfo? DetectByFileName(
            LocalModInventory inventory,
            ModrinthVersion version,
            VersionFile file)
        {
            var existing = inventory.FindByFileName(file.Filename);
            if (existing == null)
            {
                return null; // 无冲突
            }

            var existingFilePath = inventory.GetFilePathByFileName(file.Filename);
            return BuildConflictInfo(existing, version, file, existingFilePath);
        }

        /// <inheritdoc />
        public int CompareVersions(string version1, string version2)
        {
            // 去除 + 后缀（build metadata）
            var v1 = version1.Split('+')[0];
            var v2 = version2.Split('+')[0];

            // 尝试解析为数字序列
            var parts1 = ParseVersionParts(v1);
            var parts2 = ParseVersionParts(v2);

            if (parts1 != null && parts2 != null)
            {
                // 按主.次.修订逐级比较
                var maxLen = Math.Max(parts1.Count, parts2.Count);
                for (int i = 0; i < maxLen; i++)
                {
                    var p1 = i < parts1.Count ? parts1[i] : 0;
                    var p2 = i < parts2.Count ? parts2[i] : 0;
                    if (p1 != p2)
                        return p1.CompareTo(p2);
                }
                return 0;
            }

            // 回退：字符串比较
            return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 构造冲突信息的通用辅助方法（通过 inventory 获取文件路径）。
        /// </summary>
        private ModConflictInfo BuildConflictInfo(
            LocalModInventory inventory,
            ModAnalysisResult existing,
            ModrinthVersion version,
            VersionFile file)
        {
            var existingFilePath = inventory.GetModFilePath(existing.ModId);
            return BuildConflictInfo(existing, version, file, existingFilePath);
        }

        /// <summary>
        /// 构造冲突信息的核心逻辑。
        /// </summary>
        private ModConflictInfo BuildConflictInfo(
            ModAnalysisResult existing,
            ModrinthVersion version,
            VersionFile file,
            string? existingFilePath)
        {
            var existingVersion = existing.Version;
            var newVersion = version.VersionNumber;

            // 版本完全相同
            if (string.Equals(existingVersion, newVersion, StringComparison.OrdinalIgnoreCase))
            {
                return new ModConflictInfo
                {
                    ConflictType = DataModel.Enums.ModConflictType.SameModExists,
                    ModId = existing.ModId,
                    ModName = existing.Name,
                    ExistingVersion = existingVersion,
                    ExistingFilePath = existingFilePath,
                    NewVersion = newVersion,
                    NewFileName = file.Filename
                };
            }

            // 版本比较
            var comparison = CompareVersions(existingVersion, newVersion);
            if (comparison > 0)
            {
                // 本地版本更高
                return new ModConflictInfo
                {
                    ConflictType = DataModel.Enums.ModConflictType.HigherVersionExists,
                    ModId = existing.ModId,
                    ModName = existing.Name,
                    ExistingVersion = existingVersion,
                    ExistingFilePath = existingFilePath,
                    NewVersion = newVersion,
                    NewFileName = file.Filename
                };
            }
            else
            {
                // 本地版本更低（可升级）
                return new ModConflictInfo
                {
                    ConflictType = DataModel.Enums.ModConflictType.LowerVersionExists,
                    ModId = existing.ModId,
                    ModName = existing.Name,
                    ExistingVersion = existingVersion,
                    ExistingFilePath = existingFilePath,
                    NewVersion = newVersion,
                    NewFileName = file.Filename
                };
            }
        }

        /// <summary>
        /// 尝试将版本号解析为数字列表（如 "1.20.1" → [1, 20, 1]）。<br />
        /// 遇到非数字部分时截断（如 "1.20.1-beta" → [1, 20, 1]）。<br />
        /// 无法解析时返回 null。
        /// </summary>
        /// <param name="version">版本号字符串</param>
        /// <returns>数字列表，或 null</returns>
        private static List<int>? ParseVersionParts(string version)
        {
            var parts = new List<int>();
            // 按 - 分割取第一段（忽略 pre-release 标识）
            var numericPart = version.Split('-')[0];
            // 按 . 分割，尝试解析每段为数字
            foreach (var segment in numericPart.Split('.'))
            {
                // 去除前导非数字字符（如 "v1" → "1"）
                var cleaned = Regex.Replace(segment, @"^[^\d]+", "");
                if (string.IsNullOrEmpty(cleaned)) continue;
                if (int.TryParse(cleaned, out var num))
                {
                    parts.Add(num);
                }
                else
                {
                    // 遇到无法解析的段 → 返回 null
                    return parts.Count > 0 ? parts : null;
                }
            }
            return parts.Count > 0 ? parts : null;
        }
    }
}