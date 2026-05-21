using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Converters;

namespace MOneClickDownloads.DataModel.Enums
{
    /// <summary>
    /// 项目类型枚举，表示 Modrinth 上的项目分类类型。
    /// 
    /// 用途：
    /// - 在 Project 模型和搜索结果中标识项目属于模组、整合包、资源包还是光影包
    /// - 在搜索请求的 facets 中作为过滤条件，如 project_type:mod
    /// 
    /// 数据流转：
    /// - 从 Get a project / Search projects API 响应的 project_type 字段反序列化
    /// - 在搜索请求中作为 facet 过滤条件传入
    /// </summary>
    [JsonConverter(typeof(EnumMemberJsonConverter<ProjectType>))]
    public enum ProjectType
    {
        /// <summary>
        /// 模组（mod）：Minecraft 的功能扩展模块
        /// </summary>
        [EnumMember(Value = "mod")]
        Mod,

        /// <summary>
        /// 整合包（modpack）：预配置的模组集合
        /// </summary>
        [EnumMember(Value = "modpack")]
        Modpack,

        /// <summary>
        /// 资源包（resourcepack）：纹理、音效等资源替换包
        /// </summary>
        [EnumMember(Value = "resourcepack")]
        ResourcePack,

        /// <summary>
        /// 光影包（shader）：画面渲染效果增强包
        /// </summary>
        [EnumMember(Value = "shader")]
        Shader
    }
}