using System;
using System.Collections.Generic;
using MOneClickDownloads.Service.Models;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 本地模组文件夹管理服务接口。
    /// 管理已记录的本地 mods 文件夹列表，持久化到 JSON 文件。
    /// </summary>
    public interface ILocalModFolderService
    {
        /// <summary>
        /// 文件夹列表变更事件
        /// </summary>
        event EventHandler? Changed;

        /// <summary>
        /// 获取所有已记录的文件夹条目（含路径、自定义名称、筛选元数据）
        /// </summary>
        List<LocalModFolderEntry> GetAllFolders();

        /// <summary>
        /// 添加一个文件夹路径到记录中
        /// </summary>
        /// <param name="folderPath">文件夹完整路径</param>
        /// <returns>是否成功添加（已存在则返回 false）</returns>
        bool AddFolder(string folderPath);

        /// <summary>
        /// 移除一个文件夹路径记录
        /// </summary>
        /// <param name="folderPath">文件夹完整路径</param>
        /// <returns>是否成功移除</returns>
        bool RemoveFolder(string folderPath);

        /// <summary>
        /// 检查文件夹路径是否已被记录
        /// </summary>
        bool ContainsFolder(string folderPath);

        /// <summary>
        /// 重命名文件夹（设置/清除自定义显示名称）
        /// </summary>
        /// <param name="folderPath">文件夹完整路径</param>
        /// <param name="customName">自定义名称，null 或空字符串表示清除自定义名称</param>
        /// <returns>是否成功更新</returns>
        bool RenameFolder(string folderPath, string? customName);

        /// <summary>
        /// 更新文件夹的版本筛选元数据
        /// </summary>
        /// <param name="folderPath">文件夹完整路径</param>
        /// <param name="mcVersions">MC 版本列表</param>
        /// <param name="loaders">加载器列表</param>
        /// <param name="projectId">参考模组 ProjectId</param>
        /// <param name="modName">参考模组名称</param>
        /// <returns>是否成功更新</returns>
        bool UpdateFolderMetadata(string folderPath, List<string> mcVersions, List<string> loaders, string? projectId, string? modName);
    }
}