# 图标缓存服务（IconCache）

## 概述

图标缓存服务用于将模组图标从网络下载并持久化到本地磁盘，避免每次显示模组列表时重复请求网络图标，提升 UI 加载速度并减少网络流量。

## 缓存目录

```
{AppBaseDirectory}/icon_cache/
```

应用启动时自动创建该目录（如果不存在）。

## 文件命名规则

缓存文件名格式：

```
{modId}_icon.{ext}
```

| 部分 | 说明 |
|------|------|
| `modId` | 模组标识符，不同场景使用不同值（见下文） |
| `icon` | 固定文件名，简化识别 |
| `ext` | 从原始图标 URL 提取的扩展名（如 `png`、`webp`），无法提取时默认 `png` |

### 各场景下的 modId 取值

| 场景 | modId 来源 | 示例文件名 | 说明 |
|------|-----------|-----------|------|
| 模组搜索 | `ProjectId`（base62 哈希） | `a1b2c3d4_icon.png` | Modrinth 平台分配的唯一标识 |
| 本地模组 | `ProjectId`（base62 哈希） | `a1b2c3d4_icon.png` | 同上 |
| 收藏模组 | `Slug`（人类可读名称） | `sodium_icon.png` | 对应 jar 包中的 `modid`，便于人类识别 |

### Project 属性与 Jar 包对应关系

| Project 属性 | Jar 包中对应字段 | 说明 |
|-------------|----------------|------|
| `Slug` | `modid` | 模组的唯一标识符，人类可读（如 `sodium`、`iris`） |
| `Title` | `Name` | 模组的显示名称（如 `Sodium`、`Iris Shaders`） |
| `ProjectId` | — | Modrinth 平台生成的 base62 哈希标识 |

## API

### `IIconCacheService` 接口

| 方法 | 说明 |
|------|------|
| `GetCachedIconPath(modId, iconUrl)` | 检查缓存是否存在，返回本地文件路径；未缓存返回 `null` |
| `CacheIconAsync(modId, iconUrl)` | 下载单个图标并保存到缓存目录，返回本地文件路径 |
| `CacheIconsAsync(items)` | 批量下载图标，自动跳过已缓存的项 |

### 缓存流程

1. 调用方传入 `modId` + `iconUrl`
2. 根据 URL 提取扩展名，构建缓存文件名 `{modId}_icon.{ext}`
3. 如果缓存文件已存在且大小 > 0，直接返回本地路径
4. 否则从网络下载，先写入临时文件，成功后重命名为正式缓存文件（防止写入中断导致损坏）