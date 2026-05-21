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
                    // 提取大版本号 (e.g. "1.20.1" -> "1.20")
                    var parts = gv.Split('.');
                    if (parts.Length < 2) continue;
                    var majorVersion = $"{parts[0]}.{parts[1]}";

                    if (!groups.ContainsKey(majorVersion))
                        groups[majorVersion] = new List<ModVersionItem>();

                    // 避免同一个版本在一个大版本组内重复（如果它支持多个子版本）
                    // 但我们这里按 Version ID 去重比较合理，因为同一个 ModVersion 对应多个子游戏版本
                    // 不过上面是双层循环，所以同一个 ver 对象可能会被加入多次 majorVersion 组，这是预期的。
                    // 但在同一个 majorVersion 组内，同一个 ver.Id 不应该重复出现。
                    if (!groups[majorVersion].Any(x => x.Version.Id == ver.Id))
                    {
                        groups[majorVersion].Add(new ModVersionItem
                        {
                            Version = ver,
                            DisplayName = string.IsNullOrEmpty(ver.Name) ? ver.VersionNumber : ver.Name,
                            VersionTypeText = ver.VersionType.ToString(),
                            LoadersText = string.Join(", ", ver.Loaders),
                            GameVersionsText = string.Join(", ", ver.GameVersions),
                            DatePublishedText = ver.DatePublished.ToString("yyyy-MM-dd")
                        });
                    }
                }
            }

            // 排序
            var sortedGroups = groups
                .OrderByDescending(g => g.Key) // Major Version 降序
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

            ActiveLoaderFilter = null;
            ApplyLoaderFilter();

            _logger.Debug("版本分组完成: 共 {GroupCount} 个大版本组，可用加载器: {Loaders}", sortedGroups.Count, string.Join(", ", loaders));
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
            ApplyLoaderFilter();
            _logger.Information("加载器过滤切换: ActiveFilter={Filter}", ActiveLoaderFilter ?? "(无)");
        }

        private void ApplyLoaderFilter()
        {
            VersionGroups.Clear();

            var source = _allVersionGroups;
            if (!string.IsNullOrEmpty(ActiveLoaderFilter))
            {
                foreach (var group in source)
                {
                    var filtered = group.Versions
                        .Where(v => v.Version.Loaders.Contains(ActiveLoaderFilter, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                    if (filtered.Count > 0)
                    {
                        VersionGroups.Add(new McVersionGroup
                        {
                            MajorVersion = group.MajorVersion,
                            Versions = filtered
                        });
                    }
                }
            }
            else
            {
                foreach (var group in source)
                {
                    VersionGroups.Add(group);
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