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
        /// 解析 TOML 内容，提取 [[mods]] 数组中第一个条目的信息。<br />
        /// <br />
        /// 支持的 mods.toml 格式变体：<br />
        /// - 标准格式：字段直接在 [[mods]] 下方，tab 缩进<br />
        /// - TOML 解析成功但模型结构异常时，通过 key 遍历回退查找 TomlArray<br />
        /// - 所有标准方法均失败时，使用正则表达式兜底提取 modId<br />
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

            TomlTable model;
            try
            {
                model = document.ToModel();
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "TOML 模型转换失败");
                return null;
            }

            Logger.Debug("TOML 模型包含 {Count} 个顶级键: {Keys}",
                model.Count, string.Join(", ", model.Keys));

            // 策略1：标准方式查找 [[mods]] 数组
            if (model.TryGetValue("mods", out var modsObj) && modsObj is TomlArray modsArray && modsArray.Count > 0)
            {
                var result = TryParseFirstMod(modsArray);
                if (result != null) return result;
            }

            // 策略2：遍历所有顶级键，查找第一个 TomlArray（回退策略）
            foreach (var kvp in model)
            {
                if (kvp.Value is TomlArray fallbackArray && fallbackArray.Count > 0)
                {
                    Logger.Debug("通过遍历发现数组键: {Key}, 元素数量: {Count}", kvp.Key, fallbackArray.Count);
                    var result = TryParseFirstMod(fallbackArray);
                    if (result != null) return result;
                }
            }

            // 策略3：正则表达式兜底提取（应对 TOML 解析结果异常的情况）
            Logger.Debug("TOML 模型方式未找到 [[mods]]，尝试正则表达式兜底提取");
            var regexResult = TryParseTomlWithRegex(tomlContent);
            if (regexResult != null) return regexResult;

            // 所有策略均失败，记录诊断信息
            Logger.Warning("mods.toml 中未找到 [[mods]] 数组或数组为空。TOML 内容前 500 字符: {Content}",
                tomlContent.Length > 500 ? tomlContent[..500] : tomlContent);
            return null;
        }

        /// <summary>
        /// 尝试从 TomlArray 中解析第一个 mod 的信息。
        /// </summary>
        private static ModAnalysisResult? TryParseFirstMod(TomlArray array)
        {
            var firstMod = array[0];
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

                Logger.Debug("成功解析 [[mods]]: modId={ModId}, displayName={DisplayName}, version={Version}",
                    modId, displayName ?? "(null)", version ?? "(null)");

                return new ModAnalysisResult
                {
                    ModId = modId,
                    Name = displayName ?? string.Empty,
                    Version = version ?? string.Empty
                };
            }
            return null;
        }

        /// <summary>
        /// 使用正则表达式从 TOML 原始文本中兜底提取 modId、displayName、version。<br />
        /// 仅在 TOML 解析模型异常时作为最后手段使用。
        /// </summary>
        private static ModAnalysisResult? TryParseTomlWithRegex(string tomlContent)
        {
            try
            {
                var modIdMatch = System.Text.RegularExpressions.Regex.Match(
                    tomlContent, @"^\s*modId\s*=\s*""([^""]+)""", System.Text.RegularExpressions.RegexOptions.Multiline);
                var displayNameMatch = System.Text.RegularExpressions.Regex.Match(
                    tomlContent, @"^\s*displayName\s*=\s*""([^""]+)""", System.Text.RegularExpressions.RegexOptions.Multiline);
                var versionMatch = System.Text.RegularExpressions.Regex.Match(
                    tomlContent, @"^\s*version\s*=\s*""([^""]+)""", System.Text.RegularExpressions.RegexOptions.Multiline);

                if (modIdMatch.Success)
                {
                    Logger.Debug("正则兜底成功提取: modId={ModId}, displayName={DisplayName}, version={Version}",
                        modIdMatch.Groups[1].Value,
                        displayNameMatch.Success ? displayNameMatch.Groups[1].Value : "(null)",
                        versionMatch.Success ? versionMatch.Groups[1].Value : "(null)");

                    return new ModAnalysisResult
                    {
                        ModId = modIdMatch.Groups[1].Value,
                        Name = displayNameMatch.Success ? displayNameMatch.Groups[1].Value : string.Empty,
                        Version = versionMatch.Success ? versionMatch.Groups[1].Value : string.Empty
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "正则表达式兜底解析失败");
            }
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