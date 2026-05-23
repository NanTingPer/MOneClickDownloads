using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Serilog;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 本地模组文件夹管理服务实现，基于 JSON 文件持久化。<br />
    /// <br />
    /// 职责：<br />
    /// - 管理已记录的本地 mods 文件夹路径列表<br />
    /// - 持久化到 JSON 文件<br />
    /// - 变更事件通知<br />
    /// <br />
    /// 存储：<br />
    /// - 文件路径：<c>{存储目录}/local_mod_folders.json</c><br />
    /// - 格式：JSON 字符串数组，每个元素为一个文件夹完整路径<br />
    /// - 线程安全：所有读写操作通过 lock 保护<br />
    /// </summary>
    public class LocalModFolderService : ILocalModFolderService
    {
        private readonly string _filePath;
        private readonly ILogger _logger;
        private readonly object _lock = new();
        private List<string> _folders = new();

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
            _logger.Information("LocalModFolderService 初始化完成，已记录 {Count} 个文件夹", _folders.Count);
        }

        /// <inheritdoc />
        public List<string> GetAllFolders()
        {
            lock (_lock)
            {
                return new List<string>(_folders);
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
                if (_folders.Contains(folderPath, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.Debug("文件夹已存在，跳过添加: {Path}", folderPath);
                    return false;
                }

                _folders.Add(folderPath);
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
                var removed = _folders.RemoveAll(f =>
                    string.Equals(f, folderPath, StringComparison.OrdinalIgnoreCase));

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
                return _folders.Contains(folderPath, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// 从 JSON 文件加载文件夹列表
        /// </summary>
        private void Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    _logger.Debug("文件夹配置文件不存在，使用空列表: {Path}", _filePath);
                    _folders = new List<string>();
                    return;
                }

                var json = File.ReadAllText(_filePath);
                _folders = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                _logger.Debug("已加载文件夹配置: {Count} 个文件夹", _folders.Count);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "加载文件夹配置失败，使用空列表: {Path}", _filePath);
                _folders = new List<string>();
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

                var json = JsonSerializer.Serialize(_folders, JsonOptions);
                File.WriteAllText(_filePath, json);
                _logger.Debug("已保存文件夹配置: {Count} 个文件夹", _folders.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存文件夹配置失败: {Path}", _filePath);
            }
        }
    }
}