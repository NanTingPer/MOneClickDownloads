using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Tag
{
    /// <summary>
    /// 加载器模型，表示一个模组加载器（如 Fabric、Forge、Quilt 等）。
    /// 
    /// 用途：
    /// - 作为 Get a list of loaders API（GET /tag/loader）的响应数组元素
    /// - 获取可用的加载器列表供用户选择
    /// - 加载器的 name 值用于过滤模组版本（如在 List project's versions 的 loaders 查询参数）
    /// - 在搜索 facet 中使用 categories:{loader_name} 过滤
    /// 
    /// 数据流转：
    /// - 从 Get a list of loaders API 响应反序列化
    /// - 用户选择加载器后，将 name 值作为 loaders 参数传入后续 API 调用
    /// </summary>
    public class Loader : IApiModel
    {
        /// <summary>
        /// 加载器的唯一标识名称（如 "fabric"、"forge"、"quilt"、"neoforge"、"minecraft"）。
        /// 
        /// 关键用途：
        /// - 作为 List project's versions API 的 loaders 查询参数
        /// - 在搜索 facets 中使用 categories:{name} 过滤
        /// - 在构造带参下载URL时的 mr_loader 参数
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 加载器支持的游戏版本列表（如 ["1.16.5", "1.17.1", ...]）
        /// </summary>
        [JsonPropertyName("supported_project_types")]
        public List<string> SupportedProjectTypes { get; set; } = new List<string>();

        /// <summary>
        /// 获取此模型对应的 API 端点路径。
        /// </summary>
        public string GetEndpoint() => "/tag/loader";
    }
}