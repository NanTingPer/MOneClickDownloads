namespace MOneClickDownloads.Service.Models
{
    /// <summary>
    /// 单个文件的下载结果模型，记录一次下载操作的结果信息。<br />
    /// <br />
    /// 使用场景：<br />
    /// - ModDownloadService 完成一次文件下载后返回此对象<br />
    /// - 记录下载的文件路径、来源URL、所属模组信息<br />
    /// - 用于事务回滚时确定需要清理的文件<br />
    /// <br />
    /// 数据流转：<br />
    /// - ModDownloadService 下载文件成功后构造此对象<br />
    /// - 多个 DownloadResult 组成 <code>List</code> 作为下载任务的完整结果<br />
    /// - 事务回滚时遍历此列表中的 FilePath 删除已下载文件
    /// </summary>
    public class DownloadResult
    {
        /// <summary>
        /// 下载的文件保存路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 下载来源URL
        /// </summary>
        public string SourceUrl { get; set; } = string.Empty;

        /// <summary>
        /// 所属模组项目ID
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// 所属模组项目名称
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// 下载的版本ID
        /// </summary>
        public string VersionId { get; set; } = string.Empty;

        /// <summary>
        /// 下载的文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 是否为依赖下载（<code>true</code> 表示是被作为依赖自动下载的）
        /// </summary>
        public bool IsDependency { get; set; }
    }
}