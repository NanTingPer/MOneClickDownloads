using System.IO.Compression;
using MOneClickDownloads.DataModel.Enums;
using MOneClickDownloads.DataModel.Mod;
using Serilog;
using Tomlyn;
using Tomlyn.Model;

namespace MOneClickDownloads.Service.Analyzers
{
    /// <summary>
    /// NeoForge 模组文件分析器，解析 JAR 包中 META-INF/neoforge.mods.toml。<br />
    /// <br />
    /// 职责：<br />
    /// - 检测 JAR 包中是否存在 META-INF/neoforge.mods.toml 文件<br />
    /// - 解析 TOML 内容，提取 modId、displayName、version 字段<br />
    /// <br />
    /// 数据流：<br />
    /// 1. 在 JAR 包中查找 META-INF/neoforge.mods.toml<br />
    /// 2. 不存在 → 返回 null<br />
    /// 3. 存在 → 读取 TOML 内容<br />
    /// 4. 解析 [[mods]] 表格数组，提取第一个 mod 的信息<br />
    /// 5. 返回 ModAnalysisResult，LoaderType 设为 NeoForge<br />
    /// </summary>
    internal class NeoForgeModAnalyzer : IModFileAnalyzer
    {
        private const string NeoForgeModsTomlPath = "META-INF/neoforge.mods.toml";

        private static readonly ILogger Logger = Log.ForContext<NeoForgeModAnalyzer>();

        /// <summary>
        /// 尝试解析 META-INF/neoforge.mods.toml。<br />
        /// <br />
        /// NeoForge 模组 JAR 包的 META-INF 目录下会包含 neoforge.mods.toml 文件，<br />
        /// 格式与 Forge 的 mods.toml 相同：<br />
        /// <code>
        /// [[mods]]
        /// modId = "xaerominimap"
        /// version = "25.3.14"
        /// displayName = "Xaero's Minimap"
        /// </code>
        /// </summary>
        /// <param name="archive">已打开的 JAR 包 ZIP 归档</param>
        /// <returns>分析结果；若不存在 neoforge.mods.toml 则返回 null</returns>
        public ModAnalysisResult? Analyze(ZipArchive archive)
        {
            var entry = archive.GetEntry(NeoForgeModsTomlPath);
            if (entry == null)
            {
                return null;
            }

            Logger.Debug("检测到 {FilePath}，开始解析 NeoForge 模组信息", NeoForgeModsTomlPath);

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var tomlContent = reader.ReadToEnd();

            // NeoForge 的 mods.toml 格式与 Forge 相同，复用解析逻辑
            var result = ForgeModAnalyzer.ParseTomlContent(tomlContent);
            if (result != null)
            {
                result.LoaderType = ModLoaderType.NeoForge;
                Logger.Information("NeoForge 模组分析完成: ModId={ModId}, Name={Name}, Version={Version}",
                    result.ModId, result.Name, result.Version);
            }

            return result;
        }
    }
}