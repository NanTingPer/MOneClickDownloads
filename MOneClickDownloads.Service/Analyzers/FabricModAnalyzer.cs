using System.IO.Compression;
using System.Text.Json;
using MOneClickDownloads.DataModel.Enums;
using MOneClickDownloads.DataModel.Mod;
using Serilog;

namespace MOneClickDownloads.Service.Analyzers
{
    /// <summary>
    /// Fabric/Quilt 模组文件分析器，解析 JAR 包中的 fabric.mod.json。<br />
    /// <br />
    /// 职责：<br />
    /// - 检测 JAR 包中是否存在 fabric.mod.json 文件<br />
    /// - 解析 JSON 内容，提取 id、name、version 字段<br />
    /// - Fabric 和 Quilt 使用相同的 fabric.mod.json 格式，故共用此分析器<br />
    /// <br />
    /// 数据流：<br />
    /// 1. 在 JAR 包根目录查找 fabric.mod.json<br />
    /// 2. 不存在 → 返回 null<br />
    /// 3. 存在 → 读取 JSON 内容<br />
    /// 4. 反序列化提取 id、name、version<br />
    /// 5. 返回 ModAnalysisResult，LoaderType 设为 Fabric（Quilt 同格式，由外部根据其他信息区分）<br />
    /// </summary>
    internal class FabricModAnalyzer : IModFileAnalyzer
    {
        private const string FabricModJsonPath = "fabric.mod.json";

        private static readonly ILogger Logger = Log.ForContext<FabricModAnalyzer>();

        /// <summary>
        /// 尝试解析 fabric.mod.json。<br />
        /// <br />
        /// Fabric 模组 JAR 包的根目录下会包含 fabric.mod.json 文件，<br />
        /// 格式示例：<br />
        /// <code>
        /// {
        ///   "id": "fabric-api",
        ///   "name": "Fabric API",
        ///   "version": "0.147.0+26.2"
        /// }
        /// </code>
        /// </summary>
        /// <param name="archive">已打开的 JAR 包 ZIP 归档</param>
        /// <returns>分析结果；若不存在 fabric.mod.json 则返回 null</returns>
        public ModAnalysisResult? Analyze(ZipArchive archive)
        {
            var entry = archive.GetEntry(FabricModJsonPath);
            if (entry == null)
            {
                return null;
            }

            Logger.Debug("检测到 {FilePath}，开始解析 Fabric/Quilt 模组信息", FabricModJsonPath);

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var modId = GetStringProperty(root, "id");
            var name = GetStringProperty(root, "name");
            var version = GetStringProperty(root, "version");

            if (string.IsNullOrEmpty(modId))
            {
                Logger.Warning("fabric.mod.json 中缺少 id 字段");
                return null;
            }

            Logger.Information("Fabric/Quilt 模组分析完成: ModId={ModId}, Name={Name}, Version={Version}",
                modId, name, version);

            return new ModAnalysisResult
            {
                ModId = modId,
                Name = name ?? string.Empty,
                Version = version ?? string.Empty,
                LoaderType = ModLoaderType.Fabric
            };
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