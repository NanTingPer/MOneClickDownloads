using MOneClickDownloads.DataModel.Mod;
using MOneClickDownloads.DataModel.Version;
using MOneClickDownloads.Service.Models;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 模组冲突检测服务接口，负责检测待下载模组与本地已安装模组之间的冲突。<br />
    /// <br />
    /// 检测策略（三级检测）：<br />
    /// 1. <see cref="DetectBySlugOrTitle"/> — 下载前预检，通过 slug/title 匹配本地模组<br />
    /// 2. <see cref="DetectByModId"/> — JAR 分析后检测，通过提取的 ModId 匹配<br />
    /// 3. <see cref="DetectByFileName"/> — 最终回退，通过文件名精确匹配<br />
    /// <br />
    /// 使用场景：<br />
    /// - ModDownloadService 在下载流程中调用此服务进行冲突检测<br />
    /// - 可独立用于批量扫描场景（如启动时检测已安装模组状态）
    /// </summary>
    public interface IModConflictService
    {
        /// <summary>
        /// 通过 slug/title 预检冲突（下载前第一级检测）。<br />
        /// <br />
        /// 匹配逻辑：<br />
        /// 1. 若提供 slug → 按 slug 与本地 ModId 匹配（忽略大小写）<br />
        /// 2. 若未命中且提供 title → 按 title 与本地 Name 匹配（忽略大小写）<br />
        /// 3. 匹配成功 → 构造 ModConflictInfo 返回
        /// </summary>
        /// <param name="inventory">本地模组清单</param>
        /// <param name="version">待下载的版本信息</param>
        /// <param name="file">待下载的文件信息</param>
        /// <param name="projectSlug">项目 slug</param>
        /// <param name="projectTitle">项目标题</param>
        /// <returns>冲突信息；无冲突返回 null</returns>
        ModConflictInfo? DetectBySlugOrTitle(
            LocalModInventory inventory,
            ModrinthVersion version,
            VersionFile file,
            string? projectSlug,
            string? projectTitle);

        /// <summary>
        /// 通过已分析的 ModId 检测冲突（第二级检测）。<br />
        /// <br />
        /// 使用下载后分析临时文件得到的 ModId 去本地清单中查找同 ID 模组。
        /// </summary>
        /// <param name="inventory">本地模组清单</param>
        /// <param name="downloadedAnalysis">从临时文件分析得到的结果</param>
        /// <param name="version">待下载的版本信息</param>
        /// <param name="file">待下载的文件信息</param>
        /// <returns>冲突信息；无冲突返回 null</returns>
        ModConflictInfo? DetectByModId(
            LocalModInventory inventory,
            ModAnalysisResult downloadedAnalysis,
            ModrinthVersion version,
            VersionFile file);

        /// <summary>
        /// 通过文件名检测冲突（第三级检测）。<br />
        /// <br />
        /// 当预检和 JAR 分析均未命中时，使用文件名精确匹配作为最终回退。
        /// </summary>
        /// <param name="inventory">本地模组清单</param>
        /// <param name="version">待下载的版本信息</param>
        /// <param name="file">待下载的文件信息</param>
        /// <returns>冲突信息；无冲突返回 null</returns>
        ModConflictInfo? DetectByFileName(
            LocalModInventory inventory,
            ModrinthVersion version,
            VersionFile file);

        /// <summary>
        /// 比较两个版本号字符串。<br />
        /// <br />
        /// 比较策略：<br />
        /// 1. 先尝试解析为 SemVer（主.次.修订），逐级比较数值<br />
        /// 2. 去除 + 后缀（build metadata）后比较<br />
        /// 3. 解析失败则回退到字符串 OrdinalIgnoreCase 比较<br />
        /// <br />
        /// 返回值：<br />
        /// - 正数：version1 > version2<br />
        /// - 0：version1 == version2<br />
        /// - 负数：version1 < version2
        /// </summary>
        /// <param name="version1">版本号1</param>
        /// <param name="version2">版本号2</param>
        /// <returns>比较结果</returns>
        int CompareVersions(string version1, string version2);
    }
}