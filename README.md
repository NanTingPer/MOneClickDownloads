# MOneClickDownloads

<p align="center">
  <strong>基于 Modrinth API 的 Minecraft 模组一键下载桌面工具</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-purple" alt=".NET 10" />
  <img src="https://img.shields.io/badge/UI-Avalonia%2012-blue" alt="Avalonia UI 12" />
  <img src="https://img.shields.io/badge/License-MIT-green" alt="License" />
  <img src="https://img.shields.io/badge/Platform-Windows-lightgrey" alt="Platform" />
</p>

---

## 📖 项目简介

MOneClickDownloads 是一款基于 [Modrinth](https://modrinth.com/) API 的 Minecraft 模组桌面客户端。通过直观的图形界面，用户可以搜索、浏览、收藏并一键下载模组及其全部依赖，无需手动处理版本兼容性和依赖关系。

## ✨ 功能特性

### 🔍 模组搜索与浏览
- **关键词搜索**：通过 Modrinth API 搜索模组、整合包、资源包、光影等
- **版本过滤**：按 Minecraft 版本、加载器（Fabric/Forge/NeoForge/Quilt）筛选
- **版本分类浏览**：按 MC 版本分组展示所有可用版本，支持加载器/发布状态过滤
- **版本过滤记忆**：MC 版本过滤器自动持久化，跨页面保持

### 📥 智能下载
- **一键下载**：选择版本后自动下载模组文件
- **依赖递归下载**：自动识别并递归下载所有 Required 依赖
- **循环依赖防护**：已下载项目自动跳过，防止无限递归
- **下载重试**：文件下载失败自动重试 3 次，每次间隔 2 秒
- **事务性下载**：下载失败或取消时自动回滚所有已下载文件
- **进度报告**：实时显示下载进度（当前文件、完成数、百分比）

### ⚠️ 冲突检测
- **本地扫描**：下载前扫描目标文件夹中的所有 `.jar` 文件
- **智能检测**：自动检测同 ID 模组的版本冲突（相同/更高/更低版本）
- **用户决策**：弹出对话框让用户选择跳过、替换或保留两者
- **版本比较**：基于 SemVer 规则进行版本号比较

### ⭐ 收藏夹系统
- **合集管理**：创建、重命名、删除收藏夹合集
- **右键收藏**：在搜索结果中右键快速添加到收藏夹
- **批量下载**：一键下载整个收藏夹中的所有模组
- **兼容性预检**：批量下载前自动检查所有模组的版本兼容性
- **JSON 持久化**：收藏数据以 JSON 文件存储，线程安全读写

### 🎨 界面体验
- **Fluent Design**：基于 Avalonia UI 12 + Fluent 主题
- **侧边栏导航**：汉堡菜单侧边栏，支持搜索页/收藏夹切换
- **原子化 CSS**：UI 样式采用原子化 CSS 范式（参考 Tailwind 命名规范）
- **深色/浅色**：跟随系统主题自动切换

## 🛠 技术栈

| 类别 | 技术 |
|------|------|
| **运行时** | .NET 10.0 |
| **UI 框架** | Avalonia UI 12.0.3（跨平台桌面） |
| **架构模式** | MVVM（CommunityToolkit.Mvvm 8.4.1） |
| **依赖注入** | Microsoft.Extensions.DependencyInjection |
| **日志** | Serilog（文件 Sink + Compact 格式） |
| **配置** | Tomlyn（TOML 格式配置文件） |
| **JSON** | System.Text.Json |
| **测试** | MSTest |

## 🏗 项目架构

项目采用分层架构，由 4 个项目组成：

```
MOneClickDownloads (Solution)
│
├── MOneClickDownloads.App          # 表现层 - Avalonia UI 桌面应用
│   ├── ViewModels/                 # MVVM ViewModel 层
│   ├── Views/                      # Avalonia XAML 视图层
│   ├── DI/                         # 依赖注入 & 导航服务
│   ├── Configs/                    # 配置键常量
│   ├── Converters/                 # 值转换器
│   ├── Logging/                    # Serilog 日志配置
│   ├── Models/                     # UI 展示模型
│   └── Assets/                     # 应用资源
│
├── MOneClickDownloads.Service      # 业务层 - 核心业务逻辑
│   ├── ModrinthAPIService.cs       # Modrinth API 封装（HTTP 通信）
│   ├── ModSearchService.cs         # 模组搜索服务
│   ├── ModDownloadService.cs       # 模组下载服务（事务、冲突、递归）
│   ├── ModAnalysisService.cs       # JAR 文件模组元数据提取
│   ├── ModConflictService.cs       # 冲突检测服务
│   ├── FavoriteService.cs          # 收藏夹服务（JSON 持久化）
│   ├── ConfigService.cs            # 应用配置服务（TOML）
│   ├── LocalModInventory.cs        # 本地模组清单扫描
│   ├── Analyzers/                  # 加载器专用分析器
│   │   ├── FabricModAnalyzer.cs    #   Fabric/Quilt
│   │   ├── ForgeModAnalyzer.cs     #   Forge
│   │   └── NeoForgeModAnalyzer.cs  #   NeoForge
│   └── Models/                     # 业务数据模型
│
├── MOneClickDownloads.DataModel    # 数据层 - Modrinth API 数据模型
│   ├── Project/                    # 项目详情模型
│   ├── Version/                    # 版本模型（含依赖、文件哈希）
│   ├── Search/                     # 搜索响应模型
│   ├── Tag/                        # 标签模型（版本/加载器/分类）
│   ├── Favorites/                  # 收藏夹数据模型
│   ├── Mod/                        # 本地模组分析结果模型
│   ├── Enums/                      # 枚举定义
│   └── Converters/                 # JSON 转换器
│
└── MOneClickDownloads.Test         # 测试层 - 单元测试
    ├── ModAnalysisServiceTest.cs   # 模组分析服务测试
    └── Test1.cs
```

### 架构依赖关系

```
┌──────────────────────────────┐
│   MOneClickDownloads.App     │  ← 表现层：UI、ViewModel、DI
│   (.NET 10.0, Avalonia UI)   │
└────────────┬─────────────────┘
             │ 引用
     ┌───────┴───────┐
     │               │
     ▼               ▼
┌──────────┐  ┌──────────────────────┐
│ DataModel│  │   Service            │  ← 业务层：API、下载、分析
│(netstandard│  │   (.NET 10.0)        │
│   2.1)   │  └──────────────────────┘
└──────────┘           │
                       │ 引用
                       ▼
                ┌──────────┐
                │ DataModel │  ← 数据层：API 类型定义
                └──────────┘
```

## 🔄 核心流程

### 模组下载流程

```
用户选择版本 → 选择保存目录 → 获取兼容版本列表
    → 按版本类型筛选（Release > Beta > Alpha）
    → 扫描本地文件夹构建模组清单
    → 收集待下载文件（主模组 + Required 依赖递归）
    → 逐个下载：
        ① 冲突检测 → 无冲突直接下载 / 有冲突回调用户
        ② 带重试下载（3 次重试，2 秒间隔）
        ③ 实时进度报告
    → 全部成功 → 完成
    → 失败/取消 → 回滚所有已下载文件
```

### 合集批量下载流程

```
选择收藏夹 → 选择保存目录
    → 并发获取所有模组版本信息
    → 用户选择 MC 版本 + 加载器
    → 计算兼容性状态（兼容/仅预览版/不兼容）
    → 预检弹窗确认不兼容和预览版模组
    → 逐个下载兼容模组（含冲突检测和依赖递归）
    → 汇总下载结果
```

## 🧩 设计模式

### MVVM + 导航服务

项目使用 `NavigationService` 模式协调 ViewModel 创建和页面导航：

```
App.axaml.cs
  └─ 构建 DI 容器
  └─ 解析 MainWindowViewModel
  └─ 触发初始导航

MainWindowViewModel (Singleton)
  └─ 管理 CurrentViewModel 状态
  └─ 委托 NavigationService 创建子 ViewModel

NavigationService (Singleton)
  └─ 从 IServiceProvider 创建各页面 ViewModel
  └─ 使用 Lazy<T> 延迟解析，避免循环依赖
```

### 服务生命周期

| 服务 | 生命周期 | 理由 |
|------|----------|------|
| `INavigationService` | Singleton | 全局唯一导航协调器 |
| `ModrinthAPIService` | Singleton | 共享 HttpClient 连接池 |
| `ModSearchService` | Singleton | 无状态 |
| `ModDownloadService` | Singleton | 无状态 |
| `IModAnalysisService` | Singleton | 无状态 |
| `ConfigService` | Singleton | 全局唯一配置管理 |
| `FavoriteService` | Singleton | 全局唯一收藏夹数据源 |
| `MainWindowViewModel` | Singleton | 主 ViewModel，管理导航状态 |
| `ModSearchViewModel` | Transient | 每次导航创建新实例 |
| `ModDetailViewModel` | Transient | 每次查看创建新实例 |
| `FavoritesViewModel` | Transient | 每次导航创建新实例 |
| `CollectionDownloadViewModel` | Transient | 每次下载创建新实例 |

## 🚀 构建与运行

### 环境要求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/) 或更高版本
- Windows 操作系统（Avalonia UI 也支持 macOS/Linux，但当前项目配置为 WinExe）

### 构建

```bash
# 克隆仓库
git clone https://github.com/NanTingPer/MOneClickDownloads.git
cd MOneClickDownloads

# 构建解决方案
dotnet build

# 运行应用
dotnet run --project MOneClickDownloads.App
```

### 运行测试

```bash
dotnet test
```

## 📁 配置说明

应用使用 TOML 格式配置文件，由 `ConfigService` 管理。配置项包括：

| 配置键 | 类型 | 说明 |
|--------|------|------|
| `ActiveMcVersionFilter` | `string` | 上次选择的 MC 版本过滤器 |

配置文件存储在应用数据目录中，自动创建和管理。

## 🔧 开发指南

### 添加新页面

1. 创建 `XxxViewModel`（放在 `ViewModels/` 目录）
2. 创建对应的 `XxxView`（放在 `Views/` 目录，命名约定：`Xxx` 替换 `ViewModel` → `View`）
3. 在 `INavigationService` 中添加 `CreateXxxViewModel()` 方法
4. 在 `NavigationService` 中实现该方法
5. 在 `ServiceRegistration` 中注册 ViewModel（Transient 或 Singleton 视需求而定）
6. 在需要导航的地方调用 `INavigationService.CreateXxxViewModel()`

### 添加新的模组文件分析器

1. 在 `Service/Analyzers/` 目录创建 `XxxModAnalyzer.cs`
2. 实现 `IModFileAnalyzer` 接口
3. 在 `ModAnalysisService` 中注册该分析器

### 样式开发规范

UI 样式严格遵循**原子化 CSS** 范式：

- 每个类名只完成一件视觉工作（如 `.flex`、`.items-center`、`.gap-2`）
- 命名参考 Tailwind CSS 风格
- 禁止编写复合样式 class
- 禁止使用内联 `style` 属性
- 所有原子类在 `Styles.axaml` 中全局定义，跨组件复用

## 📚 相关文档

- [MOneClickDownloads.App/README.md](MOneClickDownloads.App/README.md) - 表现层详细架构文档
- [MOneClickDownloads.DataModel/README.md](MOneClickDownloads.DataModel/README.md) - 数据模型层 API 端点映射
- [MOneClickDownloads.Service/README.md](MOneClickDownloads.Service/README.md) - 服务层 API 使用文档
- [MOneClickDownloads.Service/模组下载流程.md](MOneClickDownloads.Service/模组下载流程.md) - 下载流程详解（冲突检测、事务回滚）

## 📄 许可证

本项目基于 [MIT License](LICENSE) 开源。

## 🔗 链接

- [Modrinth API 文档](https://docs.modrinth.com/)
- [Avalonia UI](https://avaloniaui.net/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/zh-cn/dotnet/communitytoolkit/mvvm/)