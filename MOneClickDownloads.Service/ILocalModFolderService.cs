using System;
using System.Collections.Generic;

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
        /// 获取所有已记录的文件夹路径
        /// </summary>
        List<string> GetAllFolders();

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
    }
}