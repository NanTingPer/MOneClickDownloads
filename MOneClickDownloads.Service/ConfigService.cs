using System.Text.Json;
using Serilog;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 应用配置服务，提供基于 JSON 文件的配置持久化。<br />
    /// <br />
    /// 职责：<br />
    /// - 读取和写入应用配置项<br />
    /// - 配置项以 JSON 文件形式持久化到磁盘<br />
    /// <br />
    /// 设计模式：<br />
    /// - 构造时传入配置文件全路径<br />
    /// - 内部使用 Dictionary<string, JsonElement> 存储配置数据<br />
    /// - Set 操作自动触发 Save，写入 JSON 文件<br />
    /// - 文件不存在时自动创建空配置<br />
    /// <br />
    /// 使用示例：<br />
    /// <code>
    /// var configService = new ConfigService("configs/app.json");
    /// 
    /// // 读取配置
    /// var filter = configService.Get<string>("ActiveMcVersionFilter");
    /// 
    /// // 写入配置（自动保存）
    /// configService.Set("ActiveMcVersionFilter", "1.20.1");
    /// </code>
    /// </summary>
    public class ConfigService
    {
        private readonly string _configFilePath;
        private readonly ILogger _logger;
        private readonly object _lock = new();
        private Dictionary<string, JsonElement> _data = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// 构造配置服务。<br />
        /// <br />
        /// 数据流：<br />
        /// - 保存配置文件全路径<br />
        /// - 创建配置文件所在目录（如不存在）<br />
        /// - 尝试从文件加载已有配置；文件不存在则初始化为空配置<br />
        /// </summary>
        /// <param name="configFilePath">配置文件的全路径</param>
        public ConfigService(string configFilePath)
        {
            _configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));
            _logger = Log.ForContext<ConfigService>();

            EnsureDirectoryExists();
            Load();

            _logger.Information("ConfigService 已初始化: ConfigPath={ConfigPath}", _configFilePath);
        }

        /// <summary>
        /// 读取配置项。<br />
        /// <br />
        /// 数据流：<br />
        /// - 在内部字典中查找指定 key<br />
        /// - 找到则反序列化为目标类型 T 并返回<br />
        /// - 未找到返回 default(T)<br />
        /// </summary>
        /// <typeparam name="T">配置项的目标类型</typeparam>
        /// <param name="key">配置项键名</param>
        /// <returns>配置项值，未找到时返回 default</returns>
        public T? Get<T>(string key)
        {
            lock (_lock)
            {
                if (_data.TryGetValue(key, out var element))
                {
                    try
                    {
                        return element.Deserialize<T>();
                    }
                    catch (JsonException ex)
                    {
                        _logger.Warning(ex, "配置项反序列化失败: Key={Key}", key);
                        return default;
                    }
                }
                return default;
            }
        }

        /// <summary>
        /// 设置配置项并自动保存到文件。<br />
        /// <br />
        /// 数据流：<br />
        /// - 将值序列化为 JsonElement 存入内部字典<br />
        /// - 调用 Save() 将完整配置写入 JSON 文件<br />
        /// </summary>
        /// <typeparam name="T">配置项的类型</typeparam>
        /// <param name="key">配置项键名</param>
        /// <param name="value">配置项值</param>
        public void Set<T>(string key, T value)
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(value);
                _data[key] = JsonSerializer.Deserialize<JsonElement>(json);
                Save();
            }

            _logger.Debug("配置项已设置: Key={Key}", key);
        }

        /// <summary>
        /// 移除配置项并自动保存到文件。<br />
        /// </summary>
        /// <param name="key">配置项键名</param>
        /// <returns>是否成功移除（key 存在时返回 true）</returns>
        public bool Remove(string key)
        {
            lock (_lock)
            {
                var removed = _data.Remove(key);
                if (removed)
                {
                    Save();
                    _logger.Debug("配置项已移除: Key={Key}", key);
                }
                return removed;
            }
        }

        /// <summary>
        /// 将当前配置写入 JSON 文件。<br />
        /// <br />
        /// 数据流：<br />
        /// - 序列化内部字典为格式化 JSON 字符串<br />
        /// - 写入配置文件（覆盖写入）<br />
        /// </summary>
        private void Save()
        {
            try
            {
                EnsureDirectoryExists();
                var json = JsonSerializer.Serialize(_data, JsonOptions);
                File.WriteAllText(_configFilePath, json);
                _logger.Debug("配置已保存: ConfigPath={ConfigPath}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存配置文件失败: ConfigPath={ConfigPath}", _configFilePath);
            }
        }

        /// <summary>
        /// 从 JSON 文件加载配置。<br />
        /// <br />
        /// 数据流：<br />
        /// - 检查配置文件是否存在<br />
        /// - 存在则读取并反序列化为字典<br />
        /// - 不存在则初始化为空字典<br />
        /// </summary>
        private void Load()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        _data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                                ?? new Dictionary<string, JsonElement>();
                        _logger.Information("配置已加载: ConfigPath={ConfigPath}, 配置项数量={Count}", _configFilePath, _data.Count);
                        return;
                    }
                }

                _data = new Dictionary<string, JsonElement>();
                _logger.Information("配置文件不存在或为空，使用空配置: ConfigPath={ConfigPath}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载配置文件失败: ConfigPath={ConfigPath}", _configFilePath);
                _data = new Dictionary<string, JsonElement>();
            }
        }

        /// <summary>
        /// 确保配置文件所在目录存在。
        /// </summary>
        private void EnsureDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.Debug("已创建配置目录: {Directory}", directory);
            }
        }
    }
}