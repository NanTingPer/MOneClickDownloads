using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Enums
{
    /// <summary>
    /// 模组冲突解决策略，表示用户在检测到模组冲突时选择的处理方式。<br />
    /// <br />
    /// 使用场景：<br />
    /// - 下载前检测到本地已存在同ID模组时，通过回调让用户选择处理方式<br />
    /// - ModDownloadService 根据此决定是跳过、替换还是保留两者<br />
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ModConflictResolution
    {
        /// <summary>
        /// 跳过：不下载新版本，保留本地已有模组。
        /// </summary>
        Skip,

        /// <summary>
        /// 替换：删除本地已有的旧文件，下载新版本。
        /// </summary>
        Replace,

        /// <summary>
        /// 保留两者：将新文件以不同名称下载，不删除旧文件。
        /// </summary>
        KeepBoth
    }
}