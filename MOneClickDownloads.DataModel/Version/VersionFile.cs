using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Converters;

namespace MOneClickDownloads.DataModel.Version
{
    /// <summary>
    /// 文件类型枚举，表示版本文件的特殊类型。
    /// </summary>
    [JsonConverter(typeof(EnumMemberJsonConverter<FileType>))]
    public enum FileType
    {
        /// <summary>必需资源包</summary>
        [EnumMember(Value = "required-resource-pack")]
        RequiredResourcePack,

        /// <summary>可选资源包</summary>
        [EnumMember(Value = "optional-resource-pack")]
        OptionalResourcePack,

        /// <summary>源代码jar包</summary>
        [EnumMember(Value = "sources-jar")]
        SourcesJar,

        /// <summary>开发用jar包</summary>
        [EnumMember(Value = "dev-jar")]
        DevJar,

        /// <summary>JavaDoc文档jar包</summary>
        [EnumMember(Value = "javadoc-jar")]
        JavadocJar,

        /// <summary>未知类型</summary>
        [EnumMember(Value = "unknown")]
        Unknown,

        /// <summary>签名文件</summary>
        [EnumMember(Value = "signature")]
        Signature
    }

    /// <summary>
    /// 版本文件模型，表示一个模组版本下的可下载文件。
    /// 
    /// 用途：
    /// - 在 Version 模型的 files 数组中使用
    /// - 获取文件的直接下载链接（url 字段），用于实际下载
    /// - 获取文件哈希值用于完整性校验
    /// - 通过 primary 标识确定主要下载文件（通常每个版本只有一个 primary 文件）
    /// 
    /// 数据流转：
    /// - 从 Get a version API 响应的 files[] 数组反序列化
    /// - 其 url 字段直接用于 HTTP 下载
    /// - 下载后使用 hashes 字段校验文件完整性
    /// - CDN下载URL格式: https://cdn.modrinth.com/data/{project_id}/versions/{version_id}/{filename}
    /// </summary>
    public class VersionFile
    {
        /// <summary>
        /// 文件的哈希值集合（SHA-512、SHA-1），用于下载后校验完整性
        /// </summary>
        [JsonPropertyName("hashes")]
        public FileHashes Hashes { get; set; } = new FileHashes();

        /// <summary>
        /// 文件的直接下载链接URL。
        /// 
        /// 重要：这是实际下载文件所需的URL。
        /// 典型格式: https://cdn.modrinth.com/data/{project_id}/versions/{version_id}/{filename}
        /// 
        /// 可追加查询参数:
        /// - mr_download_reason=standalone
        /// - mr_game_version={mc_version}
        /// - mr_loader={loader}
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 文件名（如 "fabric-api-0.141.4+1.21.11.jar"）
        /// </summary>
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        /// <summary>
        /// 是否为该版本的主要文件。每个版本最多只有一个 primary 文件。
        /// 如果没有任何文件标记为 primary，则第一个文件被视为主要文件。
        /// </summary>
        [JsonPropertyName("primary")]
        public bool Primary { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        [JsonPropertyName("size")]
        public int Size { get; set; }

        /// <summary>
        /// 文件的特殊类型标记（如资源包、源码包等），可能为null表示普通文件
        /// </summary>
        [JsonPropertyName("file_type")]
        public FileType? FileType { get; set; }
    }
}