using System.IO.Compression;
using MOneClickDownloads.DataModel.Mod;
using MOneClickDownloads.Service.Analyzers;
using Serilog;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 模组文件分析服务，从模组 JAR 包中自动检测加载器类型并提取基本信息。<br />
    /// <br />
    /// 职责：<br />
    /// - 打开 JAR 包（ZIP 格式）并依次尝试各加载器分析器<br />
    /// - 支持 Fabric、Quilt、Forge、NeoForge 四种加载器格式<br />
    /// - 自动检测：不需要预先知道模组的加载器类型<br />
    /// <br />
    /// 设计模式：策略模式<br />
    /// 内部维护一组 IModFileAnalyzer 实例，按优先级依次尝试：<br />
    /// 1. NeoForge（META-INF/neoforge.mods.toml）<br />
    /// 2. Forge（META-INF/mods.toml）<br />
    /// 3. Legacy Forge（mcmod.info，1.12.2 及更早版本）<br />
    /// 4. Fabric/Quilt（fabric.mod.json）<br />
    /// <br />
    /// 使用示例：<br />
    /// <code>
    /// var service = new ModAnalysisService();
    /// var result = await service.AnalyzeAsync(@"C:\mods\fabric-api-0.147.0.jar");
    /// if (result != null)
    /// {
    ///     Console.WriteLine($"加载器: {result.LoaderType}");
    ///     Console.WriteLine($"模组ID: {result.ModId}");
    ///     Console.WriteLine($"名称: {result.Name}");
    ///     Console.WriteLine($"版本: {result.Version}");
    /// }
    /// </code>
    /// </summary>
    public class ModAnalysisService : IModAnalysisService
    {
        private readonly ILogger _logger;
        private readonly List<IModFileAnalyzer> _analyzers;

        /// <summary>
        /// 构造模组分析服务。<br />
        /// <br />
        /// 初始化所有内置分析器并按检测优先级排列：<br />
        /// NeoForge → Forge → Legacy Forge → Fabric/Quilt<br />
        /// <br />
        /// 优先级说明：<br />
        /// - NeoForge 优先于 Forge：因为 NeoForge 的 neoforge.mods.toml 路径更具体<br />
        /// - Forge 优先于 Legacy Forge：因为 mods.toml 是 1.13+ 的新格式，优先匹配<br />
        /// - Legacy Forge 优先于 Fabric：因为 mcmod.info 路径更具体<br />
        /// - Fabric/Quilt 最后：作为兜底检测<br />
        /// - 各分析器通过检测不同特征文件来判断适用性，互不冲突
        /// </summary>
        public ModAnalysisService()
        {
            _logger = Log.ForContext<ModAnalysisService>();

            // 按优先级注册分析器
            _analyzers = new List<IModFileAnalyzer>
            {
                new NeoForgeModAnalyzer(),
                new ForgeModAnalyzer(),
                new LegacyForgeModAnalyzer(),
                new FabricModAnalyzer()
            };

            _logger.Information("ModAnalysisService 已初始化，注册 {Count} 个分析器", _analyzers.Count);
        }

        /// <summary>
        /// 分析指定 JAR 文件，提取模组基本信息。<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 校验文件是否存在<br />
        /// 2. 以只读模式打开 JAR 包（ZIP 归档）<br />
        /// 3. 按优先级依次调用各分析器的 Analyze 方法<br />
        /// 4. 首个返回非 null 结果的分析器即为匹配的加载器类型<br />
        /// 5. 所有分析器均返回 null 则该文件不是已知格式的模组<br />
        /// </summary>
        /// <param name="jarPath">JAR 文件的本地路径</param>
        /// <returns>分析结果；若无法识别模组格式则返回 null</returns>
        /// <exception cref="FileNotFoundException">JAR 文件不存在时抛出</exception>
        /// <exception cref="InvalidDataException">JAR 文件损坏无法打开时抛出</exception>
        public async Task<ModAnalysisResult?> AnalyzeAsync(string jarPath)
        {
            _logger.Information("开始分析模组文件: {JarPath}", jarPath);

            if (!File.Exists(jarPath))
            {
                _logger.Error("模组文件不存在: {JarPath}", jarPath);
                throw new FileNotFoundException($"模组文件不存在: {jarPath}", jarPath);
            }

            return await Task.Run(() => AnalyzeInternal(jarPath));
        }

        /// <summary>
        /// 内部分析逻辑，在后台线程中执行文件 I/O 操作。
        /// </summary>
        /// <param name="jarPath">JAR 文件路径</param>
        /// <returns>分析结果或 null</returns>
        private ModAnalysisResult? AnalyzeInternal(string jarPath)
        {
            try
            {
                using var archive = ZipFile.Open(jarPath, ZipArchiveMode.Read, System.Text.Encoding.UTF8);

                foreach (var analyzer in _analyzers)
                {
                    try
                    {
                        var result = analyzer.Analyze(archive);
                        if (result != null)
                        {
                            _logger.Information("模组分析成功: {JarPath}, LoaderType={LoaderType}, ModId={ModId}",
                                jarPath, result.LoaderType, result.ModId);
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "分析器 {AnalyzerType} 处理 {JarPath} 时发生异常，跳过",
                            analyzer.GetType().Name, jarPath);
                    }
                }

                _logger.Warning("无法识别模组格式: {JarPath}（所有分析器均未匹配）", jarPath);
                return null;
            }
            catch (InvalidDataException ex)
            {
                _logger.Error(ex, "JAR 文件损坏无法打开: {JarPath}", jarPath);
                throw new InvalidDataException($"JAR 文件损坏无法打开: {jarPath}", ex);
            }
        }
    }
}