using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MOneClickDownloads.App.DI;
using MOneClickDownloads.DataModel.Search;
using MOneClickDownloads.Service;
using Serilog;

namespace MOneClickDownloads.App.ViewModels
{
    public partial class ModSearchViewModel : ViewModelBase
    {
        private readonly INavigationService _navigation;
        private readonly ModSearchService _searchService;
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
        /// 构造搜索页面 ViewModel，所有依赖通过 DI 容器注入。
        /// </summary>
        /// <param name="navigation">导航服务（DI 注入）</param>
        /// <param name="searchService">模组搜索服务（DI 注入）</param>
        public ModSearchViewModel(INavigationService navigation, ModSearchService searchService)
        {
            _navigation = navigation;
            _searchService = searchService;
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
    }
}