using System.Text.Json.Serialization;

namespace MOneClickDownloads.App.Models
{
    /// <summary>
    /// 表示一个已记录的本地 mods 文件夹。
    /// </summary>
    public class LocalModFolder
    {
        /// <summary>
        /// 文件夹完整路径
        /// </summary>
        [JsonPropertyName("folder_path")]
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称（取路径最后一级目录名）
        /// </summary>
        [JsonIgnore]
        public string DisplayName => string.IsNullOrEmpty(FolderPath)
            ? string.Empty
            : System.IO.Path.GetFileName(FolderPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));

        /// <summary>
        /// 扫描后的模组数量
        /// </summary>
        [JsonIgnore]
        public int ModCount { get; set; }
    }
}