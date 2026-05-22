using System.IO.Compression;
using MOneClickDownloads.DataModel.Mod;

namespace MOneClickDownloads.Service.Analyzers
{
    /// <summary>
    /// 模组文件分析器接口，定义每种加载器类型的 JAR 包解析策略。<br />
    /// <br />
    /// 职责：<br />
    /// - 判断当前分析器是否能处理该 JAR 包（通过检测特征文件是否存在）<br />
    /// - 从 JAR 包中提取模组元数据并返回 ModAnalysisResult<br />
    /// <br />
    /// 设计模式：策略模式<br />
    /// 每种加载器类型（Fabric、Forge、NeoForge）实现此接口，<br />
    /// 由 ModAnalysisService 按优先级依次尝试各分析器。<br />
    /// <br />
    /// 实现类：<br />
    /// - FabricModAnalyzer：解析 fabric.mod.json（同时覆盖 Quilt）<br />
    /// - ForgeModAnalyzer：解析 META-INF/mods.toml<br />
    /// - NeoForgeModAnalyzer：解析 META-INF/neoforge.mods.toml<br />
    /// - LegacyForgeModAnalyzer：解析 mcmod.info（1.12.2 及更早版本）<br />
    /// </summary>
    internal interface IModFileAnalyzer
    {
        /// <summary>
        /// 尝试分析 JAR 包中的模组元数据。<br />
        /// <br />
        /// 实现要求：<br />
        /// 1. 首先检查 JAR 包中是否存在对应的特征文件<br />
        /// 2. 若特征文件不存在，返回 null（表示此分析器不适用）<br />
        /// 3. 若特征文件存在，读取并解析，返回填充好的 ModAnalysisResult<br />
        /// 4. 解析失败时应抛出异常（由调用方处理）<br />
        /// </summary>
        /// <param name="archive">已打开的 ZIP 归档（JAR 包）</param>
        /// <returns>分析结果，若此分析器不适用则返回 null</returns>
        ModAnalysisResult? Analyze(ZipArchive archive);
    }
}