using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Converters;

namespace MOneClickDownloads.DataModel.Enums
{
    /// <summary>
    /// 版本类型枚举，表示模组版本的发布频道。
    /// 
    /// 用途：
    /// - 在 Version 模型中标识该版本是正式版、测试版还是开发版
    /// - 在 List project's versions API 中用于按版本类型过滤
    /// - 用户可据此判断版本的稳定性
    /// 
    /// 数据流转：
    /// - 从 Get a version / List project's versions API 响应的 version_type 字段反序列化
    /// - 在请求版本列表时可作为查询参数过滤
    /// </summary>
    [JsonConverter(typeof(EnumMemberJsonConverter<VersionType>))]
    public enum VersionType
    {
        /// <summary>
        /// 正式发布版（release）：经过充分测试的稳定版本
        /// </summary>
        [EnumMember(Value = "release")]
        Release,

        /// <summary>
        /// 测试版（beta）：功能基本完成但可能存在问题的版本
        /// </summary>
        [EnumMember(Value = "beta")]
        Beta,

        /// <summary>
        /// 开发版（alpha）：处于积极开发中的版本，可能不稳定
        /// </summary>
        [EnumMember(Value = "alpha")]
        Alpha
    }
}