using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MOneClickDownloads.DataModel.Converters;

namespace MOneClickDownloads.DataModel.Enums
{
    /// <summary>
    /// 模组加载器类型枚举，表示从模组文件中检测到的加载器类型。
    /// 
    /// 用途：
    /// - 在 ModAnalysisResult 模型中标识模组文件所属的加载器平台
    /// - 通过分析 JAR 包内的元数据文件自动检测
    /// 
    /// 检测依据：
    /// - Fabric/Quilt: 存在 fabric.mod.json
    /// - Forge: 存在 META-INF/mods.toml
    /// - NeoForge: 存在 META-INF/neoforge.mods.toml
    /// </summary>
    public enum ModLoaderType
    {
        /// <summary>
        /// 未知：无法识别的加载器类型
        /// </summary>
        Unknown,

        /// <summary>
        /// Fabric 加载器
        /// </summary>
        Fabric,

        /// <summary>
        /// Forge 加载器
        /// </summary>
        Forge,

        /// <summary>
        /// NeoForge 加载器
        /// </summary>
        NeoForge,

        /// <summary>
        /// Quilt 加载器
        /// </summary>
        Quilt
    }
}