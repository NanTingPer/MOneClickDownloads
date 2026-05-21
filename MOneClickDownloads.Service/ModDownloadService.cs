using MOneClickDownloads.DataModel.Enums;
using MOneClickDownloads.DataModel.Version;
using MOneClickDownloads.Service.Models;
using Serilog;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 模组下载服务，实现模组下载、依赖递归下载、版本筛选等核心下载逻辑。<br />
    /// <br />
    /// 职责：<br />
    /// - 根据MC版本和加载器筛选并下载模组<br />
    /// - 支持推荐下载（最新稳定版）和指定版本类型下载<br />
    /// - 递归下载必需依赖（Required），支持3次重试<br />
    /// - 事务性下载：失败或取消时回滚所有已下载文件<br />
    /// - 通过 <code>IProgress<DownloadProgress></code> 报告下载进度<br />
    /// - 通过 <code>CancellationToken</code> 支持取消操作（取消后回滚）<br />
    /// <br />
    /// 核心下载流程：<br />
    /// 1. 调用 <code>GetProjectVersionsAsync</code> 获取兼容版本列表<br />
    /// 2. 按版本类型筛选（release > beta > alpha）并选取目标版本<br />
    /// 3. 获取版本的 primary 文件下载链接<br />
    /// 4. 下载文件到本地<br />
    /// 5. 遍历 <code>Dependencies</code>，筛选 <code>Required</code> 依赖<br />
    /// 6. 若 <code>VersionId</code> 有值 → 直接获取该版本；若为 null → 通过 <code>ProjectId</code> 查询兼容版本<br />
    /// 7. 递归执行步骤 3-6（已下载项目跳过，防止循环依赖）<br />
    /// 8. 若任何步骤失败（重试3次后仍失败）或被取消 → 回滚所有已下载文件<br />
    /// <br />
    /// 使用示例：<br />
    /// <code>
    /// var apiService = new ModrinthAPIService();
    /// var downloadService = new ModDownloadService(apiService);
    /// 
    /// // 推荐下载
    /// var progress = new Progress<DownloadProgress>(p => Console.WriteLine($"{p.Percentage:F1}% - {p.CurrentFileName}"));
    /// var results = await downloadService.DownloadRecommendedAsync(
    ///     "fabric-api", "1.20.1", "fabric", @"C:\mods", progress, cancellationToken);
    /// 
    /// // 指定版本类型下载（含依赖）
    /// var results = await downloadService.DownloadWithDependenciesAsync(
    ///     "fabric-api", "1.20.1", "fabric", @"C:\mods", VersionType.Beta, progress, cancellationToken);
    /// </code>
    /// </summary>
    public class ModDownloadService
    {
        private const int MaxRetryCount = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        private readonly ModrinthAPIService _apiService;
        private readonly ILogger _logger;

        /// <summary>
        /// 构造下载服务。<br />
        /// <br />
        /// 数据流：<br />
        /// - 注入 ModrinthAPIService 实例以复用HTTP连接和配置<br />
        /// - 后续所有API调用通过此实例进行<br />
        /// <br />
        /// 使用场景：<br />
        /// - 需要下载模组时创建此服务<br />
        /// - 通常与 ModSearchService 共享同一个 ModrinthAPIService 实例
        /// </summary>
        /// <param name="apiService">Modrinth API 服务实例</param>
        public ModDownloadService(ModrinthAPIService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _logger = Log.ForContext<ModDownloadService>();
            _logger.Information("ModDownloadService 已初始化");
        }

        /// <summary>
        /// 推荐下载：下载指定模组支持此MC版本的最新稳定版（Release）。<br />
        /// <br />
        /// 筛选逻辑：<br />
        /// 1. 获取该项目兼容指定 gameVersion + loader 的所有版本<br />
        /// 2. 筛选 <code>VersionType == Release</code> 的版本<br />
        /// 3. 按 <code>DatePublished</code> 降序排列，取最新一个<br />
        /// 4. 若无 Release 版本 → 返回空列表（不推荐非稳定版）<br />
        /// 5. 下载该版本的 primary 文件及其 Required 依赖<br />
        /// <br />
        /// 数据流：<br />
        /// 1. <code>GetProjectVersionsAsync(projectId, [gameVersion], [loader])</code><br />
        /// 2. 筛选 <code>VersionType.Release</code> → 按 <code>DatePublished</code> 降序 → 取第一个<br />
        /// 3. Get primary file → <code>DownloadFileAsync</code><br />
        /// 4. 遍历 <code>Dependencies</code> → 递归下载 Required 依赖<br />
        /// 5. 若失败 → 重试3次 → 仍失败 → 回滚已下载文件<br />
        /// <br />
        /// 使用示例：<br />
        /// <code>
        /// var results = await downloadService.DownloadRecommendedAsync(
        ///     "fabric-api", "1.20.1", "fabric", @"C:\mods", progress, cts.Token);
        /// 
        /// if (results.Count == 0)
        ///     Console.WriteLine("该模组没有支持此MC版本的稳定版");
        /// </code>
        /// </summary>
        /// <param name="projectId">项目ID或slug</param>
        /// <param name="gameVersion">Minecraft 版本号（如 "1.20.1"）</param>
        /// <param name="loader">模组加载器名称（如 "fabric"）</param>
        /// <param name="saveDirectory">文件保存目录</param>
        /// <param name="progress">进度报告回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>下载结果列表；若无稳定版则返回空列表</returns>
        /// <exception cref="DownloadException">下载失败（重试耗尽）或被取消时抛出</exception>
        public async Task<List<DownloadResult>> DownloadRecommendedAsync(
            string projectId,
            string gameVersion,
            string loader,
            string saveDirectory,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("开始推荐下载: ProjectId={ProjectId}, GameVersion={GameVersion}, Loader={Loader}, SaveDirectory={SaveDirectory}",
                projectId, gameVersion, loader, saveDirectory);

            var gameVersions = new List<string> { gameVersion };
            var loaders = new List<string> { loader };

            // 获取兼容版本列表
            var versions = await _apiService.GetProjectVersionsAsync(projectId, gameVersions, loaders);

            // 筛选 Release 版本并按发布时间降序排列
            var releaseVersions = versions
                .Where(v => v.VersionType == VersionType.Release)
                .OrderByDescending(v => v.DatePublished)
                .ToList();

            // 若无 Release 版本，不推荐（返回空列表）
            if (releaseVersions.Count == 0)
            {
                _logger.Warning("项目 {ProjectId} 没有兼容 {GameVersion} + {Loader} 的 Release 版本，跳过推荐下载",
                    projectId, gameVersion, loader);
                return new List<DownloadResult>();
            }

            var targetVersion = releaseVersions.First();
            _logger.Information("选定推荐版本: {VersionName} (ID: {VersionId})", targetVersion.Name, targetVersion.Id);

            return await ExecuteDownloadTransactionAsync(
                targetVersion, gameVersions, loaders, saveDirectory, true, progress, cancellationToken);
        }

        /// <summary>
        /// 指定MC版本下载：下载指定模组的指定版本类型。<br />
        /// <br />
        /// 筛选逻辑：<br />
        /// 1. 获取该项目兼容指定 gameVersion + loader 的所有版本<br />
        /// 2. 筛选 <code>VersionType == versionType</code> 的版本<br />
        /// 3. 按 <code>DatePublished</code> 降序排列，取最新一个<br />
        /// 4. 若无匹配版本 → 抛出异常<br />
        /// 5. 下载该版本的 primary 文件及其 Required 依赖<br />
        /// <br />
        /// 与推荐下载的区别：<br />
        /// - 推荐下载仅下载 Release 版本，无 Release 时返回空<br />
        /// - 指定版本下载允许指定任意版本类型（Release/Beta/Alpha），无匹配时抛异常<br />
        /// <br />
        /// 使用示例：<br />
        /// <code>
        /// // 下载最新 beta 版
        /// var results = await downloadService.DownloadByVersionAsync(
        ///     "fabric-api", "1.20.1", "fabric", VersionType.Beta, @"C:\mods", progress, cts.Token);
        /// </code>
        /// </summary>
        /// <param name="projectId">项目ID或slug</param>
        /// <param name="gameVersion">Minecraft 版本号（如 "1.20.1"）</param>
        /// <param name="loader">模组加载器名称（如 "fabric"）</param>
        /// <param name="versionType">版本类型（Release/Beta/Alpha）</param>
        /// <param name="saveDirectory">文件保存目录</param>
        /// <param name="progress">进度报告回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>下载结果列表</returns>
        /// <exception cref="InvalidOperationException">无匹配版本时抛出</exception>
        /// <exception cref="DownloadException">下载失败或被取消时抛出</exception>
        public async Task<List<DownloadResult>> DownloadByVersionAsync(
            string projectId,
            string gameVersion,
            string loader,
            VersionType versionType,
            string saveDirectory,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("开始指定版本下载: ProjectId={ProjectId}, GameVersion={GameVersion}, Loader={Loader}, VersionType={VersionType}",
                projectId, gameVersion, loader, versionType);

            var gameVersions = new List<string> { gameVersion };
            var loaders = new List<string> { loader };

            var versions = await _apiService.GetProjectVersionsAsync(projectId, gameVersions, loaders);

            var filteredVersions = versions
                .Where(v => v.VersionType == versionType)
                .OrderByDescending(v => v.DatePublished)
                .ToList();

            if (filteredVersions.Count == 0)
            {
                _logger.Error("项目 {ProjectId} 没有兼容 {GameVersion} + {Loader} 的 {VersionType} 版本",
                    projectId, gameVersion, loader, versionType);
                throw new InvalidOperationException(
                    $"项目 '{projectId}' 没有兼容 {gameVersion} + {loader} 的 {versionType} 版本");
            }

            var targetVersion = filteredVersions.First();
            _logger.Information("选定版本: {VersionName} (ID: {VersionId})", targetVersion.Name, targetVersion.Id);

            return await ExecuteDownloadTransactionAsync(
                targetVersion, gameVersions, loaders, saveDirectory, true, progress, cancellationToken);
        }

        /// <summary>
        /// 带依赖下载：下载指定模组及其所有 Required 依赖。<br />
        /// <br />
        /// 完整数据流：<br />
        /// 1. 获取兼容版本 → 筛选目标版本（按 versionType）<br />
        /// 2. 获取 primary 文件 → 下载<br />
        /// 3. 遍历 <code>Dependencies</code>:<br />
        /// &nbsp;&nbsp;&nbsp;&nbsp;a. 筛选 <code>DependencyType == Required</code><br />
        /// &nbsp;&nbsp;&nbsp;&nbsp;b. 若 <code>ProjectId</code> 已在已下载列表中 → 跳过（防循环依赖）<br />
        /// &nbsp;&nbsp;&nbsp;&nbsp;c. 若 <code>VersionId</code> 有值 → <code>GetVersionAsync(versionId)</code><br />
        /// &nbsp;&nbsp;&nbsp;&nbsp;d. 若 <code>VersionId</code> 为 null → <code>GetProjectVersionsAsync(projectId, gameVersions, loaders)</code><br />
        /// &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;→ 选取兼容版本（Release优先，否则最新）<br />
        /// &nbsp;&nbsp;&nbsp;&nbsp;e. 递归执行步骤 2-3<br />
        /// 4. 任何下载失败 → 重试3次 → 仍失败 → 回滚所有已下载文件<br />
        /// 5. 取消操作 → 回滚所有已下载文件<br />
        /// <br />
        /// 使用示例：<br />
        /// <code>
        /// var results = await downloadService.DownloadWithDependenciesAsync(
        ///     "sodium", "1.20.1", "fabric", @"C:\mods", VersionType.Release, progress, cts.Token);
        /// 
        /// Console.WriteLine($"下载了 {results.Count} 个文件：");
        /// foreach (var r in results)
        ///     Console.WriteLine($"  {(r.IsDependency ? "[依赖]" : "[主模组]")} {r.FileName}");
        /// </code>
        /// </summary>
        /// <param name="projectId">项目ID或slug</param>
        /// <param name="gameVersion">Minecraft 版本号</param>
        /// <param name="loader">模组加载器名称</param>
        /// <param name="saveDirectory">文件保存目录</param>
        /// <param name="versionType">版本类型（Release/Beta/Alpha）</param>
        /// <param name="progress">进度报告回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>下载结果列表（包含主模组和所有依赖）</returns>
        /// <exception cref="InvalidOperationException">无匹配版本时抛出</exception>
        /// <exception cref="DownloadException">下载失败或被取消时抛出</exception>
        public async Task<List<DownloadResult>> DownloadWithDependenciesAsync(
            string projectId,
            string gameVersion,
            string loader,
            string saveDirectory,
            VersionType versionType,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("开始带依赖下载: ProjectId={ProjectId}, GameVersion={GameVersion}, Loader={Loader}, VersionType={VersionType}, SaveDirectory={SaveDirectory}",
                projectId, gameVersion, loader, versionType, saveDirectory);

            var gameVersions = new List<string> { gameVersion };
            var loaders = new List<string> { loader };

            var versions = await _apiService.GetProjectVersionsAsync(projectId, gameVersions, loaders);

            var filteredVersions = versions
                .Where(v => v.VersionType == versionType)
                .OrderByDescending(v => v.DatePublished)
                .ToList();

            if (filteredVersions.Count == 0)
            {
                _logger.Error("项目 {ProjectId} 没有兼容 {GameVersion} + {Loader} 的 {VersionType} 版本",
                    projectId, gameVersion, loader, versionType);
                throw new InvalidOperationException(
                    $"项目 '{projectId}' 没有兼容 {gameVersion} + {loader} 的 {versionType} 版本");
            }

            var targetVersion = filteredVersions.First();
            _logger.Information("选定版本: {VersionName} (ID: {VersionId})", targetVersion.Name, targetVersion.Id);

            return await ExecuteDownloadTransactionAsync(
                targetVersion, gameVersions, loaders, saveDirectory, true, progress, cancellationToken);
        }

        /// <summary>
        /// 执行下载事务：下载指定版本及其可选依赖，支持重试和回滚。<br />
        /// <br />
        /// 事务机制：<br />
        /// - 维护 <code>downloadedFiles</code> 列表记录所有已下载文件<br />
        /// - 维护 <code>downloadedProjects</code> 集合防止循环依赖<br />
        /// - 下载失败时重试3次<br />
        /// - 重试耗尽或取消时删除所有已下载文件（回滚）<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 获取版本的 primary 文件<br />
        /// 2. 带重试的下载文件<br />
        /// 3. 报告进度<br />
        /// 4. 若 <code>downloadDependencies == true</code> → 递归下载依赖<br />
        /// 5. 返回所有下载结果
        /// </summary>
        /// <param name="version">目标版本</param>
        /// <param name="gameVersions">游戏版本列表（用于依赖查询）</param>
        /// <param name="loaders">加载器列表（用于依赖查询）</param>
        /// <param name="saveDirectory">保存目录</param>
        /// <param name="downloadDependencies">是否下载依赖</param>
        /// <param name="progress">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>下载结果列表</returns>
        /// <exception cref="DownloadException">下载失败或被取消时抛出</exception>
        private async Task<List<DownloadResult>> ExecuteDownloadTransactionAsync(
            ModrinthVersion version,
            List<string> gameVersions,
            List<string> loaders,
            string saveDirectory,
            bool downloadDependencies,
            IProgress<DownloadProgress>? progress,
            CancellationToken cancellationToken)
        {
            var downloadedFiles = new List<string>();
            var downloadedProjects = new HashSet<string>();
            var results = new List<DownloadResult>();

            try
            {
                // 先收集所有需要下载的文件（用于计算总进度）
                var allFilesToDownload = new List<(ModrinthVersion Version, VersionFile File, bool IsDependency)>();

                // 获取主模组的 primary 文件
                var primaryFile = GetPrimaryFile(version);
                if (primaryFile == null)
                {
                    _logger.Error("版本 {VersionName} (ID: {VersionId}) 没有可下载的文件", version.Name, version.Id);
                    throw new DownloadException($"版本 '{version.Name}' (ID: {version.Id}) 没有可下载的文件");
                }
                allFilesToDownload.Add((version, primaryFile, false));
                downloadedProjects.Add(version.ProjectId);

                // 如果需要下载依赖，递归收集依赖文件
                if (downloadDependencies)
                {
                    await CollectDependencyFilesAsync(
                        version, gameVersions, loaders, downloadedProjects, allFilesToDownload, cancellationToken);
                }

                var totalCount = allFilesToDownload.Count;
                var completedCount = 0;

                _logger.Information("共需下载 {TotalCount} 个文件（主模组 + 依赖）", totalCount);

                // 逐个下载文件
                foreach (var (ver, file, isDependency) in allFilesToDownload)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var savePath = Path.Combine(saveDirectory, file.Filename);

                    _logger.Information("开始下载文件 [{CompletedCount}/{TotalCount}]: {FileName} ({Type})",
                        completedCount + 1, totalCount, file.Filename, isDependency ? "依赖" : "主模组");

                    // 报告进度：开始下载当前文件
                    progress?.Report(new DownloadProgress
                    {
                        CompletedCount = completedCount,
                        TotalCount = totalCount,
                        CurrentFileName = file.Filename,
                        CurrentProjectName = ver.Name
                    });

                    // 带重试的下载
                    await DownloadFileWithRetryAsync(file.Url, savePath, cancellationToken);
                    downloadedFiles.Add(savePath);

                    results.Add(new DownloadResult
                    {
                        FilePath = savePath,
                        SourceUrl = file.Url,
                        ProjectId = ver.ProjectId,
                        ProjectName = ver.Name,
                        VersionId = ver.Id,
                        FileName = file.Filename,
                        FileSize = file.Size,
                        IsDependency = isDependency
                    });

                    completedCount++;

                    // 报告进度：当前文件下载完成
                    progress?.Report(new DownloadProgress
                    {
                        CompletedCount = completedCount,
                        TotalCount = totalCount,
                        CurrentFileName = file.Filename,
                        CurrentProjectName = ver.Name
                    });
                }

                _logger.Information("下载事务完成，共下载 {Count} 个文件", results.Count);
                return results;
            }
            catch (OperationCanceledException)
            {
                _logger.Warning("下载被取消，正在回滚 {Count} 个已下载文件", downloadedFiles.Count);
                // 取消时回滚所有已下载文件
                RollbackDownloadedFiles(downloadedFiles);
                throw;
            }
            catch (Exception ex) when (ex is not DownloadException)
            {
                _logger.Error(ex, "下载过程中发生错误，正在回滚 {Count} 个已下载文件", downloadedFiles.Count);
                // 其他异常时回滚所有已下载文件
                RollbackDownloadedFiles(downloadedFiles);
                throw new DownloadException($"下载过程中发生错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 递归收集所有需要下载的依赖文件。<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 遍历版本的 <code>Dependencies</code> 列表<br />
        /// 2. 筛选 <code>DependencyType == Required</code> 的依赖<br />
        /// 3. 检查是否已下载（防循环依赖）→ 已下载则跳过<br />
        /// 4. 若 <code>VersionId</code> 有值 → <code>GetVersionAsync</code> 获取版本详情<br />
        /// 5. 若 <code>VersionId</code> 为 null → <code>GetProjectVersionsAsync</code> 查询兼容版本 → 选取最新<br />
        /// 6. 获取 primary 文件 → 加入下载列表<br />
        /// 7. 递归处理该依赖的子依赖
        /// </summary>
        /// <param name="version">当前版本</param>
        /// <param name="gameVersions">游戏版本列表</param>
        /// <param name="loaders">加载器列表</param>
        /// <param name="downloadedProjects">已处理的项目ID集合（防循环）</param>
        /// <param name="filesToDownload">收集到的文件列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task CollectDependencyFilesAsync(
            ModrinthVersion version,
            List<string> gameVersions,
            List<string> loaders,
            HashSet<string> downloadedProjects,
            List<(ModrinthVersion Version, VersionFile File, bool IsDependency)> filesToDownload,
            CancellationToken cancellationToken)
        {
            foreach (var dependency in version.Dependencies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 仅处理 Required 依赖
                if (dependency.DependencyType != DependencyType.Required)
                {
                    continue;
                }

                // 无效依赖（既无 VersionId 也无 ProjectId）→ 跳过
                if (string.IsNullOrEmpty(dependency.VersionId) && string.IsNullOrEmpty(dependency.ProjectId))
                {
                    continue;
                }

                ModrinthVersion depVersion;

                if (!string.IsNullOrEmpty(dependency.VersionId))
                {
                    // 有具体版本ID → 直接获取该版本
                    _logger.Debug("获取依赖版本详情: VersionId={VersionId}", dependency.VersionId);
                    depVersion = await _apiService.GetVersionAsync(dependency.VersionId);
                }
                else
                {
                    // 仅有项目ID → 查询兼容版本
                    // 若已下载过该项目 → 跳过（防循环依赖）
                    if (downloadedProjects.Contains(dependency.ProjectId!))
                    {
                        _logger.Debug("跳过已下载的依赖项目: ProjectId={ProjectId}（防循环依赖）", dependency.ProjectId);
                        continue;
                    }

                    _logger.Debug("查询依赖项目兼容版本: ProjectId={ProjectId}", dependency.ProjectId);
                    var depVersions = await _apiService.GetProjectVersionsAsync(
                        dependency.ProjectId!, gameVersions, loaders);

                    if (depVersions.Count == 0)
                    {
                        _logger.Error("依赖项目 {ProjectId} 没有兼容版本", dependency.ProjectId);
                        throw new DownloadException(
                            $"依赖项目 '{dependency.ProjectId}' 没有兼容 {string.Join(",", gameVersions)} + {string.Join(",", loaders)} 的版本");
                    }

                    // 优先选择 Release 版本，否则取最新版本
                    depVersion = SelectBestVersion(depVersions);
                }

                _logger.Information("收集到依赖: {DepName} (ProjectId: {DepProjectId}, VersionId: {DepVersionId})",
                    depVersion.Name, depVersion.ProjectId, depVersion.Id);

                // 标记该项目已处理
                downloadedProjects.Add(depVersion.ProjectId);

                // 获取 primary 文件
                var depFile = GetPrimaryFile(depVersion);
                if (depFile == null)
                {
                    _logger.Error("依赖版本 {DepName} (ID: {DepId}) 没有可下载的文件", depVersion.Name, depVersion.Id);
                    throw new DownloadException(
                        $"依赖版本 '{depVersion.Name}' (ID: {depVersion.Id}) 没有可下载的文件");
                }

                filesToDownload.Add((depVersion, depFile, true));

                // 递归处理子依赖
                await CollectDependencyFilesAsync(
                    depVersion, gameVersions, loaders, downloadedProjects, filesToDownload, cancellationToken);
            }
        }

        /// <summary>
        /// 从版本列表中选取最佳版本。<br />
        /// 优先选择 Release，其次 Beta，最后 Alpha。<br />
        /// 在同一类型中按 <code>DatePublished</code> 降序取最新。<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 按 <code>VersionType</code> 分组<br />
        /// 2. 优先返回 Release 组的最新版本<br />
        /// 3. 若无 Release → 返回 Beta 组的最新版本<br />
        /// 4. 若无 Beta → 返回 Alpha 组的最新版本<br />
        /// <br />
        /// 使用场景：<br />
        /// - 依赖项目未指定具体版本时，自动选取最佳兼容版本
        /// </summary>
        /// <param name="versions">候选版本列表</param>
        /// <returns>最佳版本</returns>
        private static ModrinthVersion SelectBestVersion(List<ModrinthVersion> versions)
        {
            // 优先 Release
            var release = versions
                .Where(v => v.VersionType == VersionType.Release)
                .OrderByDescending(v => v.DatePublished)
                .FirstOrDefault();
            if (release != null) return release;

            // 其次 Beta
            var beta = versions
                .Where(v => v.VersionType == VersionType.Beta)
                .OrderByDescending(v => v.DatePublished)
                .FirstOrDefault();
            if (beta != null) return beta;

            // 最后 Alpha
            return versions
                .Where(v => v.VersionType == VersionType.Alpha)
                .OrderByDescending(v => v.DatePublished)
                .First();
        }

        /// <summary>
        /// 获取版本的主要下载文件。<br />
        /// <br />
        /// 优先级：<br />
        /// 1. 查找 <code>Primary == true</code> 的文件<br />
        /// 2. 若无 primary 文件 → 返回第一个文件<br />
        /// 3. 若文件列表为空 → 返回 null<br />
        /// <br />
        /// 使用场景：<br />
        /// - 每个版本通常有一个 primary 文件（主jar包）<br />
        /// - 其他文件可能是源码包、javadoc等非必需文件
        /// </summary>
        /// <param name="version">版本对象</param>
        /// <returns>主要下载文件，或 null</returns>
        private static VersionFile? GetPrimaryFile(ModrinthVersion version)
        {
            if (version.Files.Count == 0) return null;

            return version.Files.FirstOrDefault(f => f.Primary)
                ?? version.Files.First();
        }

        /// <summary>
        /// 带重试的文件下载。<br />
        /// <br />
        /// 重试策略：<br />
        /// - 最多重试 <code>MaxRetryCount</code> (3) 次<br />
        /// - 每次重试间隔 <code>RetryDelay</code> (2秒)<br />
        /// - 取消操作不重试，直接抛出 <code>OperationCanceledException</code><br />
        /// - 重试耗尽后抛出 <code>DownloadException</code><br />
        /// <br />
        /// 数据流：<br />
        /// 1. 尝试调用 <code>_apiService.DownloadFileAsync()</code><br />
        /// 2. 若成功 → 返回<br />
        /// 3. 若失败且非取消 → 等待后重试<br />
        /// 4. 若重试耗尽 → 抛出 <code>DownloadException</code>
        /// </summary>
        /// <param name="url">下载URL</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <exception cref="DownloadException">重试耗尽后抛出</exception>
        /// <exception cref="OperationCanceledException">取消时抛出</exception>
        private async Task DownloadFileWithRetryAsync(
            string url, string savePath, CancellationToken cancellationToken)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= MaxRetryCount; attempt++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _apiService.DownloadFileAsync(url, savePath, null, cancellationToken);
                    return; // 下载成功
                }
                catch (OperationCanceledException)
                {
                    throw; // 取消不重试
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.Warning(ex, "文件下载失败（第 {Attempt}/{MaxRetry} 次）: {Url}", attempt, MaxRetryCount, url);

                    // 清理可能的部分下载文件
                    TryDeleteFile(savePath);

                    if (attempt < MaxRetryCount)
                    {
                        await Task.Delay(RetryDelay, cancellationToken);
                    }
                }
            }

            _logger.Error(lastException, "文件下载最终失败（已重试 {MaxRetry} 次）: {Url}", MaxRetryCount, url);
            throw new DownloadException(
                $"下载文件失败（已重试 {MaxRetryCount} 次）: {url} - {lastException?.Message}", lastException);
        }

        /// <summary>
        /// 回滚：删除所有已下载的文件。<br />
        /// <br />
        /// 使用场景：<br />
        /// - 下载事务失败（重试耗尽）时清理已下载文件<br />
        /// - 用户取消下载时清理已下载文件<br />
        /// - 不抛出异常（尽力清理）<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 遍历已下载文件路径列表<br />
        /// 2. 尝试删除每个文件<br />
        /// 3. 忽略删除失败的文件（不影响后续清理）
        /// </summary>
        /// <param name="filePaths">已下载文件的路径列表</param>
        private static void RollbackDownloadedFiles(List<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                TryDeleteFile(filePath);
            }
        }

        /// <summary>
        /// 尝试删除文件，忽略异常。
        /// </summary>
        /// <param name="filePath">文件路径</param>
        private static void TryDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // 忽略删除失败
            }
        }
    }

    /// <summary>
    /// 下载异常，表示下载过程中发生的错误。<br />
    /// <br />
    /// 使用场景：<br />
    /// - 文件下载失败（重试耗尽）<br />
    /// - 版本没有可下载文件<br />
    /// - 依赖项目没有兼容版本<br />
    /// - 通常在 <code>DownloadException</code> 外层会被捕获并触发回滚
    /// </summary>
    public class DownloadException : Exception
    {
        public DownloadException(string message) : base(message) { }
        public DownloadException(string message, Exception? innerException) : base(message, innerException) { }
    }
}