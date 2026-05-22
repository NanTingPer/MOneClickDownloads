# MOneClickDownloads.Service

Modrinth API 服务层，为 MOneClickDownloads 项目提供模组搜索、下载及依赖管理等核心业务逻辑。

## 项目概述

本项目封装了与 [Modrinth API v2](https://docs.modrinth.com/) 交互的全部业务逻辑，基于 `MOneClickDownloads.DataModel` 数据模型层，提供以下核心功能：

- **API 封装**：统一管理 Modrinth API 的 HTTP 请求、JSON 序列化和 UserAgent 配置
- **模组搜索**：支持关键词搜索，可按 MC 版本、加载器过滤，支持分页
- **模组下载**：支持推荐下载（最新稳定版）和指定版本类型下载
- **依赖递归下载**：自动递归下载所有 Required 依赖，防止循环依赖
- **事务性下载**：下载失败或取消时自动回滚所有已下载文件
- **重试机制**：文件下载失败自动重试 3 次，每次间隔 2 秒
- **进度报告**：通过 `IProgress<T>` 实时报告下载进度

## 项目结构

```
MOneClickDownloads.Service/
├── MOneClickDownloads.Service.csproj       # 项目文件（net10.0）
├── ModrinthAPIService.cs                   # Modrinth API 封装服务
├── ModSearchService.cs                     # 模组搜索服务
├── ModDownloadService.cs                   # 模组下载服务（含冲突检测、DownloadException）
├── ModAnalysisService.cs                   # 模组文件分析服务
├── IModAnalysisService.cs                  # 模组分析服务接口
├── LocalModInventory.cs                    # 本地模组清单工具
├── ConfigService.cs                        # 应用配置服务
├── 模组下载流程.md                          # 下载流程详细文档
├── Analyzers/
│   ├── IModFileAnalyzer.cs                 # 模组文件分析器接口
│   ├── FabricModAnalyzer.cs                # Fabric/Quilt 分析器
│   ├── ForgeModAnalyzer.cs                 # Forge 分析器
│   └── NeoForgeModAnalyzer.cs              # NeoForge 分析器
└── Models/
    ├── DownloadProgress.cs                 # 下载进度模型
    ├── DownloadResult.cs                   # 下载结果模型
    └── ModConflictInfo.cs                  # 模组冲突信息模型
```

## 核心服务

### ModrinthAPIService

底层 API 封装服务，负责所有与 Modrinth API 的 HTTP 通信。

**职责：**
- 封装所有 Modrinth API 端点请求
- 管理 HttpClient 生命周期和配置
- 处理 JSON 序列化/反序列化
- 为所有请求添加统一的 UserAgent（`NanTingPer/MOneClickDownloads`）

**两种构造模式：**

| 模式 | 构造方式 | 适用场景 |
|------|---------|---------|
| 外部注入 | `new ModrinthAPIService(httpClient)` | ASP.NET Core DI、单元测试 |
| 自维护 | `new ModrinthAPIService()` | 控制台应用、独立服务 |

**提供的 API 方法：**

| 方法 | 对应 API 端点 | 说明 |
|------|-------------|------|
| `GetProjectAsync(idOrSlug)` | `GET /project/{id\|slug}` | 获取项目详情 |
| `SearchProjectsAsync(query, gameVersions, loaders, offset, limit)` | `GET /search` | 搜索项目 |
| `GetProjectVersionsAsync(projectId, gameVersions, loaders)` | `GET /project/{id\|slug}/version` | 获取项目版本列表 |
| `GetVersionAsync(versionId)` | `GET /version/{id}` | 获取特定版本详情 |
| `DownloadFileAsync(url, savePath, progress, cancellationToken)` | — | 下载文件到本地 |

**使用示例：**

```csharp
// 外部传入模式（推荐用于 DI 环境）
using var apiService = new ModrinthAPIService(httpClient);

// 自维护模式（适用于控制台应用）
using var apiService = new ModrinthAPIService();

// 获取项目详情
var project = await apiService.GetProjectAsync("fabric-api");
Console.WriteLine(project.Title);

// 搜索项目
var results = await apiService.SearchProjectsAsync("fabric api", null, null, 0, 10);

// 获取版本列表（带过滤）
var versions = await apiService.GetProjectVersionsAsync(
    "fabric-api",
    new List<string> { "1.20.1" },
    new List<string> { "fabric" });
```

### ModSearchService

高层搜索服务，封装搜索逻辑，提供友好的分页搜索接口。

**职责：**
- 封装 `ModrinthAPIService` 的搜索方法
- 构建 facets 过滤条件（MC 版本、加载器）
- 提供分页搜索接口（页码从 1 开始）

**提供的 API 方法：**

| 方法 | 说明 |
|------|------|
| `SearchAsync(query, page, pageSize)` | 无过滤条件搜索 |
| `SearchAsync(query, gameVersion, loader, page, pageSize)` | 带 MC 版本和加载器过滤搜索 |

**使用示例：**

```csharp
var searchService = new ModSearchService(apiService);

// 基础搜索
var results = await searchService.SearchAsync("fabric api");

// 带过滤条件搜索
var filtered = await searchService.SearchAsync("fabric api", "1.20.1", "fabric");

// 分页搜索（第2页，每页20条）
var page2 = await searchService.SearchAsync("fabric api", page: 2, pageSize: 20);

// 遍历结果
foreach (var hit in results.Hits)
{
    Console.WriteLine($"{hit.Title}: {hit.Description}");
}
```

### ModDownloadService

核心下载服务，实现模组下载、依赖递归下载、版本筛选等下载逻辑。

**职责：**
- 根据 MC 版本和加载器筛选并下载模组
- 支持推荐下载（最新稳定版）和指定版本类型下载
- 递归下载必需依赖（Required），支持 3 次重试
- 事务性下载：失败或取消时回滚所有已下载文件
- 通过 `IProgress<DownloadProgress>` 报告下载进度
- 通过 `CancellationToken` 支持取消操作

**核心下载流程：**

```
1. 调用 GetProjectVersionsAsync 获取兼容版本列表
       ↓
2. 按版本类型筛选（release > beta > alpha）并选取目标版本
       ↓
3. 获取版本的 primary 文件下载链接
       ↓
4. 下载文件到本地（失败重试3次，间隔2秒）
       ↓
5. 遍历 Dependencies，筛选 Required 依赖
       ↓
6. 若 VersionId 有值 → 直接获取该版本
   若 VersionId 为 null → 通过 ProjectId 查询兼容版本
       ↓
7. 递归执行步骤 3-6（已下载项目跳过，防止循环依赖）
       ↓
8. 若任何步骤失败或被取消 → 回滚所有已下载文件
```

**提供的 API 方法：**

| 方法 | 说明 |
|------|------|
| `DownloadRecommendedAsync(projectId, gameVersion, loader, saveDirectory, progress, cancellationToken)` | 推荐下载：最新 Release 版本 |
| `DownloadByVersionAsync(projectId, gameVersion, loader, versionType, saveDirectory, progress, cancellationToken)` | 指定版本类型下载 |
| `DownloadWithDependenciesAsync(projectId, gameVersion, loader, saveDirectory, versionType, progress, cancellationToken)` | 带依赖下载 |

**使用示例：**

```csharp
var apiService = new ModrinthAPIService();
var downloadService = new ModDownloadService(apiService);

// 进度回调
var progress = new Progress<DownloadProgress>(p =>
    Console.WriteLine($"{p.Percentage:F1}% ({p.CompletedCount}/{p.TotalCount}) - {p.CurrentFileName}"));

using var cts = new CancellationTokenSource();

// 推荐下载（最新稳定版 + 依赖）
var results = await downloadService.DownloadRecommendedAsync(
    "fabric-api", "1.20.1", "fabric", @"C:\mods", progress, cts.Token);

if (results.Count == 0)
    Console.WriteLine("该模组没有支持此 MC 版本的稳定版");

// 指定版本类型下载（Beta 版 + 依赖）
var betaResults = await downloadService.DownloadWithDependenciesAsync(
    "fabric-api", "1.20.1", "fabric", @"C:\mods", VersionType.Beta, progress, cts.Token);

// 遍历下载结果
foreach (var r in results)
{
    Console.WriteLine($"{(r.IsDependency ? "[依赖]" : "[主模组]")} {r.FileName} ({r.FileSize} bytes)");
}
```

**版本筛选优先级：**

在依赖自动选取版本时，按以下优先级选择：
1. Release 版本（同类型取最新）
2. Beta 版本（同类型取最新）
3. Alpha 版本（同类型取最新）

## 数据模型

### DownloadProgress

下载进度报告模型，通过 `IProgress<DownloadProgress>` 向调用方报告当前下载状态。

| 属性 | 类型 | 说明 |
|------|------|------|
| `CompletedCount` | `int` | 已完成下载的文件数量 |
| `TotalCount` | `int` | 需要下载的文件总数量（包含依赖） |
| `CurrentFileName` | `string` | 当前正在下载的文件名 |
| `CurrentProjectName` | `string` | 当前正在下载的项目名称 |
| `Percentage` | `double` | 下载进度百分比（0-100），自动计算 |

### DownloadResult

单个文件的下载结果模型，记录一次下载操作的结果信息。

| 属性 | 类型 | 说明 |
|------|------|------|
| `FilePath` | `string` | 下载的文件保存路径 |
| `SourceUrl` | `string` | 下载来源 URL |
| `ProjectId` | `string` | 所属模组项目 ID |
| `ProjectName` | `string` | 所属模组项目名称 |
| `VersionId` | `string` | 下载的版本 ID |
| `FileName` | `string` | 下载的文件名 |
| `FileSize` | `long` | 文件大小（字节） |
| `IsDependency` | `bool` | 是否为依赖下载 |

### DownloadException

自定义下载异常，表示下载过程中发生的错误（如重试耗尽、无可下载文件、依赖无兼容版本等）。下载失败或取消时会触发事务回滚。

## 服务间关系

```
┌─────────────────────┐
│   ModrinthAPIService │  ← 底层：HTTP 请求、JSON 序列化
│   (IDisposable)      │
└──────────┬──────────┘
           │ 注入
     ┌─────┴─────┐
     │           │
     ▼           ▼
┌──────────┐  ┌───────────────────┐
│ModSearch │  │ModDownloadService │  ← 高层：业务逻辑
│ Service  │  │                   │
└──────────┘  └───────────────────┘
```

推荐在应用中创建一个 `ModrinthAPIService` 实例，注入到 `ModSearchService` 和 `ModDownloadService` 中复用 HTTP 连接。

### LocalModInventory

本地模组清单工具，扫描指定文件夹中的所有 JAR 文件并构建已安装模组的索引，用于下载前的冲突检测。

**职责：**
- 扫描指定文件夹内的所有 `.jar` 文件
- 使用 `IModAnalysisService` 分析每个 JAR 包的模组元数据
- 提供按模组 ID 查询已安装模组的能力
- 提供已安装模组文件路径查询能力

**构造函数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `folderPath` | `string` | 要扫描的模组文件夹路径 |
| `analysisService` | `IModAnalysisService` | 模组文件分析服务 |

**提供的 API 方法：**

| 方法 | 说明 |
|------|------|
| `ScanAsync()` | 扫描文件夹内所有 `.jar` 文件，构建索引 |
| `FindByModId(modId)` | 按模组 ID 查找已安装的模组（忽略大小写） |
| `GetModFilePath(modId)` | 获取已安装模组文件的完整路径 |
| `ContainsMod(modId)` | 检查指定模组 ID 是否已安装 |
| `InstalledMods` | 获取扫描后的已安装模组列表 |
| `Count` | 已安装模组数量 |

**使用示例：**

```csharp
var analysisService = new ModAnalysisService();
var inventory = new LocalModInventory(@"C:\mods", analysisService);
await inventory.ScanAsync();

Console.WriteLine($"已安装 {inventory.Count} 个模组");

foreach (var mod in inventory.InstalledMods)
    Console.WriteLine($"  {mod.Name} v{mod.Version} ({mod.LoaderType})");

var existing = inventory.FindByModId("fabric-api");
if (existing != null)
    Console.WriteLine($"已安装: {existing.Name} v{existing.Version}");
```

### ModConflictCallback（冲突回调委托）

当下载前检测到本地已有同ID模组时调用的回调委托。

```csharp
public delegate Task<ModConflictResolution> ModConflictCallback(ModConflictInfo conflictInfo);
```

**冲突类型（ModConflictType）：**

| 类型 | 说明 |
|------|------|
| `None` | 无冲突 |
| `SameModExists` | 完全相同的模组ID和版本已存在 |
| `HigherVersionExists` | 本地已有更高版本 |
| `LowerVersionExists` | 本地已有更低版本（可升级） |

**解决策略（ModConflictResolution）：**

| 策略 | 说明 |
|------|------|
| `Skip` | 跳过，不下载新版本 |
| `Replace` | 删除本地旧文件，下载新版本 |
| `KeepBoth` | 保留两者，新文件以 `文件名-版本号.jar` 格式保存 |

详细的冲突检测流程、版本比较规则和事务机制请参见 [模组下载流程.md](模组下载流程.md)。

## 技术说明

- **目标框架**: `net10.0`
- **JSON 库**: `System.Text.Json`（系统内置）
- **HTTP 客户端**: `HttpClient` + `SocketsHttpHandler`（连接池生命周期 10 分钟）
- **异步模式**: 全异步 API（`async/await`）
- **取消支持**: 全面支持 `CancellationToken`
- **错误处理**: 自动重试（3 次）+ 事务回滚

## 依赖关系

```
MOneClickDownloads.Service
├── MOneClickDownloads.DataModel
│   └── System.Text.Json (>= 8.0.5)
└── System.Net.Http (内置)