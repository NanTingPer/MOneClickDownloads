using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using MOneClickDownloads.DataModel.Version;
using MOneClickDownloads.Service;
using MOneClickDownloads.Service.Models;
using Serilog;

namespace MOneClickDownloads.App.ViewModels
{
    /// <summary>
    /// 本地模组管理页面 ViewModel。<br />
    /// <br />
    /// 核心流程：<br />
    /// 1. 管理已记录的本地 mods 文件夹列表（支持重命名、版本元数据持久化）<br />
    /// 2. 选中文件夹后扫描本地 JAR 文件，展示模组列表<br />
    /// 3. 用户设置 MC版本 + 加载器过滤器后，可点击"检查更新"对比版本<br />
    /// 4. 点击"更新全部"批量更新有新版本的模组（保持原文件名）<br />
    /// 5. 支持删除本地模组文件<br />
    /// </summary>
    public partial class LocalModsViewModel : ViewModelBase
    {
        private static readonly ILogger Logger = Log.ForContext<LocalModsViewModel>();

        private readonly INavigationService _navigation;
        private readonly ILocalModFolderService _folderService;
        private readonly ModrinthAPIService _apiService;
        private readonly IModAnalysisService _analysisService;
        private readonly ModDownloadService _downloadService;
        private readonly ConfigService _configService;
        private readonly IIconCacheService _iconCacheService;

        /// <summary>
        /// 已记录的文件夹列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<LocalModFolder> _folders = new();

        /// <summary>
        /// 当前选中的文件夹
        /// </summary>
        [ObservableProperty]
        private LocalModFolder? _selectedFolder;

        /// <summary>
        /// 右侧模组列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<LocalModDisplayItem> _modItems = new();

        /// <summary>
        /// 是否正在加载/扫描
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// 是否正在检查更新
        /// </summary>
        [ObservableProperty]
        private bool _isCheckingUpdate;

        /// <summary>
        /// 是否正在执行更新
        /// </summary>
        [ObservableProperty]
        private bool _isUpdating;

        /// <summary>
        /// 是否已有模组数据（用于空状态显示）
        /// </summary>
        [ObservableProperty]
        private bool _hasItems;

        /// <summary>
        /// 空状态提示文本
        /// </summary>
        [ObservableProperty]
        private string _emptyMessage = "请选择左侧文件夹查看模组";

        /// <summary>
        /// 状态文本
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 更新进度值（0-100）
        /// </summary>
        [ObservableProperty]
        private double _updateProgressValue;

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
        /// 是否正在刷新筛选元数据
        /// </summary>
        [ObservableProperty]
        private bool _isRefreshingMetadata;

        private CancellationTokenSource? _updateCts;

        /// <summary>
        /// 缓存的版本信息（projectId -> 版本列表）
        /// </summary>
        private Dictionary<string, List<ModrinthVersion>> _allVersionsCache = new();

        public LocalModsViewModel(
            INavigationService navigation,
            ILocalModFolderService folderService,
            ModrinthAPIService apiService,
            IModAnalysisService analysisService,
            ModDownloadService downloadService,
            ConfigService configService,
            IIconCacheService iconCacheService)
        {
            Logger.Information("LocalModsViewModel 初始化开始");

            _navigation = navigation;
            _folderService = folderService;
            _apiService = apiService;
            _analysisService = analysisService;
            _downloadService = downloadService;
            _configService = configService;
            _iconCacheService = iconCacheService;

            // 订阅文件夹变更事件
            _folderService.Changed += OnFolderChanged;

            // 初始加载文件夹列表
            LoadFolders();

            Logger.Information("LocalModsViewModel 初始化完成");
        }

        /// <summary>
        /// 加载文件夹列表
        /// </summary>
        private void LoadFolders()
        {
            var entries = _folderService.GetAllFolders();
            Folders = new ObservableCollection<LocalModFolder>(
                entries.Select(e => new LocalModFolder
                {
                    FolderPath = e.FolderPath,
                    CustomName = e.CustomName,
                    AvailableMcVersions = e.AvailableMcVersions ?? new List<string>(),
                    AvailableLoaders = e.AvailableLoaders ?? new List<string>(),
                    MetadataProjectId = e.MetadataProjectId,
                    MetadataModName = e.MetadataModName
                }));
            Logger.Information("文件夹列表加载完成: {Count} 个", Folders.Count);
        }

