using System;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;
using MOneClickDownloads.DataModel.Converters;

namespace MOneClickDownloads.DataModel.Tag
{
    /// <summary>
    /// 游戏版本类型枚举，表示 Minecraft 游戏版本的发布类型。
    /// </summary>
    [JsonConverter(typeof(EnumMemberJsonConverter<GameVersionType>))]
    public enum GameVersionType
    {
        /// <summary>正式发布版</summary>
        [EnumMember(Value = "release")]
        Release,

        /// <summary>快照版（开发预览版）</summary>
        [EnumMember(Value = "snapshot")]
        Snapshot,

        /// <summary>远古Alpha版</summary>
        [EnumMember(Value = "alpha")]
        Alpha,

        /// <summary>远古Beta版</summary>
        [EnumMember(Value = "beta")]
        Beta
    }

    /// <summary>
    /// Minecraft 游戏版本模型，表示一个可用的 Minecraft 版本信息。
    /// 
    /// 用途：
    /// - 作为 Get a list of game versions API（GET /tag/game_version）的响应数组元素
    /// - 获取可用的 MC 版本列表供用户选择目标版本
    /// - 版本的 version 字段值用于过滤模组版本（如在 List project's versions 的 game_versions 查询参数）
    /// 
    /// 数据流转：
    /// - 从 Get a list of game versions API 响应反序列化
    /// - 用户选择 MC 版本后，将 version 值作为 game_versions 参数传入后续 API 调用
    /// - major 字段可用于优先展示重要版本
    /// </summary>
    public class GameVersion : IApiModel
    {
        /// <summary>
        /// Minecraft 版本号（如 "1.21.11"、"1.19"、"24w14a"）
        /// 
        /// 关键用途：作为搜索和版本过滤的 game_versions 参数值
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 游戏版本类型（release / snapshot / alpha / beta）
        /// </summary>
        [JsonPropertyName("version_type")]
        public GameVersionType VersionType { get; set; }

        /// <summary>
        /// 此游戏版本的发布日期（ISO-8601格式）
        /// </summary>
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        /// <summary>
        /// 是否为主要版本。主要版本用于 Featured Versions（推荐版本）的判断。
        /// 值为 true 的版本通常是正式发布的主版本（如 1.19、1.20 等）。
        /// </summary>
        [JsonPropertyName("major")]
        public bool Major { get; set; }

        /// <summary>
        /// 获取此模型对应的 API 端点路径。
        /// </summary>
        public string GetEndpoint() => "/tag/game_version";
    }
}