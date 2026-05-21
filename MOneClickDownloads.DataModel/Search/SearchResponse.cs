using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Search
{
    /// <summary>
    /// 搜索响应模型，包装 Search projects API（GET /search）的完整响应。
    /// 
    /// 用途：
    /// - 作为 Search projects API 的响应数据模型
    /// - 包含搜索结果列表（hits）、分页信息（offset/limit/total_hits）
    /// - 用于在UI中展示搜索结果并实现分页加载
    /// 
    /// 数据流转：
    /// - 从 Search projects API 响应反序列化
    /// - 其 Hits 列表中每个 ProjectHit 包含 project_id，用于后续调用版本API
    /// - offset + limit 用于请求下一页搜索结果
    /// </summary>
    public class SearchResponse : IApiModel
    {
        /// <summary>
        /// 搜索结果列表，每个元素为一个匹配的项目（精简版项目信息）
        /// </summary>
        [JsonPropertyName("hits")]
        public List<ProjectHit> Hits { get; set; } = new List<ProjectHit>();

        /// <summary>
        /// 本次查询跳过的结果数量（用于分页偏移量）
        /// </summary>
        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        /// <summary>
        /// 本次查询返回的结果数量
        /// </summary>
        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        /// <summary>
        /// 符合查询条件的总结果数，用于计算总页数
        /// </summary>
        [JsonPropertyName("total_hits")]
        public int TotalHits { get; set; }

        /// <summary>
        /// 获取此模型对应的 API 端点路径。
        /// 查询参数包括 query、facets、index、offset、limit。
        /// </summary>
        public string GetEndpoint() => "/search";
    }
}