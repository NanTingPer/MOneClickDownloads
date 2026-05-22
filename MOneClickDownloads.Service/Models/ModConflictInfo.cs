using MOneClickDownloads.DataModel.Enums;

namespace MOneClickDownloads.Service.Models
{
    /// <summary>
    /// 模组冲突信息模型，描述待下载模组与本地已安装模组之间的冲突详情。<br />
    /// <br />
    /// 使用场景：<br />
    /// - ModDownloadService 在下载前扫描本地文件夹，检测到同ID模组时构造此对象<br />
    /// - 通过冲突回调传递给 UI 层，让用户决定处理方式<br />
    /// - UI 层根据 ConflictType 展示不同提示信息<br />
    /// <br />
    /// 数据流转：<br />
    /// 1. LocalModInventory 扫描本地文件夹，获取已安装模组列表<br />
    /// 2. ModDownloadService 对比待下载模组与本地模组的版本号<br />
    /// 3. 确定冲突类型后构造此对象<br />
    /// 4. 通过 ModConflictCallback 回调传递给调用方<br />
    /// 5. 调用方返回 ModConflictResolution 决定处理方式
    /// </summary>
    public class ModConflictInfo
    {
        /// <summary>
        /// 冲突类型。
        /// </summary>
        public ModConflictType ConflictType { get; set; }

        /// <summary>
        /// 冲突的模组 ID。
        /// </summary>
        public string ModId { get; set; } = string.Empty;

        /// <summary>
        /// 冲突的模组名称。
        /// </summary>
        public string ModName { get; set; } = string.Empty;

        /// <summary>
        /// 本地已存在的版本号（如 "1.0.0"）。
        /// </summary>
        public string? ExistingVersion { get; set; }

        /// <summary>
        /// 本地已存在模组文件的完整路径。
        /// </summary>
        public string? ExistingFilePath { get; set; }

        /// <summary>
        /// 待下载的新版本号（如 "2.0.0"）。
        /// </summary>
        public string? NewVersion { get; set; }

        /// <summary>
        /// 待下载的文件名。
        /// </summary>
        public string? NewFileName { get; set; }
    }
}