using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MOneClickDownloads.App.Configs;
using MOneClickDownloads.App.DI;
using MOneClickDownloads.App.Models;
using MOneClickDownloads.App.Views;
using MOneClickDownloads.DataModel.Enums;
using MOneClickDownloads.DataModel.Favorites;
using MOneClickDownloads.DataModel.Version;
using MOneClickDownloads.Service;
using MOneClickDownloads.Service.Models;
using Serilog;

namespace MOneClickDownloads.App.ViewModels
{
    /// <summary>
    /// 合集下载页面 ViewModel，实现批量下载收藏合集中所有模组的功能。<br />
    /// <br />
    /// 核心流程：<br />
    /// 1. 接收藏集数据和保存目录<br />
    /// 2. 并发获取所有模组的版本信息<br />
    /// 3. 用户选择 MC版本 + 加载器后，计算兼容性状态<br />
    /// 4. 预检不兼容/仅预览版模组，弹窗让用户确认<br />
    /// 5. 逐个下载兼容模组（使用 ModDownloadService，含冲突检测和依赖递归）<br />
    /// </summary>
    public partial class CollectionDownloadViewModel : ViewModelBase
    {
        private readonly INavigationService _navigation;
        private readonly ModDownloadService _downloadService;
        private readonly ModrinthAPIService _apiService;
        private readonly ConfigService _configService;
        private readonly ILogger _logger;

        /// <summary>
        /// 合集名称
        /// </summary>
        [ObservableProperty]
        private string _collectionName = string.Empty;

        /// <summary>
        /// 保存目录
        /// </summary>
        [ObservableProperty]
        private string _saveDirectory = string.Empty;

        /// <summary>
        /// 合集内所有模组
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<FavoriteItem> _favoriteItems = new();

        /// <summary>
        /// 是否正在加载兼容性信息
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// 是否正在下载
        /// </summary>
        [ObservableProperty]
        private bool _isDownloading;

        /// <summary>
        /// 下载状态文本
        /// </summary>
        [ObservableProperty]
        private string _downloadStatus = string.Empty;

        /// <summary>
        /// 下载进度值（0-100）
        /// </summary>
        [ObservableProperty]
        private double _downloadProgressValue;

        /// <summary>
        /// 可选MC版本列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _availableMcVersions = new();

        /// <summary>
        /// 可选加载器列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _availableLoaders = new();

        /// <summary>
        /// 当前选中的MC版本过滤器
        /// </summary>
        [ObservableProperty]
        private string? _activeMcVersionFilter;

        /// <summary>
        /// 当前选中的加载器过滤器
        /// </summary>
        [ObservableProperty]
        private string? _activeLoaderFilter;

        /// <summary>
        /// 每个模组的兼容性状态列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CollectionModStatus> _modStatusList = new();

        /// <summary>
        /// 是否存在不兼容的模组
        /// </summary>
        [ObservableProperty]
        private bool _hasIncompatibleMods;

        /// <summary>
        /// 是否存在仅有预览版的模组
        /// </summary>
        [ObservableProperty]
        private bool _hasPreviewOnlyMods;

        /// <summary>
        /// 状态提示文本（加载/兼容性检查等阶段）
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 所有模组的版本缓存（projectId -> 版本列表）
        /// </summary>
        private Dictionary<string, List<ModrinthVersion>> _allVersionsCache = new();

        /// <summary>
        /// 从所有模组版本中提取的全部MC版本集合（用于构建可选列表）
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> _modMcVersions = new();

        /// <summary>
        /// 从所有模组版本中提取的全部加载器集合
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> _modLoaders = new();

        private CancellationTokenSource? _downloadCts;

        /// <summary>
        /// 构造合集下载 ViewModel。
        /// </summary>
        /// <param name="navigation">导航服务（DI 注入）</param>
        /// <param name="downloadService">模组下载服务（DI 注入）</param>
        /// <param name="apiService">Modrinth API 服务（DI 注入）</param>
        /// <param name="configService">应用配置服务（DI 注入）</param>
        /// <param name="collection">要下载的收藏合集</param>
        /// <param name="saveDirectory">保存目录</param>
        public CollectionDownloadViewModel(
            INavigationService navigation,
            ModDownloadService downloadService,
            ModrinthAPIService apiService,
            ConfigService configService,
            FavoriteCollection collection,
            string saveDirectory)
        {
            _navigation = navigation;
            _downloadService = downloadService;
            _apiService = apiService;
            _configService = configService;
            _logger = Log.ForContext<CollectionDownloadViewModel>();

            _collectionName = collection.Name;
            _saveDirectory = saveDirectory;

            foreach (var item in collection.Items)
            {
                FavoriteItems.Add(item);
            }

            _logger.Information("CollectionDownloadViewModel 初始化: CollectionName={Name}, SavePath={Path}, ModCount={Count}",
                collection.Name, saveDirectory, collection.Items.Count);

            // 启动兼容性加载
            LoadCompatibilityCommand.Execute(null);
        }

