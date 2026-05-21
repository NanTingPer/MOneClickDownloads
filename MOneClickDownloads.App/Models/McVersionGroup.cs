using System.Collections.Generic;

namespace MOneClickDownloads.App.Models
{
    /// <summary>
    /// MC大版本分组，用于在UI中按Minecraft大版本号（如1.20、1.21）分组展示版本列表。
    /// </summary>
    public class McVersionGroup
    {
        /// <summary>
        /// MC大版本号（如 "1.20", "1.21"）
        /// </summary>
        public string MajorVersion { get; set; } = string.Empty;

        /// <summary>
        /// 该大版本下的所有版本条目
        /// </summary>
        public List<ModVersionItem> Versions { get; set; } = new List<ModVersionItem>();
    }
}