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
using MOneClickDownloads.App.Models;
using MOneClickDownloads.DataModel.Enums;
using MOneClickDownloads.DataModel.Version;
using MOneClickDownloads.Service;
using MOneClickDownloads.Service.Models;
using Serilog;

namespace MOneClickDownloads.App.ViewModels
{
    public partial class ModDetailViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainVm;
        private readonly ModDownloadService _downloadService;
        private readonly ModrinthAPIService _apiService;
        private readonly ConfigService _configService;
        private readonly ILogger _logger;

        [ObservableProperty]
        private string _projectTitle;

        [ObservableProperty]
        private string _projectDescription;

        [ObservableProperty]
        private string _projectId;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableCollection<McVersionGroup> _versionGroups = new();

        [ObservableProperty]
        private ObservableCollection<string> _availableLoaders = new();

        [ObservableProperty]
        private string? _activeLoaderFilter;

        [ObservableProperty]
        private ObservableCollection<string> _availableMcVersions = new();

        [ObservableProperty]
        private string? _activeMcVersionFilter;

        [ObservableProperty]
        private ObservableCollection<string> _availableStatusFilters = new();

        [ObservableProperty]
        private string? _activeStatusFilter;

        private List<McVersionGroup> _allVersionGroups = new();

        [ObservableProperty]
        private bool _isDownloading;

        [ObservableProperty]
        private string _downloadStatus = string.Empty;

        [ObservableProperty]
        private double _downloadProgressValue;

        private CancellationTokenSource? _downloadCts;
        private bool _downloadCompleted;

