using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Converters;

namespace MOneClickDownloads.DataModel.Enums
{
    /// <summary>
    /// 项目状态枚举，表示 Modrinth 项目的审核/发布状态。
    /// 
    /// 用途：
    /// - 在 Project 模型中标识项目当前所处的状态阶段
    /// - 可用于筛选已审核通过的项目或排查项目是否被拒绝
    /// 
    /// 数据流转：
    /// - 从 Get a project API 响应的 status 字段反序列化
    /// - 仅项目拥有者和管理员可见此字段
    /// </summary>
    [JsonConverter(typeof(EnumMemberJsonConverter<ProjectStatus>))]
    public enum ProjectStatus
    {
        /// <summary>
        /// 已审核通过：项目已在 Modrinth 上公开发布
        /// </summary>
        [EnumMember(Value = "approved")]
        Approved,

        /// <summary>
        /// 已归档：项目已被作者归档，不再更新
        /// </summary>
        [EnumMember(Value = "archived")]
        Archived,

        /// <summary>
        /// 已拒绝：项目未通过审核
        /// </summary>
        [EnumMember(Value = "rejected")]
        Rejected,

        /// <summary>
        /// 草稿：项目尚未提交审核
        /// </summary>
        [EnumMember(Value = "draft")]
        Draft,

        /// <summary>
        /// 未列出：项目已审核但不公开显示在搜索结果中
        /// </summary>
        [EnumMember(Value = "unlisted")]
        Unlisted,

        /// <summary>
        /// 处理中：项目正在等待审核
        /// </summary>
        [EnumMember(Value = "processing")]
        Processing,

        /// <summary>
        /// 保留：项目被保留
        /// </summary>
        [EnumMember(Value = "withheld")]
        Withheld,

        /// <summary>
        /// 已计划：项目计划在未来某个时间发布
        /// </summary>
        [EnumMember(Value = "scheduled")]
        Scheduled,

        /// <summary>
        /// 私有：项目为私有状态
        /// </summary>
        [EnumMember(Value = "private")]
        Private,

        /// <summary>
        /// 未知：项目状态未知
        /// </summary>
        [EnumMember(Value = "unknown")]
        Unknown
    }
}