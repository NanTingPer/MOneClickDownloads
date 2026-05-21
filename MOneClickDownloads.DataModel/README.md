# MOneClickDownloads.DataModel

Modrinth API 数据模型库，为 MOneClickDownloads 项目提供与 Modrinth API 交互的类型定义。

## 项目概述

本项目定义了与 [Modrinth API v2](https://docs.modrinth.com/) 交互所需的全部数据模型，支持以下核心功能：

- **搜索模组**：通过关键词、分类、MC版本、加载器等条件搜索 Modrinth 上的模组
- **获取模组详情**：获取指定模组的完整信息（描述、版本列表、依赖等）
- **获取版本信息**：获取模组的特定版本详情，包含下载链接和依赖列表
- **下载模组**：获取实际下载链接并支持递归下载依赖
- **标签查询**：获取可用的MC版本、加载器、分类等标签信息

## 项目结构

```
MOneClickDownloads.DataModel/
├── IApiModel.cs                              # 抽象接口，定义 GetEndpoint() 方法
├── Converters/
│   └── EnumMemberJsonConverter.cs            # 自定义枚举JSON转换器（兼容 netstandard2.1）
├── Enums/
│   ├── SideSupport.cs                        # 端侧支持（required/optional/unsupported/unknown）
│   ├── ProjectStatus.cs                      # 项目状态（approved/archived/draft/unlisted...）
│   ├── ProjectType.cs                        # 项目类型（mod/modpack/resourcepack/shader）
│   ├── VersionType.cs                        # 版本频道（release/beta/alpha）
│   ├── VersionStatus.cs                      # 版本状态（listed/archived/draft/unlisted...）
│   ├── DependencyType.cs                     # 依赖类型（required/optional/incompatible/embedded）
│   └── MonetizationStatus.cs                 # 变现状态（monetized/demonetized/force-demonetized）
├── Project/
│   ├── Project.cs                            # 完整项目详情（Get /project/{id|slug}）
│   ├── DonationUrl.cs                        # 捐赠链接
│   ├── LicenseInfo.cs                        # 许可证信息
│   ├── GalleryImage.cs                       # 画廊图片
│   └── ModeratorMessage.cs                   # 管理员消息
├── Version/
│   ├── ModrinthVersion.cs                    # 版本详情（Get /version/{id}）
│   ├── VersionFile.cs                        # 版本文件（含 FileType 枚举）
│   ├── Dependency.cs                         # 版本依赖关系
│   └── FileHashes.cs                         # 文件哈希（SHA-512/SHA-1）
├── Search/
│   ├── SearchResponse.cs                     # 搜索响应包装（Get /search）
│   └── ProjectHit.cs                         # 搜索命中项（精简版项目）
└── Tag/
    ├── GameVersion.cs                        # MC游戏版本（Get /tag/game_version）
    ├── Loader.cs                             # 加载器（Get /tag/loader）
    ├── Category.cs                           # 分类（Get /tag/category）
    ├── LicenseTag.cs                         # 许可证标签（Get /tag/license）
    ├── DonationPlatform.cs                   # 捐赠平台（Get /tag/donation_platform）
    ├── ProjectTypeTag.cs                     # 项目类型标签（Get /tag/project_type）
    └── SideTypeTag.cs                        # 端侧类型标签（Get /tag/side_type）
```

## 核心下载流程

以下流程展示了如何使用这些数据模型实现模组下载：

### 1. 搜索模组
```
调用 Search projects API (GET /search)
↓
返回 SearchResponse → 获取 ProjectHit[] → 提取 project_id
```

### 2. 获取模组版本
```
调用 List project's versions API (GET /project/{id}/version?game_versions=X&loaders=Y)
↓
返回 ModrinthVersion[] → 筛选兼容指定MC版本和加载器的版本
```

### 3. 下载文件
```
从 ModrinthVersion.Files[] 获取下载链接
↓
URL 格式: https://cdn.modrinth.com/data/{project_id}/versions/{version_id}/{filename}
可追加参数: ?mr_download_reason=standalone&mr_game_version={mc_version}&mr_loader={loader}
```

### 4. 递归下载依赖
```
遍历 ModrinthVersion.Dependencies[]
↓
筛选 DependencyType == Required 的依赖
↓
若 VersionId 有值 → 直接调用 Get /version/{version_id}
若 VersionId 为 null → 使用 ProjectId + game_versions + loaders 查询兼容版本
↓
重复步骤 3-4
```

## API 端点映射

| 数据模型 | API 端点 | 用途 |
|---------|----------|------|
| `Project` | `GET /project/{id\|slug}` | 获取完整项目详情 |
| `ModrinthVersion` | `GET /version/{id}` | 获取特定版本详情 |
| `ModrinthVersion[]` | `GET /project/{id\|slug}/version` | 获取项目版本列表（可过滤） |
| `SearchResponse` | `GET /search` | 搜索项目 |
| `GameVersion[]` | `GET /tag/game_version` | 获取可用MC版本列表 |
| `Loader[]` | `GET /tag/loader` | 获取可用加载器列表 |
| `Category[]` | `GET /tag/category` | 获取可用分类列表 |
| `LicenseTag[]` | `GET /tag/license` | 获取可用许可证列表 |
| `DonationPlatform[]` | `GET /tag/donation_platform` | 获取捐赠平台列表 |
| `ProjectTypeTag[]` | `GET /tag/project_type` | 获取项目类型列表 |
| `SideTypeTag[]` | `GET /tag/side_type` | 获取端侧类型列表 |

## 技术说明

- **目标框架**: `netstandard2.1`
- **JSON 库**: `System.Text.Json`
- **枚举处理**: 使用自定义 `EnumMemberJsonConverter<T>` 支持 `[EnumMember]` 属性映射
- **抽象接口**: 所有模型实现 `IApiModel` 接口，提供 `GetEndpoint()` 方法获取对应 API 路径

## 依赖关系

```
MOneClickDownloads.DataModel
└── System.Text.Json (>= 8.0.5)