namespace MOneClickDownloads.Service.Models
{
    /// <summary>
    /// 下载进度报告模型，用于向调用方报告当前下载状态。<br />
    /// <br />
    /// 使用场景：<br />
    /// - 通过 <code>IProgress<DownloadProgress></code> 向UI层报告下载进度<br />
    /// - 显示当前正在下载的文件名、已完成数量、总数量<br />
    /// <br />
    /// 数据流转：<br />
    /// - ModDownloadService 在每完成一个文件下载后构造此对象并报告<br />
    /// - UI层通过 <code>IProgress<DownloadProgress>.Report()</code> 接收并更新进度条
    /// </summary>
    public class DownloadProgress
    {
        /// <summary>
        /// 已完成下载的文件数量
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// 需要下载的文件总数量（包含依赖）
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 当前正在下载的文件名
        /// </summary>
        public string CurrentFileName { get; set; } = string.Empty;

        /// <summary>
        /// 当前正在下载的项目名称（模组名）
        /// </summary>
        public string CurrentProjectName { get; set; } = string.Empty;

        /// <summary>
        /// 下载进度百分比（0-100）
        /// </summary>
        public double Percentage => TotalCount > 0 ? (double)CompletedCount / TotalCount * 100 : 0;
    }
}