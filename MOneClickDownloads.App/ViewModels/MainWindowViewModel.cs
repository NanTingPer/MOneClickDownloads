using MOneClickDownloads.App.DI;
using Serilog;

namespace MOneClickDownloads.App.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private static readonly ILogger Logger = Log.ForContext<MainWindowViewModel>();

        private readonly INavigationService _navigation;

        private ViewModelBase? _currentViewModel;
        /// <summary>
        /// 当前显示的子页面 ViewModel，通过 ViewLocator 自动匹配 View。
        /// </summary>
        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        /// <summary>
        /// 构造 MainWindowViewModel，通过导航服务协调页面创建。
        /// <br />
        /// 注意：初始导航由 App.axaml.cs 在容器构建完成后触发，不在构造函数中调用，
        /// 以避免 DI 容器解析期间的循环依赖问题。
        /// </summary>
        /// <param name="navigation">导航服务（DI 注入）</param>
        public MainWindowViewModel(INavigationService navigation)
        {
            Logger.Information("MainWindowViewModel 初始化开始");

            _navigation = navigation;

            Logger.Information("MainWindowViewModel 初始化完成");
        }

        /// <summary>
        /// 导航到搜索页面
        /// </summary>
        public void NavigateToSearch()
        {
            Logger.Information("导航到搜索页面");
            CurrentViewModel = _navigation.CreateSearchViewModel();
        }

        /// <summary>
        /// 导航到收藏夹列表页面
        /// </summary>
        public void NavigateToFavorites()
        {
            Logger.Information("导航到收藏夹列表页面");
            CurrentViewModel = _navigation.CreateFavoritesViewModel();
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
            CurrentViewModel = _navigation.CreateDetailViewModel(projectId, projectTitle, projectDescription, projectSlug);
        }
    }
}
