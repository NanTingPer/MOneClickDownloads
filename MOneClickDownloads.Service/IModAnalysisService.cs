using MOneClickDownloads.DataModel.Mod;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 模组文件分析服务接口，提供从 JAR 包中提取模组元数据的能力。<br />
    /// <br />
    /// 职责：<br />
    /// - 分析模组 JAR 包，自动检测加载器类型（Fabric/Quilt/Forge/NeoForge）<br />
    /// - 提取模组基本信息（ID、名称、版本）<br />
    /// <br />
    /// 使用示例：<br />
    /// <code>
    /// var result = await analysisService.AnalyzeAsync("path/to/mod.jar");
    /// if (result != null)
    /// {
    ///     Console.WriteLine($"模组: {result.Name} v{result.Version} ({result.LoaderType})");
    /// }
    /// </code>
    /// </summary>
    public interface IModAnalysisService
    {
        /// <summary>
        /// 分析指定 JAR 文件，提取模组基本信息。<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 打开 JAR 包（ZIP 归档）<br />
        /// 2. 按优先级依次尝试各分析器（NeoForge → Forge → Fabric）<br />
        /// 3. 首个成功解析的分析器结果作为最终结果<br />
        /// 4. 所有分析器均不适用时返回 null<br />
        /// </summary>
        /// <param name="jarPath">JAR 文件的本地路径</param>
        /// <returns>分析结果；若无法识别模组格式则返回 null</returns>
        /// <exception cref="FileNotFoundException">JAR 文件不存在时抛出</exception>
        /// <exception cref="InvalidDataException">JAR 文件损坏无法打开时抛出</exception>
        Task<ModAnalysisResult?> AnalyzeAsync(string jarPath);
    }
}