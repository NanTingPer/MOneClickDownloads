using MOneClickDownloads.App.ViewModels;

namespace MOneClickDownloads.App.DI
{
    /// <summary>
    /// 导航服务接口，负责页面间的 ViewModel 创建和导航协调。
    /// <br />
    /// 职责：
    /// - 从 DI 容器创建各页面的 ViewModel
    /// - 提供对主 ViewModel 的访问（用于子页面反向导航）
    /// <br />
    /// 设计模式：
    /// 类似 ASP.NET Core 的 IUrlHelper / IActionContextAccessor 模式，
    /// 将导航逻辑从 ViewModel 中抽离，职责更清晰。
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// 主窗口 ViewModel，用于页面间反向导航。
        /// </summary>
        MainWindowViewModel MainViewModel { get; }

        /// <summary>
        /// 创建搜索页面 ViewModel。
        /// </summary>
        /// <returns>搜索页面 ViewModel</returns>
        ModSearchViewModel CreateSearchViewModel();

        /// <summary>
        /// 创建模组详情页面 ViewModel。
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="projectTitle">项目标题</param>
        /// <param name="projectDescription">项目描述</param>
        /// <param name="projectSlug">项目 slug</param>
        /// <returns>详情页面 ViewModel</returns>
        ModDetailViewModel CreateDetailViewModel(string projectId, string projectTitle, string projectDescription, string? projectSlug = null);

        /// <summary>
        /// 创建收藏夹列表页面 ViewModel。
        /// </summary>
        /// <returns>收藏夹列表页面 ViewModel</returns>
        FavoritesViewModel CreateFavoritesViewModel();
    }
}
