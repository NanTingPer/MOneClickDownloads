using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Converters;

namespace MOneClickDownloads.DataModel.Enums
{
    /// <summary>
    /// 端侧支持枚举，表示模组在客户端/服务端的支持情况。
    /// 
    /// 用途：
    /// - 在 Project 模型中用于标识客户端(client_side)和服务端(server_side)的支持类型
    /// - 用于搜索过滤（如搜索仅客户端需要的模组）
    /// 
    /// 数据流转：
    /// - 从 Get a project / Search projects API 响应中反序列化
    /// - 在搜索请求中作为 facets 过滤条件使用
    /// </summary>
    [JsonConverter(typeof(EnumMemberJsonConverter<SideSupport>))]
    public enum SideSupport
    {
        /// <summary>
        /// 必需：此模组必须安装在该端
        /// </summary>
        [EnumMember(Value = "required")]
        Required,

        /// <summary>
        /// 可选：此模组可以选择性安装在该端
        /// </summary>
        [EnumMember(Value = "optional")]
        Optional,

        /// <summary>
        /// 不支持：此模组不支持该端
        /// </summary>
        [EnumMember(Value = "unsupported")]
        Unsupported,

        /// <summary>
        /// 未知：此模组对该端的支持情况未知
        /// </summary>
        [EnumMember(Value = "unknown")]
        Unknown
    }
}