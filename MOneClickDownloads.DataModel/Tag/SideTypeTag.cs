using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Tag
{
    /// <summary>
    /// 端侧类型标签模型，表示 Modrinth 上可用的端侧类型。
    /// 
    /// 用途：
    /// - 作为 Get a list of side types API（GET /tag/side_type）的响应数组元素
    /// - 获取可用的端侧类型列表（required、optional、unsupported）
    /// - 对应 SideSupport 枚举中的值
    /// 
    /// 数据流转：
    /// - 从 Get a list of side types API 响应反序列化
    /// - 主要用于UI展示和配置，不直接参与下载逻辑
    /// </summary>
    public class SideTypeTag : IApiModel
    {
        /// <summary>
        /// 端侧类型的名称（如 "required"、"optional"、"unsupported"）
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 获取此模型对应的 API 端点路径。
        /// </summary>
        public string GetEndpoint() => "/tag/side_type";
    }
}