using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Project
{
    /// <summary>
    /// 许可证信息模型，表示项目的开源许可证详情。
    /// 
    /// 用途：
    /// - 在 Project 模型的 license 字段中使用
    /// - 展示项目页面的许可证信息
    /// - 在搜索请求的 facets 中可按 license:SPDX_ID 过滤
    /// 
    /// 数据流转：
    /// - 从 Get a project API 响应的 license 字段反序列化
    /// </summary>
    public class LicenseInfo
    {
        /// <summary>
        /// SPDX 许可证标识符（如 "MIT"、"LGPL-3.0-or-later"）
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 许可证的完整名称（如 "GNU Lesser General Public License v3 or later"）
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 许可证的URL链接，可能为null
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}