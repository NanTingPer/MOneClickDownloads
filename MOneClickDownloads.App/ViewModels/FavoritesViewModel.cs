using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MOneClickDownloads.App.DI;
using MOneClickDownloads.App.Models;
using MOneClickDownloads.App.Views;
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
                        IconUrl = item.IconUrl,
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

        /// <summary>
        /// 重命名收藏夹：弹出对话框让用户输入新名称
        /// </summary>
        /// <param name="group">要重命名的收藏夹分组</param>
        [RelayCommand]
        private async Task RenameCollectionAsync(FavoriteGroup? group)
        {
            if (group == null) return;

            Logger.Information("用户请求重命名收藏夹: CollectionId={Id}, CurrentName={Name}", group.CollectionId, group.CollectionName);

            // 获取主窗口作为对话框所有者
            var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (owner == null) return;

            // 打开重命名对话框（复用 CreateCollectionDialog）
            var dialog = new CreateCollectionDialog(isRename: true, currentName: group.CollectionName);
            var result = await dialog.ShowDialog<string?>(owner);

            if (string.IsNullOrWhiteSpace(result) || result.Trim() == group.CollectionName)
                return;

            try
            {
                _favoriteService.RenameCollection(group.CollectionId, result.Trim());
                Logger.Information("已重命名收藏夹: {OldName} -> {NewName}", group.CollectionName, result.Trim());
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "重命名收藏夹失败: CollectionId={Id}", group.CollectionId);
            }
            // Changed 事件会自动触发 LoadCollections
        }

        /// <summary>
        /// 下载合集：弹出文件夹选择对话框后导航到合集下载页
        /// </summary>
        [RelayCommand]
        private async Task DownloadCollectionAsync(FavoriteGroup? group)
        {
            if (group == null) return;

            Logger.Information("用户请求下载合集: CollectionName={Name}, CollectionId={Id}", group.CollectionName, group.CollectionId);

            // 获取主窗口作为 TopLevel
            var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null) return;

            // 打开文件夹选择对话框
            var folderPath = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择合集下载位置",
                AllowMultiple = false
            });

            if (folderPath.Count == 0) return;

            var savePath = folderPath[0].Path.LocalPath;
            Logger.Information("用户选择保存目录: {SavePath}", savePath);

            // 获取原始 FavoriteCollection 对象
            var collection = _favoriteService.GetCollection(group.CollectionId);
            if (collection == null)
            {
                Logger.Error("找不到合集: CollectionId={Id}", group.CollectionId);
                return;
            }

            // 导航到合集下载页
            _navigation.MainViewModel.NavigateToCollectionDownload(collection, savePath);
        }
    }
}