        /// <summary>
        /// 文件夹变更事件处理
        /// </summary>
        private void OnFolderChanged(object? sender, EventArgs e)
        {
            // 保存当前选中的路径，重新加载后恢复选中
            var selectedPath = SelectedFolder?.FolderPath;
            LoadFolders();

            if (selectedPath != null)
            {
                SelectedFolder = Folders.FirstOrDefault(f =>
                    string.Equals(f.FolderPath, selectedPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// 选中文件夹变更时扫描本地模组，并恢复持久化的筛选器
        /// </summary>
        partial void OnSelectedFolderChanged(LocalModFolder? value)
        {
            if (value != null)
            {
                // 恢复持久化的筛选元数据
                RestoreFilterMetadata(value);
                ScanFolderCommand.Execute(null);
            }
            else
            {
                ModItems.Clear();
                HasItems = false;
                EmptyMessage = "请选择左侧文件夹查看模组";
                AvailableMcVersions.Clear();
                AvailableLoaders.Clear();
            }
        }

        /// <summary>
        /// 恢复文件夹持久化的筛选元数据
        /// </summary>
        private void RestoreFilterMetadata(LocalModFolder folder)
        {
            AvailableMcVersions.Clear();
            foreach (var v in folder.AvailableMcVersions)
                AvailableMcVersions.Add(v);

            AvailableLoaders.Clear();
            foreach (var l in folder.AvailableLoaders)
                AvailableLoaders.Add(l);

            if (folder.AvailableMcVersions.Count > 0 || folder.AvailableLoaders.Count > 0)
            {
                Logger.Information("已恢复文件夹筛选元数据: MC版本={McCount}, 加载器={LoaderCount}",
                    folder.AvailableMcVersions.Count, folder.AvailableLoaders.Count);
            }
        }

        partial void OnActiveMcVersionFilterChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
                _configService.Set(ConfigKeys.ActiveMcVersionFilter, value);

            ResetUpdateStatus();
        }

        partial void OnActiveLoaderFilterChanged(string? value)
        {
            ResetUpdateStatus();
        }

        /// <summary>
        /// 重置更新检查状态
        /// </summary>
        private void ResetUpdateStatus()
        {
            foreach (var item in ModItems)
            {
                item.UpdateStatus = LocalModUpdateStatus.Unknown;
                item.UpdateStatusText = string.Empty;
                item.LatestVersion = null;
            }
        }

        /// <summary>
        /// 扫描选中的文件夹
        /// </summary>
        [RelayCommand]
        private async Task ScanFolderAsync()
        {
            if (SelectedFolder == null) return;

            Logger.Information("开始扫描文件夹: {Path}", SelectedFolder.FolderPath);
            IsLoading = true;
            HasItems = false;
            EmptyMessage = "正在扫描本地模组...";
            ModItems.Clear();
            ResetUpdateStatus();

            try
            {
                var inventory = new LocalModInventory(SelectedFolder.FolderPath, _analysisService);
                await inventory.ScanAsync();

                var items = inventory.InstalledMods.Select(mod => new LocalModDisplayItem
                {
                    ModId = mod.ModId,
                    DisplayName = mod.Name,
                    DisplayDescription = string.Empty,
                    CurrentVersion = mod.Version,
                    FilePath = inventory.GetModFilePath(mod.ModId) ?? string.Empty,
                }).ToList();

                ModItems = new ObservableCollection<LocalModDisplayItem>(items);
                HasItems = items.Count > 0;
                SelectedFolder.ModCount = items.Count;

                if (!HasItems)
                {
                    EmptyMessage = "此文件夹中没有识别到模组文件";
                }

                // 异步补充项目信息（图标、项目ID等）
                _ = Task.Run(async () => await EnrichModInfoAsync(items));

                // 构建加载器选项（从本地 JAR 的 LoaderType 提取）
                // 仅当持久化的加载器列表为空时才从本地提取
                if (SelectedFolder.AvailableLoaders.Count == 0)
                {
                    // 始终包含所有常见加载器类型，确保用户可以选择任意加载器
                    var allLoaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "fabric", "forge", "neoforge", "quilt"
                    };

                    // 合并本地 JAR 中检测到的加载器类型
                    foreach (var mod in inventory.InstalledMods)
                    {
                        if (mod.LoaderType != ModLoaderType.Unknown)
                            allLoaders.Add(mod.LoaderType.ToString().ToLowerInvariant());
                    }

                    var loaders = allLoaders.OrderBy(l => l).ToList();

                    AvailableLoaders.Clear();
                    foreach (var l in loaders)
                        AvailableLoaders.Add(l);
                }

                Logger.Information("文件夹扫描完成: {Count} 个模组", items.Count);
                StatusMessage = $"共识别 {items.Count} 个模组";
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "扫描文件夹失败: {Path}", SelectedFolder.FolderPath);
                EmptyMessage = $"扫描失败: {ex.Message}";
                HasItems = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 异步补充模组项目信息（通过 Modrinth API 搜索）
        /// </summary>
        private async Task EnrichModInfoAsync(List<LocalModDisplayItem> items)
        {
            Logger.Information("开始异步补充模组项目信息: {Count} 个", items.Count);

            foreach (var item in items)
            {
                try
                {
                    // 用模组名称搜索 Modrinth
                    var searchResult = await _apiService.SearchProjectsAsync(
                        item.DisplayName, null, null, 0, 5);

                    var hit = searchResult.Hits
                        .FirstOrDefault(h =>
                            string.Equals(h.Title, item.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(h.Slug, item.ModId, StringComparison.OrdinalIgnoreCase));

                    if (hit != null)
                    {
                        // 优先使用本地缓存图标
                        var cachedIconPath = !string.IsNullOrEmpty(hit.IconUrl)
                            ? _iconCacheService.GetCachedIconPath(item.ModId, hit.IconUrl)
                            : null;

                        var iconToSet = cachedIconPath ?? hit.IconUrl;

                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            item.ProjectId = hit.ProjectId;
                            item.Slug = hit.Slug;
                            item.IconUrl = iconToSet;
                            item.Downloads = hit.Downloads;
                            item.ProjectType = hit.ProjectType;
                            if (!string.IsNullOrEmpty(hit.Description))
                                item.DisplayDescription = hit.Description;
                        });

                        // 后台缓存/更新图标（无论是否已缓存，都执行以更新缓存）
                        if (!string.IsNullOrEmpty(hit.IconUrl))
                        {
                            _ = Task.Run(async () => await _iconCacheService.CacheIconAsync(item.ModId, hit.IconUrl));
                        }

                        Logger.Debug("补充模组信息成功: {ModId} -> ProjectId={ProjectId}", item.ModId, hit.ProjectId);
                    }
                    else
                    {
                        Logger.Debug("未能在 Modrinth 找到模组: {ModId} ({Name})", item.ModId, item.DisplayName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "补充模组信息失败: {ModId}", item.ModId);
                }
            }

            Logger.Information("模组项目信息补充完成");
        }

        /// <summary>
        /// 添加文件夹
        /// </summary>
        [RelayCommand]
        private async Task AddFolderAsync()
        {
            var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null) return;

            var folderPath = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择 Mods 文件夹",
                AllowMultiple = false
            });

            if (folderPath.Count == 0) return;

            var path = folderPath[0].Path.LocalPath;
            Logger.Information("用户选择添加文件夹: {Path}", path);

            var added = _folderService.AddFolder(path);
            if (!added)
            {
                StatusMessage = "此文件夹已在列表中";
            }
        }

