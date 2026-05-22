using MOneClickDownloads.DataModel.Mod;
using Serilog;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 本地模组清单工具，扫描指定文件夹中的所有 JAR 文件并构建已安装模组的索引。<br />
    /// <br />
    /// 职责：<br />
    /// - 扫描指定文件夹内的所有 .jar 文件<br />
    /// - 使用 IModAnalysisService 分析每个 JAR 包的模组元数据<br />
    /// - 提供按模组 ID 查询已安装模组的能力<br />
    /// - 提供已安装模组文件路径查询能力<br />
    /// <br />
    /// 设计特点：<br />
    /// - 以文件夹路径作为构造参数，符合"文件夹即配置"的直觉<br />
    /// - 内部委托 IModAnalysisService 进行单文件分析，复用现有分析能力<br />
    /// - ScanAsync() 执行一次快照式扫描，后续查询均为内存操作<br />
    /// - 线程安全：扫描完成后实例只读，可安全并发查询<br />
    /// <br />
    /// 使用示例：<br />
    /// <code>
    /// var analysisService = new ModAnalysisService();
    /// var inventory = new LocalModInventory(@"C:\mods", analysisService);
    /// await inventory.ScanAsync();
    /// 
    /// // 查询已安装模组
    /// var existing = inventory.FindByModId("fabric-api");
    /// if (existing != null)
    ///     Console.WriteLine($"已安装: {existing.Name} v{existing.Version}");
    /// 
    /// // 获取文件路径
    /// var filePath = inventory.GetModFilePath("fabric-api");
    /// </code>
    /// </summary>
    public class LocalModInventory
    {
        private readonly string _folderPath;
        private readonly IModAnalysisService _analysisService;
        private readonly ILogger _logger;
        private readonly Dictionary<string, ModAnalysisResult> _modIndex = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _filePathIndex = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 扫描完成后的已安装模组列表。
        /// </summary>
        public IReadOnlyList<ModAnalysisResult> InstalledMods => _modIndex.Values.ToList().AsReadOnly();

        /// <summary>
        /// 已安装模组数量。
        /// </summary>
        public int Count => _modIndex.Count;

        /// <summary>
        /// 构造本地模组清单工具。<br />
        /// <br />
        /// 参数：<br />
        /// - folderPath：要扫描的模组文件夹路径<br />
        /// - analysisService：用于分析 JAR 文件的模组分析服务实例<br />
        /// <br />
        /// 注意：构造后需调用 <see cref="ScanAsync"/> 才能开始扫描。
        /// </summary>
        /// <param name="folderPath">要扫描的模组文件夹路径</param>
        /// <param name="analysisService">模组文件分析服务</param>
        /// <exception cref="ArgumentNullException">参数为 null 时抛出</exception>
        public LocalModInventory(string folderPath, IModAnalysisService analysisService)
        {
            _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
            _logger = Log.ForContext<LocalModInventory>();
        }

        /// <summary>
        /// 扫描文件夹内所有 .jar 文件，构建本地模组清单。<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 枚举 _folderPath 下所有 *.jar 文件<br />
        /// 2. 逐个调用 IModAnalysisService.AnalyzeAsync 分析<br />
        /// 3. 分析成功的模组按 ModId 索引到内存字典<br />
        /// 4. 分析失败的文件记录警告日志并跳过<br />
        /// <br />
        /// 注意：<br />
        /// - 如果文件夹不存在，记录警告并返回空清单（不抛异常）<br />
        /// - 每个 JAR 文件的分析异常独立处理，不影响其他文件<br />
        /// - 同一 ModId 出现多个文件时，保留最后扫描到的版本
        /// </summary>
        public async Task ScanAsync()
        {
            _modIndex.Clear();
            _filePathIndex.Clear();

            if (!Directory.Exists(_folderPath))
            {
                _logger.Warning("扫描目录不存在: {FolderPath}，将返回空清单", _folderPath);
                return;
            }

            var jarFiles = Directory.GetFiles(_folderPath, "*.jar", SearchOption.TopDirectoryOnly);
            _logger.Information("开始扫描目录: {FolderPath}，发现 {Count} 个 JAR 文件", _folderPath, jarFiles.Length);

            foreach (var jarPath in jarFiles)
            {
                try
                {
                    var result = await _analysisService.AnalyzeAsync(jarPath);
                    if (result != null)
                    {
                        // 同一 ModId 的多个文件保留最后扫描到的
                        _modIndex[result.ModId] = result;
                        _filePathIndex[result.ModId] = jarPath;
                        _logger.Debug("扫描到模组: {ModId} ({Name} v{Version}) - {JarPath}",
                            result.ModId, result.Name, result.Version, jarPath);
                    }
                    else
                    {
                        _logger.Debug("无法识别的模组文件（跳过）: {JarPath}", jarPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "分析 JAR 文件失败（跳过）: {JarPath}", jarPath);
                }
            }

            _logger.Information("扫描完成: {FolderPath}，共识别 {Count} 个模组", _folderPath, _modIndex.Count);
        }

        /// <summary>
        /// 按模组 ID 查找已安装的模组。<br />
        /// <br />
        /// 查找逻辑：<br />
        /// - 按 ModId 精确匹配（忽略大小写）<br />
        /// - 未找到返回 null<br />
        /// </summary>
        /// <param name="modId">模组 ID</param>
        /// <returns>匹配的模组分析结果，或 null</returns>
        public ModAnalysisResult? FindByModId(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return null;

            _modIndex.TryGetValue(modId, out var result);
            return result;
        }

        /// <summary>
        /// 获取已安装模组文件的完整路径。<br />
        /// <br />
        /// 使用场景：<br />
        /// - 替换旧模组时需要知道旧文件路径以删除<br />
        /// </summary>
        /// <param name="modId">模组 ID</param>
        /// <returns>文件完整路径，或 null（未找到）</returns>
        public string? GetModFilePath(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return null;

            _filePathIndex.TryGetValue(modId, out var path);
            return path;
        }

        /// <summary>
        /// 按文件名查找已安装的模组。<br />
        /// <br />
        /// 查找逻辑：<br />
        /// - 遍历已索引的文件路径，用 <code>Path.GetFileName</code> 与传入的文件名比较（忽略大小写）<br />
        /// - 未找到返回 null<br />
        /// <br />
        /// 使用场景：<br />
        /// - 下载冲突检测：用待下载的文件名查找本地是否已有同名模组文件
        /// </summary>
        /// <param name="fileName">文件名（如 "fabric-api-0.145.2+26w14a.jar"）</param>
        /// <returns>匹配的模组分析结果，或 null</returns>
        public ModAnalysisResult? FindByFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            foreach (var kvp in _filePathIndex)
            {
                if (string.Equals(Path.GetFileName(kvp.Value), fileName, StringComparison.OrdinalIgnoreCase))
                {
                    _modIndex.TryGetValue(kvp.Key, out var result);
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// 按文件名查找已安装模组文件的完整路径。<br />
        /// <br />
        /// 使用场景：<br />
        /// - 替换旧模组时需要知道旧文件路径以删除
        /// </summary>
        /// <param name="fileName">文件名（如 "fabric-api-0.145.2+26w14a.jar"）</param>
        /// <returns>文件完整路径，或 null（未找到）</returns>
        public string? GetFilePathByFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            foreach (var kvp in _filePathIndex)
            {
                if (string.Equals(Path.GetFileName(kvp.Value), fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// 检查指定模组 ID 是否已安装。
        /// </summary>
        /// <param name="modId">模组 ID</param>
        /// <returns>是否已安装</returns>
        public bool ContainsMod(string modId)
        {
            return !string.IsNullOrEmpty(modId) && _modIndex.ContainsKey(modId);
        }
    }
}