using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MOneClickDownloads.App.DI;
using MOneClickDownloads.App.Models;
using MOneClickDownloads.DataModel.Favorites;
using MOneClickDownloads.Service;
using Serilog;

namespace MOneClickDownloads.App.ViewModels
{
    /// <summary>
    /// 收藏夹列表页面 ViewModel，展示用户收藏的模组按合集分组。
    /// 对应 ModDetailViewModel 在版本列表中的角色。
    /// </summary>
    public partial class FavoritesViewModel : ViewModelBase
    {
        private static readonly ILogger Logger = Log.ForContext<FavoritesViewModel>();

        private readonly INavigationService _navigation;
        private readonly IFavoriteService _favoriteService;

        /// <summary>
        /// 收藏夹分组列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<FavoriteGroup> _favoriteGroups = new();

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        private bool _isLoading = true;

        /// <summary>
        /// 是否有收藏数据
        /// </summary>
        [ObservableProperty]
        private bool _hasItems;

        /// <summary>
        /// 空状态提示文本
        /// </summary>
        [ObservableProperty]
        private string _emptyMessage = "暂无收藏，可在模组详情页添加收藏";

        public FavoritesViewModel(INavigationService navigation, IFavoriteService favoriteService)
        {
            Logger.Information("FavoritesViewModel 初始化开始");

            _navigation = navigation;
            _favoriteService = favoriteService;

            // 订阅收藏数据变更事件
            _favoriteService.Changed += OnFavoriteChanged;

            // 初始加载
            LoadCollections();

            Logger.Information("FavoritesViewModel 初始化完成");
        }

        /// <summary>
        /// 加载所有收藏合集并转换为UI展示模型
        /// </summary>
        private void LoadCollections()
        {
            try
            {
                IsLoading = true;

                var collections = _favoriteService.GetAllCollections();
                var groups = collections.Select(c => new FavoriteGroup
                {
                    CollectionId = c.Id,
                    CollectionName = c.Name,
                    Items = c.Items.Select(item => new FavoriteDisplayItem
                    {
                        Item = item,
                        DisplayName = item.Title,
                        DisplayDescription = item.Description,
                        DisplayTypeTag = FavoriteDisplayItem.GetTypeTag(item.ProjectType),
                        ProjectType = item.ProjectType,
                        FormattedDownloads = FavoriteDisplayItem.FormatDownloads(item.Downloads),
                        FormattedDate = item.FavoritedAt.ToLocalTime().ToString("yyyy-MM-dd")
                    }).ToList()
                }).ToList();

                FavoriteGroups = new ObservableCollection<FavoriteGroup>(groups);
                HasItems = FavoriteGroups.Any(g => g.Items.Any());

                Logger.Information("收藏夹加载完成，共 {Count} 个合集", groups.Count);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "加载收藏夹失败");
                EmptyMessage = "加载收藏夹失败";
                HasItems = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 收藏数据变更时刷新列表
        /// </summary>
        private void OnFavoriteChanged(object? sender, EventArgs e)
        {
            LoadCollections();
        }

        /// <summary>
        /// 导航到搜索页面（返回）
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            Logger.Information("从收藏夹返回搜索页面");
            _navigation.MainViewModel.NavigateToSearch();
        }

        /// <summary>
        /// 导航到模组详情页面
        /// </summary>
        [RelayCommand]
        private void NavigateToDetail(FavoriteDisplayItem? item)
        {
            if (item == null) return;

            Logger.Information("从收藏夹导航到模组详情: ProjectId={ProjectId}, Title={Title}", item.Item.ProjectId, item.DisplayName);
            _navigation.MainViewModel.NavigateToDetail(item.Item.ProjectId, item.DisplayName, item.DisplayDescription, item.Item.Slug);
        }

        /// <summary>
        /// 创建新收藏夹
        /// </summary>
        /// <param name="name">收藏夹名称</param>
        [RelayCommand]
        private void CreateCollection(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                var collection = _favoriteService.CreateCollection(name.Trim());
                Logger.Information("已创建新收藏夹: {Name}, Id={Id}", collection.Name, collection.Id);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建收藏夹失败: {Name}", name);
            }
            // Changed 事件会自动触发 LoadCollections
        }

        /// <summary>
        /// 从收藏中移除模组
        /// </summary>
        [RelayCommand]
        private void RemoveFromFavorites(FavoriteDisplayItem? item)
        {
            if (item == null) return;

            // 找到包含该模组的合集
            var collectionIds = _favoriteService.GetCollectionIdsContaining(item.Item.ProjectId);
            foreach (var collectionId in collectionIds)
            {
                _favoriteService.RemoveItem(collectionId, item.Item.ProjectId);
                Logger.Information("已从合集 {CollectionId} 移除模组 {ProjectId}", collectionId, item.Item.ProjectId);
            }
            // Changed 事件会自动触发 LoadCollections
        }
    }
}