using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MOneClickDownloads.DataModel.Favorites;
using Serilog;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 收藏夹（合集）服务实现，基于 JSON 文件持久化。<br />
    /// <br />
    /// 职责：<br />
    /// - 管理多个合集的增删改查<br />
    /// - 管理合集内模组项目的添加和移除<br />
    /// - 提供收藏状态查询<br />
    /// - 变更事件通知<br />
    /// <br />
    /// 存储：<br />
    /// - 文件路径：<c>{存储目录}/favorites.json</c><br />
    /// - 格式：JSON 数组，每个元素为一个 <see cref="FavoriteCollection"/><br />
    /// - 线程安全：所有读写操作通过 lock 保护<br />
    /// <br />
    /// 设计模式：<br />
    /// - 参考 <see cref="ConfigService"/> 的 JSON 文件读写模式<br />
    /// - 构造时传入存储目录路径，由 DI 注入<br />
    /// - 每次变更后自动 Save<br />
    /// - 文件不存在时自动创建空数组<br />
    /// </summary>
    public class FavoriteService : IFavoriteService
    {
        private readonly string _filePath;
        private readonly ILogger _logger;
        private readonly object _lock = new();
        private List<FavoriteCollection> _collections = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        /// <inheritdoc />
        public event EventHandler? Changed;

        /// <summary>
        /// 构造收藏夹服务。<br />
        /// <br />
        /// 数据流：<br />
        /// - 保存存储目录路径和文件全路径<br />
        /// - 确保目录存在<br />
        /// - 尝试从文件加载已有数据；文件不存在则初始化为空列表<br />
        /// </summary>
        /// <param name="storageDirectory">存储目录路径（如 package/）</param>
        public FavoriteService(string storageDirectory)
        {
            if (storageDirectory == null) throw new ArgumentNullException(nameof(storageDirectory));

            _filePath = Path.Combine(storageDirectory, "favorites.json");
            _logger = Log.ForContext<FavoriteService>();

            EnsureDirectoryExists();
            Load();

            _logger.Information("FavoriteService 已初始化: FilePath={FilePath}, 合集数量={Count}", _filePath, _collections.Count);
        }

        // ===== 合集管理 =====

        /// <inheritdoc />
        public List<FavoriteCollection> GetAllCollections()
        {
            lock (_lock)
            {
                return new List<FavoriteCollection>(_collections);
            }
        }

        /// <inheritdoc />
        public FavoriteCollection? GetCollection(string collectionId)
        {
            lock (_lock)
            {
                return _collections.FirstOrDefault(c => c.Id == collectionId);
            }
        }

        /// <inheritdoc />
        public FavoriteCollection CreateCollection(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("合集名称不能为空", nameof(name));

            var collection = new FavoriteCollection
            {
                Name = name.Trim()
            };

            lock (_lock)
            {
                _collections.Add(collection);
                Save();
            }

            _logger.Information("已创建合集: Id={Id}, Name={Name}", collection.Id, collection.Name);
            OnChanged();
            return collection;
        }

        /// <inheritdoc />
        public void RenameCollection(string collectionId, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("合集名称不能为空", nameof(newName));

            lock (_lock)
            {
                var collection = _collections.FirstOrDefault(c => c.Id == collectionId);
                if (collection == null)
                    throw new ArgumentException($"合集不存在: {collectionId}", nameof(collectionId));

                collection.Name = newName.Trim();
                collection.UpdatedAt = DateTime.UtcNow;
                Save();
            }

            _logger.Information("已重命名合集: Id={Id}, NewName={NewName}", collectionId, newName);
            OnChanged();
        }

        /// <inheritdoc />
        public bool DeleteCollection(string collectionId)
        {
            lock (_lock)
            {
                var index = _collections.FindIndex(c => c.Id == collectionId);
                if (index < 0) return false;

                var name = _collections[index].Name;
                _collections.RemoveAt(index);
                Save();

                _logger.Information("已删除合集: Id={Id}, Name={Name}", collectionId, name);
            }

            OnChanged();
            return true;
        }

        // ===== 项目管理 =====

        /// <inheritdoc />
        public void AddItem(string collectionId, FavoriteItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            lock (_lock)
            {
                var collection = _collections.FirstOrDefault(c => c.Id == collectionId);
                if (collection == null)
                    throw new ArgumentException($"合集不存在: {collectionId}", nameof(collectionId));

                // 防止重复添加
                if (collection.Items.Any(i => i.ProjectId == item.ProjectId))
                {
                    _logger.Warning("模组已存在于合集中，跳过添加: CollectionId={CollectionId}, ProjectId={ProjectId}", collectionId, item.ProjectId);
                    return;
                }

                item.FavoritedAt = DateTime.UtcNow;
                collection.Items.Add(item);
                collection.UpdatedAt = DateTime.UtcNow;
                Save();
            }

            _logger.Information("已添加模组到合集: CollectionId={CollectionId}, ProjectId={ProjectId}, Title={Title}", collectionId, item.ProjectId, item.Title);
            OnChanged();
        }

        /// <inheritdoc />
        public bool RemoveItem(string collectionId, string projectId)
        {
            lock (_lock)
            {
                var collection = _collections.FirstOrDefault(c => c.Id == collectionId);
                if (collection == null) return false;

                var index = collection.Items.FindIndex(i => i.ProjectId == projectId);
                if (index < 0) return false;

                collection.Items.RemoveAt(index);
                collection.UpdatedAt = DateTime.UtcNow;
                Save();
            }

            _logger.Information("已从合集移除模组: CollectionId={CollectionId}, ProjectId={ProjectId}", collectionId, projectId);
            OnChanged();
            return true;
        }

        /// <inheritdoc />
        public bool IsInAnyCollection(string projectId)
        {
            lock (_lock)
            {
                return _collections.Any(c => c.Items.Any(i => i.ProjectId == projectId));
            }
        }

        /// <inheritdoc />
        public List<string> GetCollectionIdsContaining(string projectId)
        {
            lock (_lock)
            {
                return _collections
                    .Where(c => c.Items.Any(i => i.ProjectId == projectId))
                    .Select(c => c.Id)
                    .ToList();
            }
        }

        // ===== 内部方法 =====

        /// <summary>
        /// 触发变更事件。
        /// </summary>
        private void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 将当前合集数据写入 JSON 文件。<br />
        /// <br />
        /// 数据流：<br />
        /// - 序列化合集列表为格式化 JSON 字符串<br />
        /// - 写入文件（覆盖写入）<br />
        /// </summary>
        private void Save()
        {
            try
            {
                EnsureDirectoryExists();
                var json = JsonSerializer.Serialize(_collections, JsonOptions);
                File.WriteAllText(_filePath, json);
                _logger.Debug("收藏夹数据已保存: FilePath={FilePath}, 合集数量={Count}", _filePath, _collections.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存收藏夹数据失败: FilePath={FilePath}", _filePath);
            }
        }

        /// <summary>
        /// 从 JSON 文件加载合集数据。<br />
        /// <br />
        /// 数据流：<br />
        /// - 检查文件是否存在<br />
        /// - 存在则读取并反序列化为列表<br />
        /// - 不存在则初始化为空列表<br />
        /// </summary>
        private void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        _collections = JsonSerializer.Deserialize<List<FavoriteCollection>>(json)
                                       ?? new List<FavoriteCollection>();
                        _logger.Information("收藏夹数据已加载: FilePath={FilePath}, 合集数量={Count}", _filePath, _collections.Count);
                        return;
                    }
                }

                _collections = new List<FavoriteCollection>();
                _logger.Information("收藏夹文件不存在或为空，使用空数据: FilePath={FilePath}", _filePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载收藏夹数据失败: FilePath={FilePath}", _filePath);
                _collections = new List<FavoriteCollection>();
            }
        }

        /// <summary>
        /// 确保存储目录存在。
        /// </summary>
        private void EnsureDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.Debug("已创建收藏夹存储目录: {Directory}", directory);
            }
        }
    }
}