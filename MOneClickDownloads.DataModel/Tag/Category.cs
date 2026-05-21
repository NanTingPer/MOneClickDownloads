using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Tag
{
    /// <summary>
    /// 分类模型，表示 Modrinth 上的一个项目分类（如 "adventure"、"technology"、"optimization" 等）。
    /// 
    /// 用途：
    /// - 作为 Get a list of categories API（GET /tag/category）的响应数组元素
    /// - 获取可用的分类列表供搜索过滤和项目信息展示
    /// - 在搜索 facet 中使用 categories:{name} 过滤
    /// 
    /// 数据流转：
    /// - 从 Get a list of categories API 响应反序列化
    /// - 分类名称用于搜索请求的 facets 过滤
    /// </summary>
    public class Category : IApiModel
    {
        /// <summary>
        /// 此分类所属的项目类型（如 "mod"、"modpack"、"resourcepack"、"shader"）
        /// </summary>
        [JsonPropertyName("project_type")]
        public string ProjectType { get; set; } = string.Empty;

        /// <summary>
        /// 此分类所在分组的标题（如 "Categories"、"Performance & Optimization"）
        /// </summary>
        [JsonPropertyName("header")]
        public string Header { get; set; } = string.Empty;

        /// <summary>
        /// 分类的唯一标识名称（如 "adventure"、"technology"、"fabric"）。
        /// 加载器（如 fabric、forge）在搜索 API 中也归类为 categories。
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 分类图标的 URL 链接
        /// </summary>
        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// 获取此模型对应的 API 端点路径。
        /// </summary>
        public string GetEndpoint() => "/tag/category";
    }
}