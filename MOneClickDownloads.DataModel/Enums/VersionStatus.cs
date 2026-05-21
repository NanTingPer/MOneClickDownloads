using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Converters;

namespace MOneClickDownloads.DataModel.Enums
{
    /// <summary>
    /// 版本状态枚举，表示模组版本的可见性/发布状态。
    /// 
    /// 用途：
    /// - 在 Version 模型中标识版本当前的状态
    /// - 可用于过滤已列出的版本（listed）与草稿（draft）
    /// 
    /// 数据流转：
    /// - 从 Get a version API 响应的 status 字段反序列化
    /// </summary>
    [JsonConverter(typeof(EnumMemberJsonConverter<VersionStatus>))]
    public enum VersionStatus
    {
        /// <summary>
        /// 已列出：版本已公开发布且在项目页面可见
        /// </summary>
        [EnumMember(Value = "listed")]
        Listed,

        /// <summary>
        /// 已归档：版本已被归档
        /// </summary>
        [EnumMember(Value = "archived")]
        Archived,

        /// <summary>
        /// 草稿：版本为草稿状态，尚未发布
        /// </summary>
        [EnumMember(Value = "draft")]
        Draft,

        /// <summary>
        /// 未列出：版本已发布但不显示在项目页面列表中
        /// </summary>
        [EnumMember(Value = "unlisted")]
        Unlisted,

        /// <summary>
        /// 已计划：版本计划在未来某个时间发布
        /// </summary>
        [EnumMember(Value = "scheduled")]
        Scheduled,

        /// <summary>
        /// 未知：版本状态未知
        /// </summary>
        [EnumMember(Value = "unknown")]
        Unknown
    }
}