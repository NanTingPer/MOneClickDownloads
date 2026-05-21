using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Enums;

namespace MOneClickDownloads.DataModel.Version
{
    /// <summary>
    /// 版本依赖模型，表示一个模组版本对另一个模组/版本的依赖关系。
    /// 
    /// 用途：
    /// - 在 Version 模型的 dependencies 数组中使用
    /// - 递归下载依赖时，通过 project_id 查找依赖模组的兼容版本并下载
    /// - 通过 dependency_type 判断是必需依赖（自动下载）还是可选依赖（用户选择）
    /// 
    /// 数据流转：
    /// - 从 Get a version API 响应的 dependencies[] 数组反序列化
    /// - 若 version_id 有值，直接使用该版本；若为 null，需通过 project_id + game_versions + loaders 查询兼容版本
    /// - 仅 dependency_type == Required 的依赖需要自动递归下载
    /// </summary>
    public class Dependency
    {
        /// <summary>
        /// 依赖的具体版本ID（base62编码）。
        /// 若有值，表示依赖某个特定版本，直接使用此ID调用 Get a version API 获取详情。
        /// 若为 null，表示依赖该模组的任意兼容版本，需通过 project_id 结合当前MC版本和加载器查询。
        /// </summary>
        [JsonPropertyName("version_id")]
        public string? VersionId { get; set; }

        /// <summary>
        /// 依赖的模组项目ID（base62编码）。
        /// 当 version_id 为 null 时，使用此 project_id 调用 List project's versions API
        /// 并用 game_versions 和 loaders 参数过滤出兼容版本。
        /// </summary>
        [JsonPropertyName("project_id")]
        public string? ProjectId { get; set; }

        /// <summary>
        /// 依赖的文件名（主要用于整合包中显示外部依赖的文件名）。
        /// </summary>
        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        /// <summary>
        /// 依赖类型：required（必需下载）、optional（可选）、incompatible（不兼容）、embedded（已内嵌）。
        /// 仅 Required 类型的依赖需要在下载主模组时自动递归下载。
        /// </summary>
        [JsonPropertyName("dependency_type")]
        public DependencyType DependencyType { get; set; }
    }
}