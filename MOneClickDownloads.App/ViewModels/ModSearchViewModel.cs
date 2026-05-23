using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MOneClickDownloads.App.DI;
using MOneClickDownloads.App.Views;
using MOneClickDownloads.DataModel.Favorites;
using MOneClickDownloads.DataModel.Search;
using MOneClickDownloads.Service;
using Serilog;

namespace MOneClickDownloads.App.ViewModels
{
    public partial class ModSearchViewModel : ViewModelBase
    {
        private readonly INavigationService _navigation;
        private readonly ModSearchService _searchService;
        private readonly IFavoriteService _favoriteService;
        private readonly IIconCacheService _iconCacheService;
        private readonly ILogger _logger;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ProjectHit> _searchResults = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _totalHits;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 当前右键点击的搜索结果项
        /// </summary>
        [ObservableProperty]
        private ProjectHit? _rightClickedItem;

        /// <summary>
        /// 收藏夹列表（右键菜单子菜单数据源）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<FavoriteCollection> _favoriteCollections = new();

        /// <summary>
        /// 构造搜索页面 ViewModel，所有依赖通过 DI 容器注入。
        /// </summary>
        /// <param name="navigation">导航服务（DI 注入）</param>
        /// <param name="searchService">模组搜索服务（DI 注入）</param>
        /// <param name="favoriteService">收藏夹服务（DI 注入）</param>
        public ModSearchViewModel(
            INavigationService navigation,
            ModSearchService searchService,
            IFavoriteService favoriteService,
            IIconCacheService iconCacheService)
        {
            _navigation = navigation;
            _searchService = searchService;
            _favoriteService = favoriteService;
            _iconCacheService = iconCacheService;
            _logger = Log.ForContext<ModSearchViewModel>();
            _logger.Information("ModSearchViewModel 初始化完成");
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
                return;

            _logger.Information("开始搜索: Query={Query}", SearchQuery.Trim());
            IsLoading = true;
            StatusMessage = "搜索中...";
            SearchResults.Clear();

            try
            {
                var response = await _searchService.SearchAsync(SearchQuery.Trim());
                TotalHits = response.TotalHits;

                foreach (var hit in response.Hits)
                {
                    SearchResults.Add(hit);
                }

                StatusMessage = $"共找到 {TotalHits} 个结果";
                _logger.Information("搜索完成: Query={Query}, TotalHits={TotalHits}", SearchQuery.Trim(), TotalHits);

                // 后台缓存搜索结果的图标（使用 projectId 作为 modId）
                var iconItems = response.Hits
                    .Where(h => !string.IsNullOrEmpty(h.IconUrl))
                    .Select(h => (h.ProjectId, h.IconUrl!))
                    .ToList();
                if (iconItems.Count > 0)
                {
                    _ = Task.Run(async () => await _iconCacheService.CacheIconsAsync(iconItems));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"搜索失败: {ex.Message}";
                _logger.Error(ex, "搜索失败: Query={Query}", SearchQuery.Trim());
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void SelectMod(ProjectHit? hit)
        {
            if (hit == null) return;
            _logger.Information("用户选择模组: ProjectId={ProjectId}, Title={Title}", hit.ProjectId, hit.Title);
            _navigation.MainViewModel.NavigateToDetail(hit.ProjectId, hit.Title, hit.Description, hit.Slug);
        }

        /// <summary>
        /// 刷新收藏夹列表（右键菜单展开时调用）
        /// </summary>
        [RelayCommand]
        private void LoadFavoriteCollections()
        {
            var collections = _favoriteService.GetAllCollections();
            FavoriteCollections = new ObservableCollection<FavoriteCollection>(collections);
            _logger.Information("收藏夹列表已刷新，共 {Count} 个收藏夹", collections.Count);
        }

        /// <summary>
        /// 将右键点击的模组添加到指定收藏夹
        /// </summary>
        [RelayCommand]
        private void AddToFavorites(FavoriteCollection? collection)
        {
            if (collection == null || RightClickedItem == null) return;

            var item = new FavoriteItem
            {
                ProjectId = RightClickedItem.ProjectId,
                Title = RightClickedItem.Title,
                Description = RightClickedItem.Description,
                Slug = RightClickedItem.Slug,
                IconUrl = RightClickedItem.IconUrl,
                ProjectType = RightClickedItem.ProjectType,
                Categories = RightClickedItem.Categories?.ToList() ?? new(),
                ClientSide = RightClickedItem.ClientSide,
                ServerSide = RightClickedItem.ServerSide,
                Downloads = RightClickedItem.Downloads,
                FavoritedAt = DateTime.UtcNow
            };

            try
            {
                _favoriteService.AddItem(collection.Id, item);
                _logger.Information("已将模组 {Title} 添加到收藏夹 {CollectionName}", RightClickedItem.Title, collection.Name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "添加到收藏夹失败: ProjectId={ProjectId}, CollectionId={CollectionId}", RightClickedItem.ProjectId, collection.Id);
            }
        }

        /// <summary>
        /// 创建新收藏夹并添加右键点击的模组
        /// </summary>
        [RelayCommand]
        private async Task CreateNewCollectionAndAddAsync()
        {
            if (RightClickedItem == null) return;

            var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var mainWindow = desktop?.MainWindow;
            if (mainWindow == null) return;

            var dialog = new CreateCollectionDialog();
            var result = await dialog.ShowDialog<string?>(mainWindow);

            if (string.IsNullOrWhiteSpace(result)) return;

            try
            {
                var collection = _favoriteService.CreateCollection(result.Trim());
                _logger.Information("已创建新收藏夹: {Name}, Id={Id}", collection.Name, collection.Id);

                // 将当前模组添加到新收藏夹
                AddToFavorites(collection);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "创建收藏夹失败: {Name}", result);
            }
        }
    }
}