using System.IO.Compression;
using MOneClickDownloads.DataModel.Enums;
using MOneClickDownloads.DataModel.Mod;
using Serilog;
using Tomlyn;
using Tomlyn.Model;

namespace MOneClickDownloads.Service.Analyzers
{
    /// <summary>
    /// Forge 模组文件分析器，解析 JAR 包中 META-INF/mods.toml。<br />
    /// <br />
    /// 职责：<br />
    /// - 检测 JAR 包中是否存在 META-INF/mods.toml 文件<br />
    /// - 解析 TOML 内容，提取 modId、displayName、version 字段<br />
    /// <br />
    /// 数据流：<br />
    /// 1. 在 JAR 包中查找 META-INF/mods.toml<br />
    /// 2. 不存在 → 返回 null<br />
    /// 3. 存在 → 读取 TOML 内容<br />
    /// 4. 解析 [[mods]] 表格数组，提取第一个 mod 的信息<br />
    /// 5. 返回 ModAnalysisResult，LoaderType 设为 Forge<br />
    /// </summary>
    internal class ForgeModAnalyzer : IModFileAnalyzer
    {
        private const string ModsTomlPath = "META-INF/mods.toml";

        private static readonly ILogger Logger = Log.ForContext<ForgeModAnalyzer>();

        /// <summary>
        /// 尝试解析 META-INF/mods.toml。<br />
        /// <br />
        /// Forge 模组 JAR 包的 META-INF 目录下会包含 mods.toml 文件，<br />
        /// 格式示例：<br />
        /// <code>
        /// [[mods]]
        /// modId = "xaerominimap"
        /// version = "25.3.12"
        /// displayName = "Xaero's Minimap"
        /// </code>
        /// </summary>
        /// <param name="archive">已打开的 JAR 包 ZIP 归档</param>
        /// <returns>分析结果；若不存在 mods.toml 则返回 null</returns>
        public ModAnalysisResult? Analyze(ZipArchive archive)
        {
            var entry = archive.GetEntry(ModsTomlPath);
            if (entry == null)
            {
                return null;
            }

            Logger.Debug("检测到 {FilePath}，开始解析 Forge 模组信息", ModsTomlPath);

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var tomlContent = reader.ReadToEnd();

            var result = ParseTomlContent(tomlContent);
            if (result != null)
            {
                result.LoaderType = ModLoaderType.Forge;
                Logger.Information("Forge 模组分析完成: ModId={ModId}, Name={Name}, Version={Version}",
                    result.ModId, result.Name, result.Version);
            }

            return result;
        }

        /// <summary>
        /// 解析 TOML 内容，提取 [[mods]] 数组中第一个条目的信息。
        /// </summary>
        /// <param name="tomlContent">TOML 文本内容</param>
        /// <returns>解析结果；若无法解析则返回 null</returns>
        internal static ModAnalysisResult? ParseTomlContent(string tomlContent)
        {
            var document = Toml.Parse(tomlContent);
            if (document.HasErrors)
            {
                Logger.Warning("TOML 解析失败: {Errors}", document.Diagnostics);
                return null;
            }

            var model = document.ToModel();

            // mods.toml 使用 [[mods]] 数组语法
            if (model.TryGetValue("mods", out var modsObj) && modsObj is TomlArray modsArray && modsArray.Count > 0)
            {
                var firstMod = modsArray[0];
                if (firstMod is TomlTable modTable)
                {
                    var modId = GetStringField(modTable, "modId");
                    var displayName = GetStringField(modTable, "displayName");
                    var version = GetStringField(modTable, "version");

                    if (string.IsNullOrEmpty(modId))
                    {
                        Logger.Warning("mods.toml 中 [[mods]] 缺少 modId 字段");
                        return null;
                    }

                    return new ModAnalysisResult
                    {
                        ModId = modId,
                        Name = displayName ?? string.Empty,
                        Version = version ?? string.Empty
                    };
                }
            }

            Logger.Warning("mods.toml 中未找到 [[mods]] 数组或数组为空");
            return null;
        }

        /// <summary>
        /// 安全获取 TOML 表格中的字符串字段值。
        /// </summary>
        private static string? GetStringField(TomlTable table, string key)
        {
            if (table.TryGetValue(key, out var value) && value is string str)
            {
                return str;
            }
            return null;
        }
    }
}