using System.Text.Json;
using MOneClickDownloads.DataModel.Project;
using MOneClickDownloads.DataModel.Search;
using MOneClickDownloads.DataModel.Version;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// Modrinth API 封装服务，提供统一的HTTP请求接口。<br />
    /// <br />
    /// 职责：<br />
    /// - 封装所有Modrinth API端点请求<br />
    /// - 管理HttpClient生命周期和配置<br />
    /// - 处理JSON序列化/反序列化<br />
    /// - 为所有请求添加统一的UserAgent<br />
    /// <br />
    /// 设计模式：<br />
    /// 支持两种构造方式：<br />
    /// 1. 外部传入HttpClient（适用于已配置HttpClientFactory的DI场景）<br />
    /// 2. 自维护HttpClient（实现IDisposable，配置PooledConnectionLifetime）<br />
    /// <br />
    /// 使用示例：<br />
    /// <code>
    /// // 外部传入模式（推荐用于ASP.NET Core等DI环境）
    /// using var apiService = new ModrinthAPIService(httpClient);
    /// 
    /// // 自维护模式（适用于控制台应用或独立服务）
    /// using var apiService = new ModrinthAPIService();
    /// </code>
    /// </summary>
    public class ModrinthAPIService : IDisposable
    {
        private const string BaseUrl = "https://api.modrinth.com/v2";
        private const string UserAgent = "NanTingPer/MOneClickDownloads";

        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;
        private bool _disposed = false;

        /// <summary>
        /// 使用外部传入的HttpClient构造服务（推荐用于DI环境）。<br />
        /// <br />
        /// 数据流：<br />
        /// - 外部HttpClient通过构造函数注入<br />
        /// - 服务会为该HttpClient添加UserAgent头（如果尚未添加）<br />
        /// - 服务不负责HttpClient的生命周期管理<br />
        /// <br />
        /// 使用场景：<br />
        /// - ASP.NET Core应用中通过IHttpClientFactory创建的HttpClient<br />
        /// - 单元测试中注入的模拟HttpClient
        /// </summary>
        /// <param name="httpClient">外部提供的HttpClient实例</param>
        public ModrinthAPIService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _ownsHttpClient = false;
            ConfigureHttpClient(_httpClient);
        }

        /// <summary>
        /// 使用自维护的HttpClient构造服务（适用于独立应用）。<br />
        /// <br />
        /// 数据流：<br />
        /// - 内部创建HttpClient实例<br />
        /// - 配置PooledConnectionLifetime为10分钟（用于DNS刷新）<br />
        /// - 配置默认请求头<br />
        /// - 服务负责HttpClient的生命周期管理，Dispose时会释放HttpClient<br />
        /// <br />
        /// 使用场景：<br />
        /// - 控制台应用程序<br />
        /// - 没有DI容器的独立服务<br />
        /// - 需要长期运行且需要DNS刷新的场景
        /// </summary>
        public ModrinthAPIService()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                ConnectTimeout = TimeSpan.FromSeconds(30)
            };
            _httpClient = new HttpClient(handler);
            _ownsHttpClient = true;
            ConfigureHttpClient(_httpClient);
        }

        /// <summary>
        /// 配置HttpClient的默认请求头和基础设置。
        /// </summary>
        /// <param name="httpClient">要配置的HttpClient实例</param>
        private void ConfigureHttpClient(HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// 获取项目详情。<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 构建请求URL：<code>/project/{id|slug}</code><br />
        /// 2. 发送GET请求到Modrinth API<br />
        /// 3. 反序列化JSON响应为Project对象<br />
        /// 4. 返回项目详情<br />
        /// <br />
        /// 使用示例：<br />
        /// <code>
        /// var project = await apiService.GetProjectAsync("fabric-api");
        /// Console.WriteLine(project.Title);
        /// </code>
        /// </summary>
        /// <param name="idOrSlug">项目ID（base62编码）或URL友好标识符（slug）</param>
        /// <returns>项目详情对象</returns>
        /// <exception cref="HttpRequestException">请求失败时抛出</exception>
        /// <exception cref="JsonException">JSON解析失败时抛出</exception>
        public async Task<Project> GetProjectAsync(string idOrSlug)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/project/{idOrSlug}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Project>(content) 
                ?? throw new JsonException("无法反序列化Project对象");
        }

        /// <summary>
        /// 搜索项目。<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 构建搜索查询参数（query, facets, offset, limit）<br />
        /// 2. 发送GET请求到<code>/search</code>端点<br />
        /// 3. 反序列化JSON响应为SearchResponse对象<br />
        /// 4. 返回搜索结果（包含分页信息）<br />
        /// <br />
        /// 使用示例：<br />
        /// <code>
        /// // 基础搜索
        /// var results = await apiService.SearchProjectsAsync("fabric api", null, null, 0, 10);
        /// 
        /// // 带过滤条件的搜索
        /// var versions = new List<string> { "1.20.1" };
        /// var loaders = new List<string> { "fabric" };
        /// var filteredResults = await apiService.SearchProjectsAsync("fabric api", versions, loaders, 0, 10);
        /// </code>
        /// </summary>
        /// <param name="query">搜索关键词</param>
        /// <param name="gameVersions">要过滤的Minecraft版本列表（可选）</param>
        /// <param name="loaders">要过滤的模组加载器列表（可选）</param>
        /// <param name="offset">分页偏移量</param>
        /// <param name="limit">每页数量（最大100）</param>
        /// <returns>搜索响应对象</returns>
        public async Task<SearchResponse> SearchProjectsAsync(
            string query, 
            List<string>? gameVersions = null, 
            List<string>? loaders = null, 
            int offset = 0, 
            int limit = 10)
        {
            var queryParams = new List<string>
            {
                $"query={Uri.EscapeDataString(query)}",
                $"offset={offset}",
                $"limit={limit}"
            };

            // 构建facets过滤条件
            var facets = new List<List<string>>();
            if (gameVersions != null && gameVersions.Count > 0)
            {
                facets.Add(gameVersions.Select(v => $"versions:{v}").ToList());
            }
            if (loaders != null && loaders.Count > 0)
            {
                facets.Add(loaders.Select(l => $"categories:{l}").ToList());
            }
            if (facets.Count > 0)
            {
                var facetsJson = JsonSerializer.Serialize(facets);
                queryParams.Add($"facets={Uri.EscapeDataString(facetsJson)}");
            }

            var url = $"{BaseUrl}/search?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SearchResponse>(content) 
                ?? throw new JsonException("无法反序列化SearchResponse对象");
        }

        /// <summary>
        /// 获取项目的版本列表（支持筛选）。<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 构建请求URL：<code>/project/{id|slug}/version</code><br />
        /// 2. 添加查询参数（game_versions, loaders）进行版本过滤<br />
        /// 3. 发送GET请求<br />
        /// 4. 反序列化JSON响应为ModrinthVersion数组<br />
        /// 5. 返回版本列表<br />
        /// <br />
        /// 使用示例：<br />
        /// <code>
        /// // 获取所有版本
        /// var versions = await apiService.GetProjectVersionsAsync("fabric-api");
        /// 
        /// // 筛选特定MC版本和加载器的版本
        /// var filteredVersions = await apiService.GetProjectVersionsAsync(
        ///     "fabric-api", 
        ///     new List<string> { "1.20.1" }, 
        ///     new List<string> { "fabric" });
        /// </code>
        /// </summary>
        /// <param name="projectId">项目ID或slug</param>
        /// <param name="gameVersions">要过滤的Minecraft版本列表（可选）</param>
        /// <param name="loaders">要过滤的模组加载器列表（可选）</param>
        /// <returns>版本列表</returns>
        public async Task<List<ModrinthVersion>> GetProjectVersionsAsync(
            string projectId, 
            List<string>? gameVersions = null, 
            List<string>? loaders = null)
        {
            var queryParams = new List<string>();
            if (gameVersions != null && gameVersions.Count > 0)
            {
                queryParams.Add($"game_versions=[{string.Join(",", gameVersions.Select(v => $"\"{v}\""))}]");
            }
            if (loaders != null && loaders.Count > 0)
            {
                queryParams.Add($"loaders=[{string.Join(",", loaders.Select(l => $"\"{l}\""))}]");
            }

            var url = $"{BaseUrl}/project/{projectId}/version";
            if (queryParams.Count > 0)
            {
                url += $"?{string.Join("&", queryParams)}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ModrinthVersion>>(content) 
                ?? new List<ModrinthVersion>();
        }

        /// <summary>
        /// 获取特定版本详情。<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 构建请求URL：<code>/version/{id}</code><br />
        /// 2. 发送GET请求到Modrinth API<br />
        /// 3. 反序列化JSON响应为ModrinthVersion对象<br />
        /// 4. 返回版本详情（包含下载链接和依赖信息）<br />
        /// <br />
        /// 使用示例：<br />
        /// <code>
        /// var version = await apiService.GetVersionAsync("5zJNhXV2");
        /// Console.WriteLine($"版本: {version.Name}, 文件: {version.Files[0].Filename}");
        /// </code>
        /// </summary>
        /// <param name="versionId">版本ID（base62编码）</param>
        /// <returns>版本详情对象</returns>
        /// <exception cref="HttpRequestException">请求失败时抛出</exception>
        /// <exception cref="JsonException">JSON解析失败时抛出</exception>
        public async Task<ModrinthVersion> GetVersionAsync(string versionId)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/version/{versionId}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ModrinthVersion>(content) 
                ?? throw new JsonException("无法反序列化ModrinthVersion对象");
        }

        /// <summary>
        /// 下载文件到本地路径。<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 发送GET请求到文件的下载URL<br />
        /// 2. 确保目录存在（不存在则创建）<br />
        /// 3. 将响应流写入本地文件<br />
        /// 4. 支持进度报告和取消操作<br />
        /// <br />
        /// 使用示例：<br />
        /// <code>
        /// var progress = new Progress<long>(bytes => Console.WriteLine($"已下载: {bytes} 字节"));
        /// await apiService.DownloadFileAsync(
        ///     "https://cdn.modrinth.com/data/.../file.jar",
        ///     @"C:\mods\file.jar",
        ///     progress,
        ///     cancellationToken);
        /// </code>
        /// </summary>
        /// <param name="url">文件下载URL</param>
        /// <param name="savePath">本地保存路径</param>
        /// <param name="progress">进度报告（报告已下载字节数）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <exception cref="HttpRequestException">下载请求失败时抛出</exception>
        /// <exception cref="OperationCanceledException">操作被取消时抛出</exception>
        public async Task DownloadFileAsync(
            string url, 
            string savePath, 
            IProgress<long>? progress = null, 
            CancellationToken cancellationToken = default)
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var response = await _httpClient.GetAsync(
                url, 
                HttpCompletionOption.ResponseHeadersRead, 
                cancellationToken);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(
                savePath, 
                FileMode.Create, 
                FileAccess.Write, 
                FileShare.None, 
                bufferSize: 8192, 
                useAsync: true);

            var buffer = new byte[8192];
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }

        /// <summary>
        /// 释放资源。<br />
        /// 如果是自维护的HttpClient，则同时释放HttpClient。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的核心方法。
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && _ownsHttpClient)
                {
                    _httpClient.Dispose();
                }
                _disposed = true;
            }
        }
    }
}