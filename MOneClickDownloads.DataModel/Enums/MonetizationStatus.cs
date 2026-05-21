using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Converters;

namespace MOneClickDownloads.DataModel.Enums
{
    /// <summary>
    /// 变现状态枚举，表示项目的盈利/货币化状态。
    /// 
    /// 用途：
    /// - 在 Project 模型中标识项目是否开启了变现功能
    /// - 仅项目拥有者和管理员可见此字段
    /// 
    /// 数据流转：
    /// - 从 Get a project API 响应的 monetization_status 字段反序列化
    /// </summary>
    [JsonConverter(typeof(EnumMemberJsonConverter<MonetizationStatus>))]
    public enum MonetizationStatus
    {
        /// <summary>
        /// 已开启变现：项目已启用收入功能
        /// </summary>
        [EnumMember(Value = "monetized")]
        Monetized,

        /// <summary>
        /// 已关闭变现：项目已主动关闭收入功能
        /// </summary>
        [EnumMember(Value = "demonetized")]
        Demonetized,

        /// <summary>
        /// 强制关闭变现：管理员强制关闭了项目的收入功能
        /// </summary>
        [EnumMember(Value = "force-demonetized")]
        ForceDemonetized
    }
}