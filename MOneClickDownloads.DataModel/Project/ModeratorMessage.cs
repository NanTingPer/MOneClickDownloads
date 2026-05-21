using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Project
{
    /// <summary>
    /// 管理员消息模型，表示审核管理员发送给项目的消息。
    /// 
    /// 用途：
    /// - 在 Project 模型的 moderator_message 字段中使用
    /// - 展示审核反馈或要求修改的通知
    /// 
    /// 数据流转：
    /// - 从 Get a project API 响应的 moderator_message 字段反序列化
    /// - 仅在管理员发送过消息时存在
    /// </summary>
    public class ModeratorMessage
    {
        /// <summary>
        /// 管理员的消息标题/摘要
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 管理员消息的详细内容，可能为null
        /// </summary>
        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }
}