        /// <summary>
        /// 加载所有模组的版本兼容性信息
        /// </summary>
        [RelayCommand]
        private async Task LoadCompatibilityAsync()
        {
            _logger.Information("开始加载合集中所有模组的版本信息");
            IsLoading = true;
            StatusMessage = "正在加载模组版本信息...";
            _allVersionsCache.Clear();
            _modMcVersions.Clear();
            _modLoaders.Clear();

            try
            {
                // 并发获取所有模组的版本
                var tasks = FavoriteItems.Select(async item =>
                {
                    try
                    {
                        var versions = await _apiService.GetProjectVersionsAsync(item.ProjectId);
                        _logger.Debug("加载模组版本成功: ProjectId={Id}, Name={Name}, VersionCount={Count}",
                            item.ProjectId, item.Title, versions.Count);

                        // 提取该模组支持的所有MC版本和加载器
                        var mcVersions = new HashSet<string>();
                        var loaders = new HashSet<string>();
                        foreach (var v in versions)
                        {
                            foreach (var gv in v.GameVersions)
                                mcVersions.Add(gv);
                            foreach (var l in v.Loaders)
                                loaders.Add(l);
                        }

                        return (item.ProjectId, versions, mcVersions, loaders);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "加载模组版本失败: ProjectId={Id}, Name={Name}", item.ProjectId, item.Title);
                        return (item.ProjectId, new List<ModrinthVersion>(), new HashSet<string>(), new HashSet<string>());
                    }
                });

                var results = await Task.WhenAll(tasks);

                foreach (var (projectId, versions, mcVersions, loaders) in results)
                {
                    _allVersionsCache[projectId] = versions;
                    _modMcVersions[projectId] = mcVersions;
                    _modLoaders[projectId] = loaders;
                }

                // 构建可选MC版本列表（取所有模组支持的并集）
                var allMcVersions = _modMcVersions.Values
                    .SelectMany(s => s)
                    .Distinct()
                    .OrderByDescending(v => v, new McVersionComparer())
                    .ToList();

                AvailableMcVersions.Clear();
                foreach (var v in allMcVersions)
                    AvailableMcVersions.Add(v);

                // 构建可选加载器列表
                var allLoaders = _modLoaders.Values
                    .SelectMany(s => s)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToList();

                AvailableLoaders.Clear();
                foreach (var l in allLoaders)
                    AvailableLoaders.Add(l);

                // 从配置中恢复 MC 版本过滤器
                var savedMcVersion = _configService.Get<string>(ConfigKeys.ActiveMcVersionFilter);
                if (!string.IsNullOrEmpty(savedMcVersion) && allMcVersions.Contains(savedMcVersion))
                {
                    ActiveMcVersionFilter = savedMcVersion;
                    _logger.Information("从配置恢复 MC 版本过滤器: {McVersion}", savedMcVersion);
                }

                StatusMessage = "请选择 Minecraft 版本和加载器";
                _logger.Information("版本信息加载完成: MC版本数={McCount}, 加载器数={LoaderCount}",
                    allMcVersions.Count, allLoaders.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载版本信息失败");
                StatusMessage = $"加载失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnActiveMcVersionFilterChanged(string? value)
        {
            // 持久化 MC 版本过滤器
            if (string.IsNullOrEmpty(value))
                _configService.Remove(ConfigKeys.ActiveMcVersionFilter);
            else
                _configService.Set(ConfigKeys.ActiveMcVersionFilter, value);

            RefreshCompatibility();
        }

        partial void OnActiveLoaderFilterChanged(string? value)
        {
            RefreshCompatibility();
        }

        /// <summary>
        /// 刷新兼容性状态
        /// </summary>
        private void RefreshCompatibility()
        {
            if (string.IsNullOrEmpty(ActiveMcVersionFilter) || string.IsNullOrEmpty(ActiveLoaderFilter))
            {
                ModStatusList.Clear();
                HasIncompatibleMods = false;
                HasPreviewOnlyMods = false;
                StatusMessage = "请选择 Minecraft 版本和加载器";
                return;
            }

            _logger.Information("刷新兼容性: MC={Mc}, Loader={Loader}", ActiveMcVersionFilter, ActiveLoaderFilter);

            var statusList = new List<CollectionModStatus>();

            foreach (var item in FavoriteItems)
            {
                var status = EvaluateModCompatibility(item);
                statusList.Add(status);
            }

            ModStatusList = new ObservableCollection<CollectionModStatus>(statusList);
            HasIncompatibleMods = statusList.Any(s => s.Status == ModCompatibilityStatus.Incompatible);
            HasPreviewOnlyMods = statusList.Any(s => s.Status == ModCompatibilityStatus.PreviewOnly);

            var compatible = statusList.Count(s => s.Status == ModCompatibilityStatus.Compatible);
            var preview = statusList.Count(s => s.Status == ModCompatibilityStatus.PreviewOnly);
            var incompatible = statusList.Count(s => s.Status == ModCompatibilityStatus.Incompatible);

            StatusMessage = $"兼容: {compatible} 个 | 仅预览版: {preview} 个 | 不兼容: {incompatible} 个";

            _logger.Information("兼容性检查完成: Compatible={C}, PreviewOnly={P}, Incompatible={I}",
                compatible, preview, incompatible);
        }

        /// <summary>
        /// 评估单个模组的兼容性
        /// </summary>
        private CollectionModStatus EvaluateModCompatibility(FavoriteItem item)
        {
            var status = new CollectionModStatus
            {
                ModName = item.Title,
                ProjectId = item.ProjectId,
                Slug = item.Slug
            };

            if (!_allVersionsCache.TryGetValue(item.ProjectId, out var allVersions) || allVersions.Count == 0)
            {
                status.Status = ModCompatibilityStatus.Incompatible;
                status.StatusText = "无法获取版本信息";
                return status;
            }

            // 筛选兼容 MC版本 + 加载器的版本
            var compatibleVersions = allVersions
                .Where(v => v.GameVersions.Contains(ActiveMcVersionFilter!)
                         && v.Loaders.Contains(ActiveLoaderFilter!, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (compatibleVersions.Count == 0)
            {
                // 检查是否仅缺少加载器
                var hasMcVersion = allVersions.Any(v => v.GameVersions.Contains(ActiveMcVersionFilter!));
                if (!hasMcVersion)
                {
                    status.Status = ModCompatibilityStatus.Incompatible;
                    status.StatusText = $"没有 {ActiveMcVersionFilter} 的版本";
                }
                else
                {
                    status.Status = ModCompatibilityStatus.Incompatible;
                    status.StatusText = $"没有 {ActiveLoaderFilter} 加载器的版本";
                }
                return status;
            }

            // 优先选择 Release 版本
            var releaseVersion = compatibleVersions
                .Where(v => v.VersionType == VersionType.Release)
                .OrderByDescending(v => v.DatePublished)
                .FirstOrDefault();

            if (releaseVersion != null)
            {
                status.Status = ModCompatibilityStatus.Compatible;
                status.BestVersion = releaseVersion;
                var name = string.IsNullOrEmpty(releaseVersion.Name) ? releaseVersion.VersionNumber : releaseVersion.Name;
                status.StatusText = $"✓ {name} (Release)";
                return status;
            }

            // 无 Release，选最佳可用版本
            var bestVersion = SelectBestVersion(compatibleVersions);
            status.Status = ModCompatibilityStatus.PreviewOnly;
            status.BestVersion = bestVersion;
            var bestName = string.IsNullOrEmpty(bestVersion.Name) ? bestVersion.VersionNumber : bestVersion.Name;
            status.StatusText = $"⚠ {bestName} ({bestVersion.VersionType})";

            return status;
        }

        /// <summary>
        /// 从版本列表中选取最佳版本（优先 Release > Beta > Alpha）
        /// </summary>
        private static ModrinthVersion SelectBestVersion(List<ModrinthVersion> versions)
        {
            var release = versions.Where(v => v.VersionType == VersionType.Release)
                .OrderByDescending(v => v.DatePublished).FirstOrDefault();
            if (release != null) return release;

            var beta = versions.Where(v => v.VersionType == VersionType.Beta)
                .OrderByDescending(v => v.DatePublished).FirstOrDefault();
            if (beta != null) return beta;

            return versions.OrderByDescending(v => v.DatePublished).First();
        }

        /// <summary>
        /// 切换MC版本过滤器
        /// </summary>
        [RelayCommand]
        private void ToggleMcVersionFilter(string? mcVersion)
        {
            if (string.IsNullOrEmpty(mcVersion))
                ActiveMcVersionFilter = null;
            else if (ActiveMcVersionFilter == mcVersion)
                ActiveMcVersionFilter = null;
            else
                ActiveMcVersionFilter = mcVersion;
        }

        /// <summary>
        /// 切换加载器过滤器
        /// </summary>
        [RelayCommand]
        private void ToggleLoaderFilter(string? loader)
        {
            if (string.IsNullOrEmpty(loader))
                ActiveLoaderFilter = null;
            else if (ActiveLoaderFilter == loader)
                ActiveLoaderFilter = null;
            else
                ActiveLoaderFilter = loader;
        }

        /// <summary>
        /// 开始下载
        /// </summary>
        [RelayCommand]
        private async Task StartDownloadAsync()
        {
            if (string.IsNullOrEmpty(ActiveMcVersionFilter) || string.IsNullOrEmpty(ActiveLoaderFilter))
            {
                StatusMessage = "请先选择 Minecraft 版本和加载器";
                return;
            }

            // 如果没有刷新过兼容性状态，先刷新
            if (ModStatusList.Count == 0)
            {
                RefreshCompatibility();
            }

            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            // 【预检1】不兼容模组
            var incompatibleMods = ModStatusList
                .Where(s => s.Status == ModCompatibilityStatus.Incompatible)
                .ToList();

            if (incompatibleMods.Count > 0)
            {
                var warningDialog = new CollectionWarningDialog();
                warningDialog.SetWarningInfo(
                    "部分模组不兼容",
                    $"以下 {incompatibleMods.Count} 个模组在 {ActiveMcVersionFilter} + {ActiveLoaderFilter} 下没有兼容版本，将被跳过：",
                    incompatibleMods);

                var result = await warningDialog.ShowDialog<bool?>(mainWindow);
                if (result != true)
                {
                    StatusMessage = "下载已取消";
                    return;
                }
            }

            // 【预检2】仅预览版模组
            var previewOnlyMods = ModStatusList
                .Where(s => s.Status == ModCompatibilityStatus.PreviewOnly)
                .ToList();

            if (previewOnlyMods.Count > 0)
            {
                var warningDialog = new CollectionWarningDialog();
                warningDialog.SetWarningInfo(
                    "部分模组仅有预览版",
                    $"以下 {previewOnlyMods.Count} 个模组没有 Release 版本，将使用最佳可用版本（Beta/Alpha）：",
                    previewOnlyMods);

                var result = await warningDialog.ShowDialog<bool?>(mainWindow);
                if (result != true)
                {
                    StatusMessage = "下载已取消";
                    return;
                }
            }

            // 开始下载
            var modsToDownload = ModStatusList
                .Where(s => s.Status != ModCompatibilityStatus.Incompatible && s.BestVersion != null)
                .ToList();

            if (modsToDownload.Count == 0)
            {
                StatusMessage = "没有可下载的模组";
                return;
            }

            await ExecuteDownloadAsync(modsToDownload);
        }

        /// <summary>
        /// 执行批量下载
        /// </summary>
        private async Task ExecuteDownloadAsync(List<CollectionModStatus> modsToDownload)
        {
            _logger.Information("开始批量下载: 模组数={Count}, MC={Mc}, Loader={Loader}, SavePath={Path}",
                modsToDownload.Count, ActiveMcVersionFilter, ActiveLoaderFilter, SaveDirectory);

            IsDownloading = true;
            DownloadStatus = "准备下载...";
            DownloadProgressValue = 0;
            _downloadCts = new CancellationTokenSource();

            var totalMods = modsToDownload.Count;
            var completedMods = 0;
            var failedMods = new List<string>();

            // 构建冲突回调
            ModConflictCallback conflictCallback = async (conflictInfo) =>
            {
                _logger.Information("弹出冲突对话框: ModId={ModId}, ConflictType={Type}",
                    conflictInfo.ModId, conflictInfo.ConflictType);

                var resolution = await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                    var win = desktop?.MainWindow;
                    if (win == null)
                    {
                        _logger.Warning("无法获取主窗口，使用默认 Skip 策略");
                        return ModConflictResolution.Skip;
                    }

                    var dialog = new ModConflictDialog();
                    dialog.SetConflictInfo(conflictInfo);
                    var result = await dialog.ShowDialog<ModConflictResolution>(win);
                    _logger.Information("用户选择冲突解决策略: {Resolution}", result);
                    return result;
                });

                return resolution;
            };

            var stopwatch = Stopwatch.StartNew();

            foreach (var modStatus in modsToDownload)
            {
                if (_downloadCts.Token.IsCancellationRequested)
                    break;

                var currentMod = modStatus;
                completedMods++;

                DownloadStatus = $"[{completedMods}/{totalMods}] 正在下载: {currentMod.ModName}";
                DownloadProgressValue = (double)(completedMods - 1) / totalMods * 100;

                _logger.Information("开始下载模组 [{Completed}/{Total}]: {Name} (ProjectId: {Id})",
                    completedMods, totalMods, currentMod.ModName, currentMod.ProjectId);

                try
                {
                    var progress = new Progress<DownloadProgress>(p =>
                    {
                        // 子进度：当前模组内的文件下载进度
                        var baseProgress = (double)(completedMods - 1) / totalMods * 100;
                        var subProgress = p.Percentage / totalMods;
                        DownloadProgressValue = baseProgress + subProgress;
                        DownloadStatus = $"[{completedMods}/{totalMods}] {currentMod.ModName}: {p.CurrentFileName}";
                    });

                    var gameVersion = ActiveMcVersionFilter!;
                    var loader = ActiveLoaderFilter!;

                    // 首先尝试推荐下载（Release 版本）
                    var results = await _downloadService.DownloadRecommendedAsync(
                        currentMod.ProjectId,
                        gameVersion,
                        loader,
                        SaveDirectory,
                        progress,
                        _downloadCts.Token,
                        conflictCallback,
                        currentMod.Slug,
                        currentMod.ModName);

                    // 如果推荐下载返回空（无 Release），且我们有最佳版本（预览版），则下载它
                    if (results.Count == 0 && currentMod.Status == ModCompatibilityStatus.PreviewOnly && currentMod.BestVersion != null)
                    {
                        _logger.Information("无 Release 版本，使用预览版下载: {Name} ({Type})",
                            currentMod.ModName, currentMod.BestVersion.VersionType);

                        results = await _downloadService.DownloadByVersionAsync(
                            currentMod.ProjectId,
                            gameVersion,
                            loader,
                            currentMod.BestVersion.VersionType,
                            SaveDirectory,
                            progress,
                            _downloadCts.Token,
                            conflictCallback,
                            currentMod.Slug,
                            currentMod.ModName);
                    }

                    if (results.Count > 0)
                    {
                        _logger.Information("模组下载成功: {Name}, 文件数={Count}", currentMod.ModName, results.Count);
                    }
                    else
                    {
                        _logger.Warning("模组下载结果为空: {Name}", currentMod.ModName);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.Warning("下载被用户取消");
                    DownloadStatus = "下载已取消";
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "模组下载失败: {Name}", currentMod.ModName);
                    failedMods.Add(currentMod.ModName);
                }
            }

            stopwatch.Stop();

            if (!_downloadCts.Token.IsCancellationRequested)
            {
                DownloadProgressValue = 100;
                if (failedMods.Count > 0)
                {
                    DownloadStatus = $"下载完成（{failedMods.Count} 个失败），用时 {stopwatch.Elapsed.TotalSeconds:F1} 秒";
                    _logger.Warning("批量下载完成（有失败）: 成功={Success}, 失败={Failed}, 失败列表={List}",
                        totalMods - failedMods.Count, failedMods.Count, string.Join(", ", failedMods));
                }
                else
                {
                    DownloadStatus = $"全部下载完成，用时 {stopwatch.Elapsed.TotalSeconds:F1} 秒，共 {totalMods} 个模组";
                    _logger.Information("批量下载全部完成: 模组数={Count}, 耗时={Elapsed}秒", totalMods, stopwatch.Elapsed.TotalSeconds);
                }
            }

            IsDownloading = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }

        /// <summary>
        /// 取消下载
        /// </summary>
        [RelayCommand]
        private void CancelDownload()
        {
            _logger.Information("用户请求取消合集下载");
            _downloadCts?.Cancel();
        }

        /// <summary>
        /// 返回收藏夹页面
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            _logger.Information("从合集下载页返回收藏夹");
            if (IsDownloading)
            {
                _downloadCts?.Cancel();
            }
            _navigation.MainViewModel.NavigateToFavorites();
        }

        /// <summary>
        /// 获取主窗口引用
        /// </summary>
        private static Window? GetMainWindow()
        {
            return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
        }
    }
}