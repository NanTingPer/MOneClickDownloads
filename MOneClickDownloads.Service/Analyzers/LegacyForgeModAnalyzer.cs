using System.IO.Compression;
using System.Text.Json;
using MOneClickDownloads.DataModel.Enums;
using MOneClickDownloads.DataModel.Mod;
using Serilog;

namespace MOneClickDownloads.Service.Analyzers
{
    /// <summary>
    /// 老版本 Forge 模组文件分析器，解析 JAR 包根目录中的 mcmod.info。<br />
    /// <br />
    /// 职责：<br />
    /// - 检测 JAR 包中是否存在 mcmod.info 文件（老版本 Forge 特征文件）<br />
    /// - 解析 JSON 内容，提取 modid、name、version 字段<br />
    /// <br />
    /// 适用版本：<br />
    /// - Minecraft 1.12.2 及更早版本的 Forge 模组<br />
    /// - 1.13+ 版本的 Forge 改用 META-INF/mods.toml 格式<br />
    /// <br />
    /// 数据流：<br />
    /// 1. 在 JAR 包根目录查找 mcmod.info<br />
    /// 2. 不存在 → 返回 null<br />
    /// 3. 存在 → 读取 JSON 内容<br />
    /// 4. 解析 JSON 数组，提取第一个元素的 modid、name、version<br />
    /// 5. 返回 ModAnalysisResult，LoaderType 设为 Forge<br />
    /// </summary>
    internal class LegacyForgeModAnalyzer : IModFileAnalyzer
    {
        private const string McModInfoPath = "mcmod.info";

        private static readonly ILogger Logger = Log.ForContext<LegacyForgeModAnalyzer>();

        /// <summary>
        /// 尝试解析 mcmod.info。<br />
        /// <br />
        /// 老版本 Forge 模组 JAR 包的根目录下会包含 mcmod.info 文件，<br />
        /// 格式示例：<br />
        /// <code>
        /// [
        ///   {
        ///     "modid": "xaerominimap",
        ///     "name": "Xaero's Minimap",
        ///     "version": "25.3.13",
        ///     "mcversion": "1.12.2",
        ///     "description": "The most vanilla-looking minimap for Minecraft.",
        ///     "authorList": ["Xaero96"],
        ///     "dependencies": []
        ///   }
        /// ]
        /// </code>
        /// </summary>
        /// <param name="archive">已打开的 JAR 包 ZIP 归档</param>
        /// <returns>分析结果；若不存在 mcmod.info 则返回 null</returns>
        public ModAnalysisResult? Analyze(ZipArchive archive)
        {
            var entry = archive.GetEntry(McModInfoPath);
            if (entry == null)
            {
                return null;
            }

            Logger.Debug("检测到 {FilePath}，开始解析老版本 Forge 模组信息", McModInfoPath);

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var result = ParseMcModInfo(json);
            if (result != null)
            {
                result.LoaderType = ModLoaderType.Forge;
                Logger.Information("老版本 Forge 模组分析完成: ModId={ModId}, Name={Name}, Version={Version}",
                    result.ModId, result.Name, result.Version);
            }

            return result;
        }

        /// <summary>
        /// 解析 mcmod.info JSON 内容，提取数组中第一个条目的信息。
        /// </summary>
        /// <param name="jsonContent">JSON 文本内容</param>
        /// <returns>解析结果；若无法解析则返回 null</returns>
        internal static ModAnalysisResult? ParseMcModInfo(string jsonContent)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                // mcmod.info 是一个 JSON 数组
                if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
                {
                    Logger.Warning("mcmod.info 不是有效的 JSON 数组或数组为空");
                    return null;
                }

                var firstMod = root[0];
                var modId = GetStringProperty(firstMod, "modid");
                var name = GetStringProperty(firstMod, "name");
                var version = GetStringProperty(firstMod, "version");

                if (string.IsNullOrEmpty(modId))
                {
                    Logger.Warning("mcmod.info 中缺少 modid 字段");
                    return null;
                }

                return new ModAnalysisResult
                {
                    ModId = modId,
                    Name = name ?? string.Empty,
                    Version = version ?? string.Empty
                };
            }
            catch (JsonException ex)
            {
                Logger.Warning(ex, "mcmod.info JSON 解析失败");
                return null;
            }
        }

        /// <summary>
        /// 安全获取 JSON 对象的字符串属性值。
        /// </summary>
        private static string? GetStringProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
            return null;
        }
    }
}