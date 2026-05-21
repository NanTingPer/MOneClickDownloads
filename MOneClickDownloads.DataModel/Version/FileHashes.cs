using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Version
{
    /// <summary>
    /// 文件哈希模型，包含一个版本文件的多种哈希算法计算值。
    /// 
    /// 用途：
    /// - 在 VersionFile 模型的 hashes 字段中使用
    /// - 用于验证下载文件的完整性（SHA-512、SHA-1）
    /// - 可用于文件去重和缓存校验
    /// 
    /// 数据流转：
    /// - 从 Get a version API 响应的 files[].hashes 字段反序列化
    /// - 下载完成后用于校验文件完整性
    /// </summary>
    public class FileHashes
    {
        /// <summary>
        /// 文件的 SHA-512 哈希值
        /// </summary>
        [JsonPropertyName("sha512")]
        public string? Sha512 { get; set; }

        /// <summary>
        /// 文件的 SHA-1 哈希值
        /// </summary>
        [JsonPropertyName("sha1")]
        public string? Sha1 { get; set; }
    }
}