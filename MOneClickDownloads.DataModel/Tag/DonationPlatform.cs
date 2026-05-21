using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Tag
{
    /// <summary>
    /// 捐赠平台模型，表示 Modrinth 支持的捐赠平台（如 Patreon、Ko-fi 等）。
    /// 
    /// 用途：
    /// - 作为 Get a list of donation platforms API（GET /tag/donation_platform）的响应数组元素
    /// - 获取可用的捐赠平台列表
    /// - 平台名称对应 DonationUrl 模型中的 platform 字段
    /// 
    /// 数据流转：
    /// - 从 Get a list of donation platforms API 响应反序列化
    /// - 主要用于展示和配置，不参与下载逻辑
    /// </summary>
    public class DonationPlatform : IApiModel
    {
        /// <summary>
        /// 捐赠平台的简短标识（如 "patreon"、"ko-fi"、"bmac"）
        /// </summary>
        [JsonPropertyName("short")]
        public string Short { get; set; } = string.Empty;

        /// <summary>
        /// 捐赠平台的完整名称（如 "Patreon"、"Ko-fi"、"Buy Me a Coffee"）
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 获取此模型对应的 API 端点路径。
        /// </summary>
        public string GetEndpoint() => "/tag/donation_platform";
    }
}