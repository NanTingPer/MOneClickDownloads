using System;
using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Project
{
    /// <summary>
    /// 画廊图片模型，表示项目画廊中的一张截图或展示图片。
    /// 
    /// 用途：
    /// - 在 Project 模型的 gallery 数组中使用
    /// - 展示项目详情页的图片画廊
    /// 
    /// 数据流转：
    /// - 从 Get a project API 响应中反序列化
    /// - 仅用于展示，不参与下载逻辑
    /// </summary>
    public class GalleryImage
    {
        /// <summary>
        /// 图片的CDN链接URL
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 是否为画廊中的推荐图片
        /// </summary>
        [JsonPropertyName("featured")]
        public bool Featured { get; set; }

        /// <summary>
        /// 图片标题，可能为null
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// 图片描述，可能为null
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 图片创建时间（ISO-8601格式）
        /// </summary>
        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        /// <summary>
        /// 图片排序顺序，值越小越靠前
        /// </summary>
        [JsonPropertyName("ordering")]
        public int Ordering { get; set; }
    }
}