        /// <summary>
        /// 移除文件夹记录
        /// </summary>
        [RelayCommand]
        private void RemoveFolder(LocalModFolder? folder)
        {
            if (folder == null) return;

            Logger.Information("移除文件夹记录: {Path}", folder.FolderPath);
            _folderService.RemoveFolder(folder.FolderPath);

            if (SelectedFolder?.FolderPath == folder.FolderPath)
            {
                SelectedFolder = null;
            }
        }

        /// <summary>
        /// 重命名文件夹（弹出输入对话框）
        /// </summary>
        [RelayCommand]
        private async Task RenameFolderAsync(LocalModFolder? folder)
        {
            if (folder == null) return;

            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            var dialog = new CreateCollectionDialog(true, folder.DisplayName);
            var newName = await dialog.ShowDialog<string?>(mainWindow);

            if (string.IsNullOrEmpty(newName)) return;

            Logger.Information("用户重命名文件夹: {Path} -> {Name}", folder.FolderPath, newName);
            _folderService.RenameFolder(folder.FolderPath, newName);

            // 立即更新 UI（不等待 Changed 事件）
            folder.CustomName = newName;
        }

        /// <summary>
        /// 切换MC版本过滤器
        /// </summary>
        [RelayCommand]
        private void ToggleMcVersionFilter(string? mcVersion)
        {
            ActiveMcVersionFilter = ActiveMcVersionFilter == mcVersion ? null : mcVersion;
        }

