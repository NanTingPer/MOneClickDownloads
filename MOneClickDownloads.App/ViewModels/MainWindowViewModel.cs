using System;
using System.IO;
using MOneClickDownloads.Service;
using Serilog;

namespace MOneClickDownloads.App.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private static readonly ILogger Logger = Log.ForContext<MainWindowViewModel>();

        /// <summary>
        /// 共享的 Modrinth API 服务实例，注入到子 ViewModel 中复用 HTTP 连接。
        /// </summary>
        public ModrinthAPIService ApiService { get; }

        /// <summary>
        /// 共享的模组搜索服务
        /// </summary>
        public ModSearchService SearchService { get; }

        /// <summary>
        /// 共享的模组下载服务
        /// </summary>
        public ModDownloadService DownloadService { get; }

        /// <summary>
        /// 共享的应用配置服务，提供配置项的持久化读写。
        /// </summary>
        public ConfigService ConfigService { get; }

        /// <summary>
        /// 共享的模组文件分析服务，用于分析 JAR 包提取模组元数据。
        /// </summary>
        public IModAnalysisService ModAnalysisService { get; }

        /// <summary>
        /// 共享的模组冲突检测服务
        /// </summary>
        public IModConflictService ConflictService { get; }

        private ViewModelBase? _currentViewModel;
        /// <summary>
        /// 当前显示的子页面 ViewModel，通过 ViewLocator 自动匹配 View。
        /// </summary>
        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public MainWindowViewModel()
        {
            Logger.Information("MainWindowViewModel 初始化开始");

            ApiService = new ModrinthAPIService();
            SearchService = new ModSearchService(ApiService);

            var configPath = Path.Combine(AppContext.BaseDirectory, "configs", "app.json");
            ConfigService = new ConfigService(configPath);
            ModAnalysisService = new ModAnalysisService();
            ConflictService = new ModConflictService();
            DownloadService = new ModDownloadService(ApiService, ModAnalysisService, ConflictService);

            // 默认显示搜索页
            NavigateToSearch();

            Logger.Information("MainWindowViewModel 初始化完成");
        }

        /// <summary>
        /// 导航到搜索页面
        /// </summary>
        public void NavigateToSearch()
        {
            Logger.Information("导航到搜索页面");
            CurrentViewModel = new ModSearchViewModel(this);
        }

        /// <summary>
        /// 导航到模组详情页面（版本选择页）
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="projectTitle">项目标题</param>
        /// <param name="projectDescription">项目描述</param>
        /// <param name="projectSlug">项目 slug，用于下载前冲突预检</param>
        public void NavigateToDetail(string projectId, string projectTitle, string projectDescription, string? projectSlug = null)
        {
            Logger.Information("导航到模组详情页面: ProjectId={ProjectId}, Title={Title}, Slug={Slug}", projectId, projectTitle, projectSlug ?? "null");
            CurrentViewModel = new ModDetailViewModel(this, projectId, projectTitle, projectDescription, projectSlug);
        }
    }
}