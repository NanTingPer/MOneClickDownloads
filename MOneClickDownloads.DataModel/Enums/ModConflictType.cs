using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Enums
{
    /// <summary>
    /// 模组冲突类型，表示待下载的模组与本地已安装模组之间的冲突关系。<br />
    /// <br />
    /// 使用场景：<br />
    /// - 下载前扫描本地文件夹，检测是否存在同ID模组<br />
    /// - 根据版本比较结果确定冲突类型，供用户决定是否替换<br />
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ModConflictType
    {
        /// <summary>
        /// 无冲突，本地不存在同ID模组，可直接下载。
        /// </summary>
        None,

        /// <summary>
        /// 完全相同的模组ID和版本已存在于本地。
        /// </summary>
        SameModExists,

        /// <summary>
        /// 本地已有更高版本的同ID模组。
        /// </summary>
        HigherVersionExists,

        /// <summary>
        /// 本地已有更低版本的同ID模组（可升级）。
        /// </summary>
        LowerVersionExists
    }
}