        /// <summary>
        /// 切换加载器过滤器
        /// </summary>
        [RelayCommand]
        private void ToggleLoaderFilter(string? loader)
        {
            ActiveLoaderFilter = ActiveLoaderFilter == loader ? null : loader;
        }

        /// <summary>
        /// 更新筛选元数据（通过 API 获取版本信息并持久化）
        /// </summary>
        [RelayCommand]
        private async Task RefreshFilterMetadataAsync()
        {
            if (SelectedFolder == null)
            {
                StatusMessage = "请先选择一个文件夹";
                return;
            }

            Logger.Information("开始更新筛选元数据: {Path}", SelectedFolder.FolderPath);
            IsRefreshingMetadata = true;
            StatusMessage = "正在更新筛选元数据...";

            try
            {
                // 寻找一个已有 ProjectId 的模组
                var referenceItem = ModItems.FirstOrDefault(m => !string.IsNullOrEmpty(m.ProjectId));

                if (referenceItem == null)
                {
                    // 如果还没有 ProjectId，尝试先补充一个
                    StatusMessage = "正在识别模组信息...";
                    foreach (var item in ModItems.Take(5))
                    {
                        try
                        {
                            var searchResult = await _apiService.SearchProjectsAsync(
                                item.DisplayName, null, null, 0, 5);

                            var hit = searchResult.Hits
                                .FirstOrDefault(h =>
                                    string.Equals(h.Title, item.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(h.Slug, item.ModId, StringComparison.OrdinalIgnoreCase));

                            if (hit != null)
                            {
                                // 优先使用本地缓存图标
                                var cachedPath = !string.IsNullOrEmpty(hit.IconUrl)
                                    ? _iconCacheService.GetCachedIconPath(item.ModId, hit.IconUrl)
                                    : null;

                                item.ProjectId = hit.ProjectId;
                                item.Slug = hit.Slug;
                                item.IconUrl = cachedPath ?? hit.IconUrl;
                                item.Downloads = hit.Downloads;
                                item.ProjectType = hit.ProjectType;
                                if (!string.IsNullOrEmpty(hit.Description))
                                    item.DisplayDescription = hit.Description;

                                // 后台缓存/更新图标
                                if (!string.IsNullOrEmpty(hit.IconUrl))
                                {
                                    _ = Task.Run(async () => await _iconCacheService.CacheIconAsync(item.ModId, hit.IconUrl));
                                }

                                referenceItem = item;
                                Logger.Information("识别到参考模组: {Name} -> ProjectId={ProjectId}", item.DisplayName, hit.ProjectId);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning(ex, "识别模组失败: {Name}", item.DisplayName);
                        }
                    }
                }

                if (referenceItem == null || string.IsNullOrEmpty(referenceItem.ProjectId))
                {
                    StatusMessage = "未能识别任何模组，无法获取版本元数据";
                    Logger.Warning("未能找到可用于获取版本元数据的参考模组");
                    return;
                }

                StatusMessage = $"正在获取 {referenceItem.DisplayName} 的版本信息...";
                var versions = await _apiService.GetProjectVersionsAsync(referenceItem.ProjectId);

                // 提取 MC 版本列表
                var allMcVersions = new HashSet<string>();
                foreach (var v in versions)
                    foreach (var gv in v.GameVersions)
                        allMcVersions.Add(gv);

                var sortedMcVersions = allMcVersions
                    .OrderByDescending(v => v, new McVersionComparer())
                    .ToList();

                // 提取加载器列表（始终包含所有常见加载器类型）
                var allLoaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "fabric", "forge", "neoforge", "quilt"
                };
                foreach (var v in versions)
                    foreach (var loader in v.Loaders)
                        allLoaders.Add(loader.ToLowerInvariant());

                var sortedLoaders = allLoaders.OrderBy(l => l).ToList();

                // 更新 UI
                AvailableMcVersions.Clear();
                foreach (var v in sortedMcVersions)
                    AvailableMcVersions.Add(v);

                AvailableLoaders.Clear();
                foreach (var l in sortedLoaders)
                    AvailableLoaders.Add(l);

                // 持久化到文件夹模型
                _folderService.UpdateFolderMetadata(
                    SelectedFolder.FolderPath,
                    sortedMcVersions,
                    sortedLoaders,
                    referenceItem.ProjectId,
                    referenceItem.DisplayName);

                // 更新本地模型
                SelectedFolder.AvailableMcVersions = sortedMcVersions;
                SelectedFolder.AvailableLoaders = sortedLoaders;
                SelectedFolder.MetadataProjectId = referenceItem.ProjectId;
                SelectedFolder.MetadataModName = referenceItem.DisplayName;

                StatusMessage = $"筛选元数据已更新: {sortedMcVersions.Count} 个 MC 版本, {sortedLoaders.Count} 个加载器 (参考: {referenceItem.DisplayName})";
                Logger.Information("筛选元数据更新完成: MC版本={McCount}, 加载器={LoaderCount}, 参考模组={Mod}",
                    sortedMcVersions.Count, sortedLoaders.Count, referenceItem.DisplayName);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "更新筛选元数据失败");
                StatusMessage = $"更新筛选元数据失败: {ex.Message}";
            }
            finally
            {
                IsRefreshingMetadata = false;
            }
        }

