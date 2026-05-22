# MOneClickDownloads.App

Avalonia UI 桌面应用程序，负责用户界面展示和交互。

## 依赖注入架构

本项目使用 `Microsoft.Extensions.DependencyInjection` 作为 DI 容器，采用 **导航服务（NavigationService）** 模式协调 ViewModel 的创建和页面间导航。

### 架构概览

```
App.axaml.cs
  └─ 构建 DI 容器 (ServiceCollection)
  └─ 解析 MainWindowViewModel
  └─ 调用 mainVm.NavigateToSearch() 触发初始导航

MainWindowViewModel (Singleton)
  └─ 注入 INavigationService
  └─ 管理 CurrentViewModel 状态
  └─ 委托 INavigationService 创建子 ViewModel

INavigationService / NavigationService (Singleton)
  └─ 从 IServiceProvider 创建各页面 ViewModel
  └─ 使用 Lazy<MainWindowViewModel> 延迟获取主 VM，避免循环依赖
  └─ CreateSearchViewModel() → 从容器解析
  └─ CreateDetailViewModel(...) → ActivatorUtilities.CreateInstance

ModSearchViewModel (Transient)
  └─ 注入 INavigationService + ModSearchService

ModDetailViewModel (Transient)
  └─ 注入 INavigationService + ModDownloadService + ModrinthAPIService + ConfigService
  └─ 通过 ActivatorUtilities.CreateInstance 创建（需要运行时参数）
```

### 文件结构

```
DI/
  ├── INavigationService.cs      # 导航服务接口
  ├── NavigationService.cs       # 导航服务实现
  └── ServiceRegistration.cs     # 服务注册扩展方法
ViewModels/
  ├── ViewModelBase.cs           # ViewModel 基类
  ├── MainWindowViewModel.cs     # 主窗口 ViewModel（管理导航状态）
  ├── ModSearchViewModel.cs      # 模组搜索页 ViewModel
  └── ModDetailViewModel.cs      # 模组详情页 ViewModel
App.axaml.cs                     # 应用入口，构建 DI 容器
ViewLocator.cs                   # Avalonia View-ViewModel 匹配器
```

### 服务注册（ServiceRegistration.cs）

所有服务和 ViewModel 的注册集中在 `ServiceRegistration.AddAppServices()` 中：

```csharp
public static IServiceCollection AddAppServices(this IServiceCollection services)
{
    // 基础设施
    services.AddSingleton<INavigationService, NavigationService>();

    // 服务层（Singleton）
    services.AddSingleton<ModrinthAPIService>();
    services.AddSingleton<ModSearchService>();
    services.AddSingleton<IModAnalysisService, ModAnalysisService>();
    services.AddSingleton<IModConflictService, ModConflictService>();
    services.AddSingleton<ModDownloadService>();
    services.AddSingleton<ConfigService>(sp => { /* 工厂注册 */ });

    // ViewModel 层
    services.AddSingleton<MainWindowViewModel>();
    services.AddTransient<ModSearchViewModel>();

    return services;
}
```

### 服务生命周期

| 服务 | 生命周期 | 理由 |
|------|----------|------|
| `INavigationService` | Singleton | 全局唯一导航协调器 |
| `ModrinthAPIService` | Singleton | 共享 HttpClient 连接池 |
| `ModSearchService` | Singleton | 无状态，依赖 Singleton API 服务 |
| `IModAnalysisService` | Singleton | 无状态 |
| `IModConflictService` | Singleton | 无状态 |
| `ModDownloadService` | Singleton | 无状态 |
| `ConfigService` | Singleton | 全局唯一配置管理 |
| `MainWindowViewModel` | Singleton | 应用主 ViewModel，管理导航状态 |
| `ModSearchViewModel` | Transient | 每次导航到搜索页时创建新实例 |
| `ModDetailViewModel` | Transient | 每次查看模组详情时创建新实例 |

### 职责分离

| 组件 | 职责 |
|------|------|
| `MainWindowViewModel` | 只管理 `CurrentViewModel` 状态，调用 `INavigationService` 执行导航 |
| `INavigationService` | 从 DI 容器创建子 ViewModel，提供对主 ViewModel 的延迟访问 |
| `NavigationService` | 实现导航逻辑，使用 `Lazy<T>` 解决循环依赖 |
| `ViewLocator` | 根据 ViewModel 类型自动匹配对应的 View |
| 子 ViewModel | 通过 `INavigationService.MainViewModel` 执行反向导航 |

### 循环依赖解决方案

`MainWindowViewModel` → 创建子 ViewModel → 子 ViewModel 需要访问 `MainWindowViewModel` 形成循环依赖。

解决方案：`NavigationService` 使用 `Lazy<MainWindowViewModel>` 延迟获取主 ViewModel，在所有 Singleton 创建完成后再解析，避免无限循环。

### 添加新页面

1. 创建 `XxxViewModel`（放在 `ViewModels/` 目录）
2. 创建对应的 `XxxView`（放在 `Views/` 目录，命名约定：`Xxx` 替换 ViewModel → View）
3. 在 `INavigationService` 中添加 `CreateXxxViewModel()` 方法
4. 在 `NavigationService` 中实现该方法
5. 在 `ServiceRegistration` 中注册 ViewModel（Transient 或 Singleton 视需求而定）
6. 在需要导航的地方调用 `INavigationService.CreateXxxViewModel()`