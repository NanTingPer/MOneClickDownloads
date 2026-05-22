using System;
using Microsoft.Extensions.DependencyInjection;
using MOneClickDownloads.App.ViewModels;

namespace MOneClickDownloads.App.DI
{
    /// <summary>
    /// 导航服务实现，从 DI 容器创建各页面的 ViewModel 并协调导航。
    /// <br />
    /// 设计模式：
    /// - MainViewModel 使用 Lazy 延迟获取，避免循环依赖
    /// - 子 ViewModel 通过 IServiceProvider 按需创建
    /// - ActivatorUtilities.CreateInstance 用于需要运行时参数的 ViewModel
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<MainWindowViewModel> _mainViewModel;

        /// <summary>
        /// 主窗口 ViewModel，延迟获取以避免循环依赖。
        /// </summary>
        public MainWindowViewModel MainViewModel => _mainViewModel.Value;

        /// <summary>
        /// 构造导航服务。
        /// </summary>
        /// <param name="serviceProvider">DI 服务提供者</param>
        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _mainViewModel = new Lazy<MainWindowViewModel>(
                () => _serviceProvider.GetRequiredService<MainWindowViewModel>());
        }

        /// <summary>
        /// 创建搜索页面 ViewModel。
        /// 从 DI 容器解析 ModSearchViewModel（Transient），注入 INavigationService。
        /// </summary>
        /// <returns>搜索页面 ViewModel</returns>
        public ModSearchViewModel CreateSearchViewModel()
        {
            return _serviceProvider.GetRequiredService<ModSearchViewModel>();
        }

        /// <summary>
        /// 创建模组详情页面 ViewModel。
        /// 使用 ActivatorUtilities.CreateInstance 处理需要运行时参数的创建。
        /// </summary>
        /// <returns>详情页面 ViewModel</returns>
        public ModDetailViewModel CreateDetailViewModel(string projectId, string projectTitle, string projectDescription, string? projectSlug = null)
        {
            return ActivatorUtilities.CreateInstance<ModDetailViewModel>(
                _serviceProvider, projectId, projectTitle, projectDescription, projectSlug!);
        }

        /// <summary>
        /// 创建收藏夹列表页面 ViewModel。
        /// 从 DI 容器解析 FavoritesViewModel（Transient），注入 INavigationService 和 IFavoriteService。
        /// </summary>
        /// <returns>收藏夹列表页面 ViewModel</returns>
        public FavoritesViewModel CreateFavoritesViewModel()
        {
            return _serviceProvider.GetRequiredService<FavoritesViewModel>();
        }
    }
}
