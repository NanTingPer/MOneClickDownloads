using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Tag
{
    /// <summary>
    /// 许可证标签模型，表示 Modrinth 上可用的开源许可证信息。
    /// 
    /// 用途：
    /// - 作为 Get a list of licenses API（GET /tag/license）的响应数组元素
    /// - 获取可用的许可证列表供搜索过滤和项目展示
    /// - 在搜索 facet 中使用 license:{id} 过滤
    /// 
    /// 数据流转：
    /// - 从 Get a list of licenses API 响应反序列化
    /// - LicenseId 字段用于搜索请求的 facets 过滤
    /// </summary>
    public class LicenseTag : IApiModel
    {
        /// <summary>
        /// SPDX 许可证标识符（如 "MIT"、"LGPL-3.0-or-later"、"Apache-2.0"）。
        /// 
        /// 关键用途：在搜索 facet 中使用 license:{id} 过滤。
        /// </summary>
        [JsonPropertyName("short")]
        public string LicenseId { get; set; } = string.Empty;

        /// <summary>
        /// 许可证的完整名称（如 "MIT License"、"GNU Lesser General Public License v3 or later"）
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 获取此模型对应的 API 端点路径。
        /// </summary>
        public string GetEndpoint() => "/tag/license";
    }
}