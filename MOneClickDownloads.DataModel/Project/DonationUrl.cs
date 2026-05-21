using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Project
{
    /// <summary>
    /// 捐赠链接模型，表示项目关联的一个捐赠平台链接。
    /// 
    /// 用途：
    /// - 在 Project 模型的 donation_urls 数组中使用
    /// - 展示项目页面上的捐赠按钮（如 Patreon、Ko-fi 等）
    /// 
    /// 数据流转：
    /// - 从 Get a project API 响应中反序列化
    /// - 仅用于展示，不参与下载逻辑
    /// </summary>
    public class DonationUrl
    {
        /// <summary>
        /// 捐赠平台的标识符（如 "patreon"、"ko-fi"、"bmac" 等）
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 捐赠平台的显示名称（如 "Patreon"、"Ko-fi" 等）
        /// </summary>
        [JsonPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;

        /// <summary>
        /// 捐赠平台的用户页面URL
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}