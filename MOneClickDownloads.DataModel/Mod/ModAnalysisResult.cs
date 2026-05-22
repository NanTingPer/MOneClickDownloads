using MOneClickDownloads.DataModel.Enums;

namespace MOneClickDownloads.DataModel.Mod
{
    /// <summary>
    /// 模组文件分析结果，表示从模组 JAR 包中提取的基本元数据信息。<br />
    /// <br />
    /// 用途：<br />
    /// - 由 ModAnalysisService 分析 JAR 包后返回<br />
    /// - 提供模组的 ID、名称、版本和检测到的加载器类型<br />
    /// - 可用于本地模组管理、模组信息展示等场景<br />
    /// <br />
    /// 数据来源：<br />
    /// - Fabric/Quilt: fabric.mod.json（id, name, version）<br />
    /// - Forge: META-INF/mods.toml（modId, displayName, version）<br />
    /// - NeoForge: META-INF/neoforge.mods.toml（modId, displayName, version）<br />
    /// </summary>
    public class ModAnalysisResult
    {
        /// <summary>
        /// 模组的唯一标识符（如 "fabric-api", "xaerominimap"）。
        /// 
        /// 来源字段：
        /// - Fabric/Quilt: fabric.mod.json → id
        /// - Forge/NeoForge: mods.toml / neoforge.mods.toml → modId
        /// </summary>
        public string ModId { get; set; } = string.Empty;

        /// <summary>
        /// 模组的显示名称（如 "Fabric API", "Xaero's Minimap"）。
        /// 
        /// 来源字段：
        /// - Fabric/Quilt: fabric.mod.json → name
        /// - Forge/NeoForge: mods.toml / neoforge.mods.toml → displayName
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 模组版本号（如 "0.147.0+26.2", "25.3.12"）。
        /// 
        /// 来源字段：
        /// - Fabric/Quilt: fabric.mod.json → version
        /// - Forge/NeoForge: mods.toml / neoforge.mods.toml → version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 检测到的模组加载器类型。
        /// 通过分析 JAR 包内存在的元数据文件自动判断。
        /// </summary>
        public ModLoaderType LoaderType { get; set; } = ModLoaderType.Unknown;
    }
}