        /// <summary>
        /// 检查更新
        /// </summary>
        [RelayCommand]
        private async Task CheckUpdateAsync()
        {
            if (string.IsNullOrEmpty(ActiveMcVersionFilter) || string.IsNullOrEmpty(ActiveLoaderFilter))
            {
                StatusMessage = "请先选择 Minecraft 版本和加载器";
                return;
            }

            if (ModItems.Count == 0)
            {
                StatusMessage = "请先选择一个包含模组的文件夹";
                return;
            }

            Logger.Information("开始检查更新: MC={Mc}, Loader={Loader}, 模组数={Count}",
                ActiveMcVersionFilter, ActiveLoaderFilter, ModItems.Count);

            IsCheckingUpdate = true;
            StatusMessage = "正在检查更新...";
            _allVersionsCache.Clear();

            var upToDate = 0;
            var updateAvailable = 0;
            var notFound = 0;
            var incompatible = 0;
            var error = 0;

            try
            {
                // 收集所有MC版本（用于构建可选列表）
                var allMcVersions = new HashSet<string>();

                foreach (var item in ModItems)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(item.ProjectId))
                        {
                            item.UpdateStatus = LocalModUpdateStatus.NotFound;
                            item.UpdateStatusText = "Modrinth 未收录";
                            notFound++;
                            continue;
                        }

                        // 获取该项目的版本列表
                        var versions = await _apiService.GetProjectVersionsAsync(item.ProjectId);
                        _allVersionsCache[item.ProjectId] = versions;

                        // 提取所有支持的MC版本
                        foreach (var v in versions)
                            foreach (var gv in v.GameVersions)
                                allMcVersions.Add(gv);

                        // 筛选兼容当前过滤器的版本
                        var compatibleVersions = versions
                            .Where(v => v.GameVersions.Contains(ActiveMcVersionFilter!)
                                     && v.Loaders.Contains(ActiveLoaderFilter!, StringComparer.OrdinalIgnoreCase))
                            .ToList();

                        if (compatibleVersions.Count == 0)
                        {
                            item.UpdateStatus = LocalModUpdateStatus.Incompatible;
                            item.UpdateStatusText = $"无 {ActiveMcVersionFilter} + {ActiveLoaderFilter} 版本";
                            incompatible++;
                            continue;
                        }

                        // 优先选 Release 版本
                        var bestVersion = SelectBestVersion(compatibleVersions);
                        item.LatestVersion = bestVersion;

                        // 比较版本号
                        if (bestVersion.VersionNumber == item.CurrentVersion)
                        {
                            item.UpdateStatus = LocalModUpdateStatus.UpToDate;
                            item.UpdateStatusText = "已是最新";
                            upToDate++;
                        }
                        else
                        {
                            item.UpdateStatus = LocalModUpdateStatus.UpdateAvailable;
                            item.UpdateStatusText = $"{item.CurrentVersion} → {bestVersion.VersionNumber}";
                            updateAvailable++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning(ex, "检查模组更新失败: {ModId}", item.ModId);
                        item.UpdateStatus = LocalModUpdateStatus.Error;
                        item.UpdateStatusText = "检查失败";
                        error++;
                    }
                }

