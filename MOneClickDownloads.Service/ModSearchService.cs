using MOneClickDownloads.DataModel.Search;
using Serilog;

namespace MOneClickDownloads.Service
{
    /// <summary>
    /// 模组搜索服务，封装搜索逻辑，提供友好的搜索接口。<br />
    /// <br />
    /// 职责：<br />
    /// - 封装 ModrinthAPIService 的搜索方法<br />
    /// - 构建 facets 过滤条件（MC版本、加载器）<br />
    /// - 提供分页搜索接口<br />
    /// <br />
    /// 数据流（搜索流程）：<br />
    /// 1. 用户输入关键词和可选过滤条件<br />
    /// 2. 构建 facets 过滤参数<br />
    /// 3. 调用 <code>ModrinthAPIService.SearchProjectsAsync()</code><br />
    /// 4. 返回 <code>SearchResponse</code>（包含 <code>ProjectHit[]</code> 和分页信息）<br />
    /// <br />
    /// 使用示例：<br />
    /// <code>
    /// var searchService = new ModSearchService(apiService);
    /// 
    /// // 基础搜索
    /// var results = await searchService.SearchAsync("fabric api");
    /// 
    /// // 带过滤条件搜索
    /// var filtered = await searchService.SearchAsync("fabric api", "1.20.1", "fabric");
    /// 
    /// // 分页搜索
    /// var page2 = await searchService.SearchAsync("fabric api", page: 2, pageSize: 20);
    /// </code>
    /// </summary>
    public class ModSearchService
    {
        private readonly ModrinthAPIService _apiService;
        private readonly ILogger _logger;

        /// <summary>
        /// 构造搜索服务。<br />
        /// <br />
        /// 使用场景：<br />
        /// - 需要搜索模组时创建此服务<br />
        /// - 通过注入 ModrinthAPIService 复用HTTP连接
        /// </summary>
        /// <param name="apiService">Modrinth API 服务实例</param>
        public ModSearchService(ModrinthAPIService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _logger = Log.ForContext<ModSearchService>();
            _logger.Information("ModSearchService 已初始化");
        }

        /// <summary>
        /// 按关键词搜索模组（无过滤条件）。<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 将关键词传递给 <code>ModrinthAPIService.SearchProjectsAsync()</code><br />
        /// 2. 不设置 facets 过滤条件<br />
        /// 3. 返回搜索结果列表<br />
        /// <br />
        /// 使用示例：<br />
        /// <code>
        /// var results = await searchService.SearchAsync("inventory");
        /// foreach (var hit in results.Hits)
        /// {
        ///     Console.WriteLine($"{hit.Title}: {hit.Description}");
        /// }
        /// </code>
        /// </summary>
        /// <param name="query">搜索关键词</param>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页数量（最大100）</param>
        /// <returns>搜索响应对象</returns>
        public async Task<SearchResponse> SearchAsync(string query, int page = 1, int pageSize = 10)
        {
            _logger.Debug("执行无过滤搜索: Query={Query}, Page={Page}, PageSize={PageSize}", query, page, pageSize);
            var offset = (page - 1) * pageSize;
            return await _apiService.SearchProjectsAsync(query, null, null, offset, pageSize);
        }

        /// <summary>
        /// 按关键词搜索模组（带MC版本和加载器过滤）。<br />
        /// <br />
        /// 数据流：<br />
        /// 1. 构建过滤条件：<code>gameVersions = [gameVersion], loaders = [loader]</code><br />
        /// 2. 调用 <code>ModrinthAPIService.SearchProjectsAsync()</code> 并传入 facets<br />
        /// 3. 返回仅包含兼容指定MC版本和加载器的搜索结果<br />
        /// <br />
        /// 使用示例：<br />
        /// <code>
        /// // 搜索支持 1.20.1 + fabric 的模组
        /// var results = await searchService.SearchAsync("inventory", "1.20.1", "fabric");
        /// </code>
        /// </summary>
        /// <param name="query">搜索关键词</param>
        /// <param name="gameVersion">Minecraft 版本号（如 "1.20.1"）</param>
        /// <param name="loader">模组加载器名称（如 "fabric", "forge"）</param>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页数量（最大100）</param>
        /// <returns>搜索响应对象</returns>
        public async Task<SearchResponse> SearchAsync(
            string query, 
            string gameVersion, 
            string loader, 
            int page = 1, 
            int pageSize = 10)
        {
            _logger.Debug("执行带过滤搜索: Query={Query}, GameVersion={GameVersion}, Loader={Loader}, Page={Page}, PageSize={PageSize}",
                query, gameVersion, loader, page, pageSize);
            var offset = (page - 1) * pageSize;
            var gameVersions = new List<string> { gameVersion };
            var loaders = new List<string> { loader };
            return await _apiService.SearchProjectsAsync(query, gameVersions, loaders, offset, pageSize);
        }
    }
}