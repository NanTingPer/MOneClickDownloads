using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Converters;

namespace MOneClickDownloads.DataModel.Enums
{
    /// <summary>
    /// 依赖类型枚举，表示一个版本对另一个模组版本的依赖关系类型。
    /// 
    /// 用途：
    /// - 在 Version 模型的 dependencies 数组中标识每个依赖的类型
    /// - 下载逻辑中用于区分必须下载的依赖和可选依赖
    /// - "required" 类型的依赖需要递归下载，"optional" 由用户选择
    /// 
    /// 数据流转：
    /// - 从 Get a version API 响应的 dependencies[].dependency_type 字段反序列化
    /// - 在递归下载依赖时根据此字段决定是否自动下载
    /// </summary>
    [JsonConverter(typeof(EnumMemberJsonConverter<DependencyType>))]
    public enum DependencyType
    {
        /// <summary>
        /// 必需依赖（required）：此模组运行必须安装该依赖模组，应自动下载
        /// </summary>
        [EnumMember(Value = "required")]
        Required,

        /// <summary>
        /// 可选依赖（optional）：此模组可以搭配该依赖模组使用，由用户决定是否下载
        /// </summary>
        [EnumMember(Value = "optional")]
        Optional,

        /// <summary>
        /// 不兼容（incompatible）：此模组与该依赖模组不兼容，不应同时安装
        /// </summary>
        [EnumMember(Value = "incompatible")]
        Incompatible,

        /// <summary>
        /// 内嵌（embedded）：该依赖模组已内嵌在此模组中，无需单独下载
        /// </summary>
        [EnumMember(Value = "embedded")]
        Embedded
    }
}