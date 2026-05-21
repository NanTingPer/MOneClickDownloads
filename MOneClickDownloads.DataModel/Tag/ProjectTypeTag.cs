using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Tag
{
    /// <summary>
    /// 项目类型标签模型，表示 Modrinth 支持的项目类型。
    /// 
    /// 用途：
    /// - 作为 Get a list of project types API（GET /tag/project_type）的响应数组元素
    /// - 获取可用的项目类型列表（mod、modpack、resourcepack、shader）
    /// - 在搜索 facet 中使用 project_type:{name} 过滤
    /// 
    /// 数据流转：
    /// - 从 Get a list of project types API 响应反序列化
    /// - 对应 ProjectType 枚举中的值
    /// </summary>
    public class ProjectTypeTag : IApiModel
    {
        /// <summary>
        /// 项目类型的名称（如 "mod"、"modpack"、"resourcepack"、"shader"）
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 获取此模型对应的 API 端点路径。
        /// </summary>
        public string GetEndpoint() => "/tag/project_type";
    }
}