                StatusMessage = $"有更新: {updateAvailable} | 已是最新: {upToDate} | 无兼容: {incompatible} | 未找到: {notFound}";
                if (error > 0)
                    StatusMessage += $" | 错误: {error}";

                Logger.Information("检查更新完成: UpToDate={UpToDate}, UpdateAvailable={Update}, Incompatible={Incomp}, NotFound={NotFound}, Error={Error}",
                    upToDate, updateAvailable, incompatible, notFound, error);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "检查更新过程失败");
                StatusMessage = $"检查更新失败: {ex.Message}";
            }
            finally
            {
                IsCheckingUpdate = false;
            }
        }

        /// <summary>
        /// 更新全部
        /// </summary>
        [RelayCommand]
        private async Task UpdateAllAsync()
        {
            var hasCheckedAny = ModItems.Any(m => m.UpdateStatus != LocalModUpdateStatus.Unknown);
            if (!hasCheckedAny)
            {
                StatusMessage = "请先点击[检查更新]";
                return;
            }

            var modsToUpdate = ModItems
                .Where(m => m.UpdateStatus == LocalModUpdateStatus.UpdateAvailable && m.LatestVersion != null)
                .ToList();

            if (modsToUpdate.Count == 0)
            {
                StatusMessage = "所有模组已是最新版，无需更新";
                return;
            }

            // 确认对话框
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            var dialog = new ConfirmDialog();
            dialog.SetContent("确认更新", $"以下 {modsToUpdate.Count} 个模组将被更新（文件内容更新，文件名保持不变），是否继续？");
            var confirmed = await dialog.ShowDialog<bool>(mainWindow);
            if (!confirmed)
            {
                StatusMessage = "更新已取消";
                return;
            }

            Logger.Information("开始更新全部: {Count} 个模组", modsToUpdate.Count);
            IsUpdating = true;
            UpdateProgressValue = 0;
            _updateCts = new CancellationTokenSource();

            var totalMods = modsToUpdate.Count;
            var completedMods = 0;
            var successCount = 0;
            var failedMods = new List<string>();

            foreach (var item in modsToUpdate)
            {
                if (_updateCts.Token.IsCancellationRequested)
                    break;

                completedMods++;
                StatusMessage = $"[{completedMods}/{totalMods}] 正在更新: {item.DisplayName}";
                UpdateProgressValue = (double)(completedMods - 1) / totalMods * 100;

                try
                {
                    var progress = new Progress<DownloadProgress>(p =>
                    {
                        var baseProgress = (double)(completedMods - 1) / totalMods * 100;
                        var subProgress = p.Percentage / totalMods;
                        UpdateProgressValue = baseProgress + subProgress;
                    });

                    var success = await UpdateSingleModAsync(item, progress, _updateCts.Token);
                    if (success)
                    {
                        successCount++;
                        item.UpdateStatus = LocalModUpdateStatus.UpToDate;
                        item.UpdateStatusText = "已是最新";
                        item.CurrentVersion = item.LatestVersion!.VersionNumber;
                    }
                    else
                    {
                        failedMods.Add(item.DisplayName);
                        item.UpdateStatus = LocalModUpdateStatus.Error;
                        item.UpdateStatusText = "更新失败";
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.Warning("更新被用户取消");
                    StatusMessage = "更新已取消";
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "更新模组失败: {Name}", item.DisplayName);
                    failedMods.Add(item.DisplayName);
                    item.UpdateStatus = LocalModUpdateStatus.Error;
                    item.UpdateStatusText = "更新失败";
                }
            }

            UpdateProgressValue = 100;

            if (!_updateCts.Token.IsCancellationRequested)
            {
                if (failedMods.Count > 0)
                {
                    StatusMessage = $"更新完成: {successCount} 个成功, {failedMods.Count} 个失败";
                    Logger.Warning("更新完成（有失败）: 成功={Success}, 失败列表={List}",
                        successCount, string.Join(", ", failedMods));
                }
                else
                {
                    StatusMessage = $"全部更新完成: {successCount} 个模组";
                    Logger.Information("全部更新完成: {Count} 个", successCount);
                }

                // 刷新状态消息
                var upToDateCount = ModItems.Count(m => m.UpdateStatus == LocalModUpdateStatus.UpToDate);
                var updateCount = ModItems.Count(m => m.UpdateStatus == LocalModUpdateStatus.UpdateAvailable);
                StatusMessage = $"有更新: {updateCount} | 已是最新: {upToDateCount}";
            }

            IsUpdating = false;
            _updateCts?.Dispose();
            _updateCts = null;
        }

        /// <summary>
        /// 更新单个模组：下载新内容到临时文件 → 删除旧文件 → 重命名为原始文件名
        /// </summary>
        private async Task<bool> UpdateSingleModAsync(
            LocalModDisplayItem item,
            IProgress<DownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            if (item.LatestVersion == null || string.IsNullOrEmpty(item.ProjectId))
                return false;

            var version = item.LatestVersion;
            var primaryFile = version.Files.FirstOrDefault(f => f.Primary) ?? version.Files.FirstOrDefault();
            if (primaryFile == null)
            {
                Logger.Warning("模组版本没有可下载文件: {Name} v{Version}", item.DisplayName, version.VersionNumber);
                return false;
            }

            var tempFilePath = item.FilePath + ".updating";

            try
            {
                // 直接下载新文件内容到临时文件（不使用 DownloadRecommendedAsync，避免文件名冲突）
                // DownloadFileAsync 接受 IProgress<long>（字节数），这里传 null，整体进度在 UpdateAll 层面跟踪
                await _apiService.DownloadFileAsync(primaryFile.Url, tempFilePath, null, cancellationToken);

                // 验证临时文件存在
                if (!File.Exists(tempFilePath))
                {
                    Logger.Warning("下载的临时文件不存在: {Path}", tempFilePath);
                    return false;
                }

                // 删除旧文件
                if (File.Exists(item.FilePath))
                {
                    File.Delete(item.FilePath);
                }

                // 将临时文件重命名为原始文件名（保持文件名不变）
                File.Move(tempFilePath, item.FilePath);

                Logger.Information("模组更新成功（文件名保持不变）: {Name} v{OldVersion} -> v{NewVersion}, 文件: {FilePath}",
                    item.DisplayName, item.CurrentVersion, version.VersionNumber, item.FilePath);
                return true;
            }
            catch (OperationCanceledException)
            {
                // 清理临时文件
                TryDeleteFile(tempFilePath);
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "更新模组失败: {Name}", item.DisplayName);
                TryDeleteFile(tempFilePath);
                return false;
            }
        }

        /// <summary>
        /// 取消更新
        /// </summary>
        [RelayCommand]
        private void CancelUpdate()
        {
            Logger.Information("用户请求取消更新");
            _updateCts?.Cancel();
        }

        /// <summary>
        /// 删除本地模组文件
        /// </summary>
        [RelayCommand]
        private async Task DeleteModFileAsync(LocalModDisplayItem? item)
        {
            if (item == null) return;

            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            var dialog = new ConfirmDialog();
            dialog.SetContent("确认删除文件",
                $"确定要删除本地模组文件「{item.DisplayName}」吗？\n文件路径: {item.FilePath}\n\n此操作将永久删除文件，不可撤销。");
            var confirmed = await dialog.ShowDialog<bool>(mainWindow);
            if (!confirmed) return;

            try
            {
                if (File.Exists(item.FilePath))
                {
                    File.Delete(item.FilePath);
                    Logger.Information("已删除模组文件: {Path}", item.FilePath);
                }

                ModItems.Remove(item);
                HasItems = ModItems.Count > 0;
                StatusMessage = $"已删除: {item.DisplayName}";
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "删除模组文件失败: {Path}", item.FilePath);
                StatusMessage = $"删除失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 返回搜索页面
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            Logger.Information("从本地模组页面返回搜索");
            if (IsUpdating)
            {
                _updateCts?.Cancel();
            }
            _navigation.MainViewModel.NavigateToSearch();
        }

        /// <summary>
        /// 从版本列表中选取最佳版本
        /// </summary>
        private static ModrinthVersion SelectBestVersion(List<ModrinthVersion> versions)
        {
            var release = versions
                .Where(v => v.VersionType == VersionType.Release)
                .OrderByDescending(v => v.DatePublished)
                .FirstOrDefault();
            if (release != null) return release;

            var beta = versions
                .Where(v => v.VersionType == VersionType.Beta)
                .OrderByDescending(v => v.DatePublished)
                .FirstOrDefault();
            if (beta != null) return beta;

            return versions.OrderByDescending(v => v.DatePublished).First();
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

        /// <summary>
        /// 尝试删除文件，忽略异常
        /// </summary>
        private static void TryDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {
                // 忽略删除失败
            }
        }
    }
}