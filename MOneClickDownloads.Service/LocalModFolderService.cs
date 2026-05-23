using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MOneClickDownloads.Service.Models;
using Serilog;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 本地模组文件夹管理服务实现，基于 JSON 文件持久化。<br />
    /// <br />
    /// 职责：<br />
    /// - 管理已记录的本地 mods 文件夹路径列表<br />
    /// - 持久化自定义名称、版本筛选元数据到 JSON 文件<br />
    /// - 变更事件通知<br />
    /// <br />
    /// 存储：<br />
    /// - 文件路径：<c>{存储目录}/local_mod_folders.json</c><br />
    /// - 格式：JSON 对象数组（LocalModFolderEntry[]）<br />
    /// - 向后兼容：自动将旧的字符串数组格式升级为新格式<br />
    /// - 线程安全：所有读写操作通过 lock 保护<br />
    /// </summary>
    public class LocalModFolderService : ILocalModFolderService
    {
        private readonly string _filePath;
        private readonly ILogger _logger;
        private readonly object _lock = new();
        private List<LocalModFolderEntry> _entries = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        /// <inheritdoc />
        public event EventHandler? Changed;

        /// <summary>
        /// 构造本地模组文件夹管理服务。
        /// </summary>
        /// <param name="storagePath">存储目录路径</param>
        public LocalModFolderService(string storagePath)
        {
            _logger = Log.ForContext<LocalModFolderService>();
            _filePath = Path.Combine(storagePath, "local_mod_folders.json");

            Load();
            _logger.Information("LocalModFolderService 初始化完成，已记录 {Count} 个文件夹", _entries.Count);
        }

        /// <inheritdoc />
        public List<LocalModFolderEntry> GetAllFolders()
        {
            lock (_lock)
            {
                return new List<LocalModFolderEntry>(_entries);
            }
        }

        /// <inheritdoc />
        public bool AddFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) return false;

            // 规范化路径
            folderPath = Path.GetFullPath(folderPath.Trim());

            lock (_lock)
            {
                if (_entries.Any(e => string.Equals(e.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.Debug("文件夹已存在，跳过添加: {Path}", folderPath);
                    return false;
                }

                _entries.Add(new LocalModFolderEntry { FolderPath = folderPath });
                Save();
                _logger.Information("已添加文件夹: {Path}", folderPath);
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <inheritdoc />
        public bool RemoveFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) return false;

            folderPath = Path.GetFullPath(folderPath.Trim());

            lock (_lock)
            {
                var removed = _entries.RemoveAll(e =>
                    string.Equals(e.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));

                if (removed == 0)
                {
                    _logger.Debug("文件夹不存在，跳过移除: {Path}", folderPath);
                    return false;
                }

                Save();
                _logger.Information("已移除文件夹: {Path}", folderPath);
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <inheritdoc />
        public bool ContainsFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) return false;

            folderPath = Path.GetFullPath(folderPath.Trim());

            lock (_lock)
            {
                return _entries.Any(e => string.Equals(e.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <inheritdoc />
        public bool RenameFolder(string folderPath, string? customName)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) return false;

            folderPath = Path.GetFullPath(folderPath.Trim());

            lock (_lock)
            {
                var entry = _entries.FirstOrDefault(e =>
                    string.Equals(e.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                {
                    _logger.Debug("文件夹不存在，跳过重命名: {Path}", folderPath);
                    return false;
                }

                entry.CustomName = string.IsNullOrWhiteSpace(customName) ? null : customName.Trim();
                Save();
                _logger.Information("已重命名文件夹: {Path} -> {Name}", folderPath, entry.CustomName ?? "(默认)");
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <inheritdoc />
        public bool UpdateFolderMetadata(string folderPath, List<string> mcVersions, List<string> loaders, string? projectId, string? modName)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) return false;

            folderPath = Path.GetFullPath(folderPath.Trim());

            lock (_lock)
            {
                var entry = _entries.FirstOrDefault(e =>
                    string.Equals(e.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                {
                    _logger.Debug("文件夹不存在，跳过元数据更新: {Path}", folderPath);
                    return false;
                }

                entry.AvailableMcVersions = mcVersions ?? new List<string>();
                entry.AvailableLoaders = loaders ?? new List<string>();
                entry.MetadataProjectId = projectId;
                entry.MetadataModName = modName;
                Save();
                _logger.Information("已更新文件夹元数据: {Path}, MC版本={McCount}, 加载器={LoaderCount}",
                    folderPath, entry.AvailableMcVersions.Count, entry.AvailableLoaders.Count);
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// 从 JSON 文件加载文件夹列表。
        /// 支持两种格式：旧的字符串数组和新的对象数组。
        /// </summary>
        private void Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    _logger.Debug("文件夹配置文件不存在，使用空列表: {Path}", _filePath);
                    _entries = new List<LocalModFolderEntry>();
                    return;
                }

                var json = File.ReadAllText(_filePath);

                // 先尝试按新格式（对象数组）反序列化
                try
                {
                    _entries = JsonSerializer.Deserialize<List<LocalModFolderEntry>>(json) ?? new List<LocalModFolderEntry>();
                    _logger.Debug("已加载文件夹配置（新格式）: {Count} 个文件夹", _entries.Count);
                }
                catch (JsonException)
                {
                    // 新格式失败，尝试旧格式（字符串数组）
                    var oldFolders = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    _entries = oldFolders.Select(p => new LocalModFolderEntry { FolderPath = p }).ToList();

                    // 自动升级为新格式
                    Save();
                    _logger.Information("已将旧格式文件夹配置升级为新格式: {Count} 个文件夹", _entries.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "加载文件夹配置失败，使用空列表: {Path}", _filePath);
                _entries = new List<LocalModFolderEntry>();
            }
        }

        /// <summary>
        /// 保存文件夹列表到 JSON 文件
        /// </summary>
        private void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_entries, JsonOptions);
                File.WriteAllText(_filePath, json);
                _logger.Debug("已保存文件夹配置: {Count} 个文件夹", _entries.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存文件夹配置失败: {Path}", _filePath);
            }
        }
    }
}