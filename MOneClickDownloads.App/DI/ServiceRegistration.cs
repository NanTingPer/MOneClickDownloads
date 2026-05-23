using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using MOneClickDownloads.App.ViewModels;
using MOneClickDownloads.Service;

namespace MOneClickDownloads.App.DI
{
    /// <summary>
    /// 依赖注入容器的服务注册扩展方法。
    /// 集中管理所有服务和 ViewModel 的注册。
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// 注册应用所需的所有服务和 ViewModel。
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合（支持链式调用）</returns>
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            // ===== 基础设施（Singleton） =====

            // 导航服务 - 协调页面间的 ViewModel 创建和导航
            services.AddSingleton<INavigationService, NavigationService>();

            // ===== 服务层（Singleton） =====

            // Modrinth API 服务 - 共享 HttpClient 连接池
            services.AddSingleton<ModrinthAPIService>();

            // 模组搜索服务 - 无状态，依赖 ModrinthAPIService
            services.AddSingleton<ModSearchService>();

            // 模组文件分析服务 - 无状态
            services.AddSingleton<IModAnalysisService, ModAnalysisService>();

            // 模组冲突检测服务 - 无状态
            services.AddSingleton<IModConflictService, ModConflictService>();

            // 模组下载服务 - 无状态，依赖上面三个服务
            services.AddSingleton<ModDownloadService>();

            // 应用配置服务 - 全局唯一配置管理，使用工厂注册以注入配置文件路径
            services.AddSingleton<ConfigService>(sp =>
            {
                var configPath = Path.Combine(AppContext.BaseDirectory, "configs", "app.json");
                return new ConfigService(configPath);
            });

            // 收藏夹（合集）服务 - JSON 文件持久化，存储目录为 logs 同级的 package 目录
            services.AddSingleton<IFavoriteService>(sp =>
            {
                var packagePath = Path.Combine(AppContext.BaseDirectory, "package");
                return new FavoriteService(packagePath);
            });

            // 本地模组文件夹管理服务 - JSON 文件持久化
            services.AddSingleton<ILocalModFolderService>(sp =>
            {
                var packagePath = Path.Combine(AppContext.BaseDirectory, "package");
                return new LocalModFolderService(packagePath);
            });

            // 图标缓存服务 - 将模组图标持久化到 App 根目录下的 icon_cache 目录
            services.AddSingleton<IIconCacheService>(sp =>
            {
                return new IconCacheService(AppContext.BaseDirectory);
            });

            // ===== ViewModel 层 =====

            // MainWindowViewModel - Singleton（应用主 VM，管理导航状态）
            services.AddSingleton<MainWindowViewModel>();

            // ModSearchViewModel - Singleton（保留搜索状态，避免重复搜索）
            services.AddSingleton<ModSearchViewModel>();

            // FavoritesViewModel - Transient（每次导航到收藏夹页面时创建新实例）
            services.AddTransient<FavoritesViewModel>();

            // LocalModsViewModel - Transient（每次导航到本地模组管理页面时创建新实例）
            services.AddTransient<LocalModsViewModel>();

            // ModDetailViewModel 通过 ActivatorUtilities.CreateInstance 创建（需要运行时参数）

            return services;
        }
    }
}