        public ModDetailViewModel(MainWindowViewModel mainVm, string projectId, string projectTitle, string projectDescription)
        {
            _mainVm = mainVm;
            _downloadService = mainVm.DownloadService;
            _apiService = mainVm.ApiService;
            _configService = mainVm.ConfigService;
            _logger = Log.ForContext<ModDetailViewModel>();
            
            _projectId = projectId;
            _projectTitle = projectTitle;
            _projectDescription = projectDescription;

            _logger.Information("ModDetailViewModel 初始化: ProjectId={ProjectId}, Title={Title}", projectId, projectTitle);

            // 启动加载
            LoadVersionsCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadVersionsAsync()
        {
            _logger.Information("开始加载版本列表: ProjectId={ProjectId}", ProjectId);
            IsLoading = true;
            try
            {
                var versions = await _apiService.GetProjectVersionsAsync(ProjectId);
                _logger.Information("版本列表加载成功: ProjectId={ProjectId}, 版本数量={Count}", ProjectId, versions.Count);
                GroupVersions(versions);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载版本列表失败: ProjectId={ProjectId}", ProjectId);
                // 错误处理
                System.Diagnostics.Debug.WriteLine($"Load versions failed: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void GroupVersions(List<ModrinthVersion> versions)
        {
            var groups = new Dictionary<string, List<ModVersionItem>>();

            foreach (var ver in versions)
            {
                foreach (var gv in ver.GameVersions)
                {
                    if (!groups.ContainsKey(gv))
                        groups[gv] = new List<ModVersionItem>();

                    // 避免同一个版本在同一个游戏版本组内重复
                    if (!groups[gv].Any(x => x.Version.Id == ver.Id))
                    {
                        groups[gv].Add(new ModVersionItem
                        {
                            Version = ver,
                            DisplayName = string.IsNullOrEmpty(ver.Name) ? ver.VersionNumber : ver.Name,
                            VersionTypeText = ver.VersionType.ToString(),
                            DisplayTypeTag = ModVersionItem.GetTypeTag(ver.VersionType),
                            LoadersText = string.Join(", ", ver.Loaders),
                            GameVersionsText = string.Join(", ", ver.GameVersions),
                            DatePublishedText = ver.DatePublished.ToString("yyyy-MM-dd")
                        });
                    }
                }
            }

            // 按版本号排序（降序），自定义版本号比较器
            var sortedGroups = groups
                .OrderByDescending(g => g.Key, new McVersionComparer())
                .Select(g => new McVersionGroup
                {
                    MajorVersion = g.Key,
                    Versions = g.Value.OrderByDescending(v => v.Version.DatePublished).ToList()
                })
                .ToList();

            _allVersionGroups = sortedGroups;

            // 提取所有不重复的加载器
            var loaders = versions
                .SelectMany(v => v.Loaders)
                .Distinct()
                .OrderBy(l => l)
                .ToList();
            AvailableLoaders.Clear();
            foreach (var loader in loaders)
            {
                AvailableLoaders.Add(loader);
            }

            // 提取所有不重复的 MC 版本（降序）
            var mcVersions = groups.Keys
                .OrderByDescending(v => v, new McVersionComparer())
                .ToList();
            AvailableMcVersions.Clear();
            foreach (var mcVer in mcVersions)
            {
                AvailableMcVersions.Add(mcVer);
            }

            // 初始化发布状态过滤选项
            AvailableStatusFilters.Clear();
            AvailableStatusFilters.Add("全部");
            AvailableStatusFilters.Add("发布");
            AvailableStatusFilters.Add("预览");

            ActiveLoaderFilter = null;
            ActiveStatusFilter = "全部";

            // 从配置中恢复 MC 版本过滤器
            var savedMcVersion = _configService.Get<string>(ConfigKeys.ActiveMcVersionFilter);
            if (!string.IsNullOrEmpty(savedMcVersion) && mcVersions.Contains(savedMcVersion))
            {
                ActiveMcVersionFilter = savedMcVersion;
                _logger.Information("从配置恢复 MC 版本过滤器: {McVersion}", savedMcVersion);
            }
            else
            {
                ActiveMcVersionFilter = null;
            }

            ApplyFilters();

            _logger.Debug("版本分组完成: 共 {GroupCount} 个版本组，可用加载器: {Loaders}", sortedGroups.Count, string.Join(", ", loaders));
        }

        [RelayCommand]
        private void ToggleLoaderFilter(string? loader)
        {
            if (string.IsNullOrEmpty(loader))
            {
                ActiveLoaderFilter = null;
            }
            else if (ActiveLoaderFilter == loader)
            {
                ActiveLoaderFilter = null;
            }
            else
            {
                ActiveLoaderFilter = loader;
            }
            ApplyFilters();
            _logger.Information("加载器过滤切换: ActiveFilter={Filter}", ActiveLoaderFilter ?? "(无)");
        }

        [RelayCommand]
        private void ToggleMcVersionFilter(string? mcVersion)
        {
            if (string.IsNullOrEmpty(mcVersion))
            {
                ActiveMcVersionFilter = null;
            }
            else if (ActiveMcVersionFilter == mcVersion)
            {
                ActiveMcVersionFilter = null;
            }
            else
            {
                ActiveMcVersionFilter = mcVersion;
            }

            // 持久化 MC 版本过滤器
            if (string.IsNullOrEmpty(ActiveMcVersionFilter))
            {
                _configService.Remove(ConfigKeys.ActiveMcVersionFilter);
            }
            else
            {
                _configService.Set(ConfigKeys.ActiveMcVersionFilter, ActiveMcVersionFilter);
            }

            ApplyFilters();
            _logger.Information("MC版本过滤切换: ActiveFilter={Filter}", ActiveMcVersionFilter ?? "(无)");
        }

        [RelayCommand]
        private void ToggleStatusFilter(string? status)
        {
            if (string.IsNullOrEmpty(status) || status == "全部")
            {
                ActiveStatusFilter = "全部";
            }
            else if (ActiveStatusFilter == status)
            {
                ActiveStatusFilter = "全部";
            }
            else
            {
                ActiveStatusFilter = status;
            }
            ApplyFilters();
            _logger.Information("发布状态过滤切换: ActiveFilter={Filter}", ActiveStatusFilter ?? "(全部)");
        }

        private void ApplyFilters()
        {
            VersionGroups.Clear();

            foreach (var group in _allVersionGroups)
            {
                var filtered = group.Versions.AsEnumerable();

                // 应用 Loader 过滤
                if (!string.IsNullOrEmpty(ActiveLoaderFilter))
                {
                    filtered = filtered.Where(v => v.Version.Loaders.Contains(ActiveLoaderFilter, StringComparer.OrdinalIgnoreCase));
                }

                // 应用 MC 版本过滤
                if (!string.IsNullOrEmpty(ActiveMcVersionFilter))
                {
                    filtered = filtered.Where(v => v.Version.GameVersions.Contains(ActiveMcVersionFilter));
                }

                // 应用发布状态过滤
                if (!string.IsNullOrEmpty(ActiveStatusFilter) && ActiveStatusFilter != "全部")
                {
                    filtered = ActiveStatusFilter == "发布"
                        ? filtered.Where(v => v.Version.VersionType == VersionType.Release)
                        : filtered.Where(v => v.Version.VersionType != VersionType.Release);
                }

                var filteredList = filtered.ToList();
                if (filteredList.Count > 0)
                {
                    VersionGroups.Add(new McVersionGroup
                    {
                        MajorVersion = group.MajorVersion,
                        Versions = filteredList
                    });
                }
            }
        }

        [RelayCommand]
        private async Task DownloadVersionAsync(ModVersionItem? item)
        {
            if (item == null || IsDownloading) return;

            _logger.Information("用户请求下载: ProjectId={ProjectId}, VersionName={VersionName}, VersionId={VersionId}",
                item.Version.ProjectId, item.DisplayName, item.Version.Id);

            // 获取主窗口作为 TopLevel
            var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null) return;

            // 打开文件夹选择对话框
            var folderPath = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择下载位置",
                AllowMultiple = false
            });

            if (folderPath.Count == 0) return;
            
            var savePath = folderPath[0].Path.LocalPath;

            _logger.Information("用户选择保存目录: {SavePath}", savePath);

            IsDownloading = true;
            _downloadCompleted = false;
            DownloadStatus = "准备下载...";
            DownloadProgressValue = 0;
            _downloadCts = new CancellationTokenSource();

            var progress = new Progress<DownloadProgress>(p =>
            {
                if (!_downloadCompleted)
                {
                    DownloadStatus = $"正在下载: {p.CurrentFileName} ({p.CompletedCount}/{p.TotalCount})";
                    DownloadProgressValue = p.Percentage;
                }
            });

            try
            {
                var gameVersion = item.Version.GameVersions.FirstOrDefault() ?? "";
                var loader = item.Version.Loaders.FirstOrDefault() ?? "";
                var versionType = item.Version.VersionType;

                _logger.Information("开始下载任务: GameVersion={GameVersion}, Loader={Loader}, VersionType={VersionType}",
                    gameVersion, loader, versionType);

                var stopwatch = Stopwatch.StartNew();

                var results = await _downloadService.DownloadWithDependenciesAsync(
                    item.Version.ProjectId,
                    gameVersion,
                    loader,
                    savePath,
                    versionType,
                    progress,
                    _downloadCts.Token
                );

                stopwatch.Stop();
                _downloadCompleted = true;
                DownloadProgressValue = 100;
                DownloadStatus = $"下载完成，用时 {stopwatch.Elapsed.TotalSeconds:F1} 秒，共 {results.Count} 个文件";
                _logger.Information("下载任务完成: 共 {Count} 个文件，耗时 {Elapsed} 秒", results.Count, stopwatch.Elapsed.TotalSeconds);
            }
            catch (OperationCanceledException)
            {
                DownloadStatus = "下载已取消。";
                _logger.Warning("下载任务被用户取消");
            }
            catch (Exception ex)
            {
                DownloadStatus = $"下载失败: {ex.Message}";
                _logger.Error(ex, "下载任务失败: ProjectId={ProjectId}", item.Version.ProjectId);
            }
            finally
            {
                IsDownloading = false;
                _downloadCts?.Dispose();
                _downloadCts = null;
            }
        }

        [RelayCommand]
        private void CancelDownload()
        {
            _logger.Information("用户请求取消下载");
            _downloadCts?.Cancel();
        }

        [RelayCommand]
        private void GoBack()
        {
            _logger.Information("用户请求返回搜索页面");
            if (IsDownloading)
            {
                _logger.Information("取消正在进行的下载");
                _downloadCts?.Cancel();
            }
            _mainVm.NavigateToSearch();
        }
    }
}