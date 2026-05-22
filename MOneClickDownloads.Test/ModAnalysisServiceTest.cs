using System.IO.Compression;
using System.Text;
using MOneClickDownloads.DataModel.Enums;
using MOneClickDownloads.DataModel.Mod;
using MOneClickDownloads.Service;

namespace MOneClickDownloads.Test
{
    [TestClass]
    public sealed class ModAnalysisServiceTest
    {
        private readonly ModAnalysisService _service = new();

        #region Fabric 分析器测试

        [TestMethod]
        public async Task AnalyzeAsync_FabricMod_ReturnsCorrectResult()
        {
            // Arrange: 创建包含 fabric.mod.json 的测试 JAR
            var jarPath = CreateTestJar(entries: new Dictionary<string, string>
            {
                ["fabric.mod.json"] = """
                {
                    "id": "fabric-api",
                    "name": "Fabric API",
                    "version": "0.147.0+26.2"
                }
                """
            });

            try
            {
                // Act
                var result = await _service.AnalyzeAsync(jarPath);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("fabric-api", result.ModId);
                Assert.AreEqual("Fabric API", result.Name);
                Assert.AreEqual("0.147.0+26.2", result.Version);
                Assert.AreEqual(ModLoaderType.Fabric, result.LoaderType);
            }
            finally
            {
                CleanupFile(jarPath);
            }
        }

        [TestMethod]
        public async Task AnalyzeAsync_QuiltMod_ReturnsFabricLoaderType()
        {
            // Arrange: Quilt 使用与 Fabric 相同的 fabric.mod.json 格式
            var jarPath = CreateTestJar(entries: new Dictionary<string, string>
            {
                ["fabric.mod.json"] = """
                {
                    "id": "quilted-fabric-api",
                    "name": "Quilted Fabric API",
                    "version": "7.0.0"
                }
                """
            });

            try
            {
                // Act
                var result = await _service.AnalyzeAsync(jarPath);

                // Assert: Quilt 与 Fabric 共用 fabric.mod.json，检测为 Fabric
                Assert.IsNotNull(result);
                Assert.AreEqual("quilted-fabric-api", result.ModId);
                Assert.AreEqual(ModLoaderType.Fabric, result.LoaderType);
            }
            finally
            {
                CleanupFile(jarPath);
            }
        }

        [TestMethod]
        public async Task AnalyzeAsync_FabricMod_MissingId_ReturnsNull()
        {
            // Arrange: fabric.mod.json 缺少 id 字段
            var jarPath = CreateTestJar(entries: new Dictionary<string, string>
            {
                ["fabric.mod.json"] = """
                {
                    "name": "Some Mod",
                    "version": "1.0.0"
                }
                """
            });

            try
            {
                // Act
                var result = await _service.AnalyzeAsync(jarPath);

                // Assert: 缺少 id 时应返回 null
                Assert.IsNull(result);
            }
            finally
            {
                CleanupFile(jarPath);
            }
        }

        #endregion

        #region Forge 分析器测试

        [TestMethod]
        public async Task AnalyzeAsync_ForgeMod_ReturnsCorrectResult()
        {
            // Arrange: 创建包含 META-INF/mods.toml 的测试 JAR
            var jarPath = CreateTestJar(entries: new Dictionary<string, string>
            {
                ["META-INF/mods.toml"] = """
                [[mods]]
                modId = "xaerominimap"
                version = "25.3.12"
                displayName = "Xaero's Minimap"
                """
            });

            try
            {
                // Act
                var result = await _service.AnalyzeAsync(jarPath);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("xaerominimap", result.ModId);
                Assert.AreEqual("Xaero's Minimap", result.Name);
                Assert.AreEqual("25.3.12", result.Version);
                Assert.AreEqual(ModLoaderType.Forge, result.LoaderType);
            }
            finally
            {
                CleanupFile(jarPath);
            }
        }

        [TestMethod]
        public async Task AnalyzeAsync_ForgeMod_MissingModId_ReturnsNull()
        {
            // Arrange: mods.toml 缺少 modId
            var jarPath = CreateTestJar(entries: new Dictionary<string, string>
            {
                ["META-INF/mods.toml"] = """
                [[mods]]
                version = "1.0.0"
                displayName = "Some Mod"
                """
            });

            try
            {
                // Act
                var result = await _service.AnalyzeAsync(jarPath);

                // Assert
                Assert.IsNull(result);
            }
            finally
            {
                CleanupFile(jarPath);
            }
        }

        #endregion

        #region NeoForge 分析器测试

        [TestMethod]
        public async Task AnalyzeAsync_NeoForgeMod_ReturnsCorrectResult()
        {
            // Arrange: 创建包含 META-INF/neoforge.mods.toml 的测试 JAR
            var jarPath = CreateTestJar(entries: new Dictionary<string, string>
            {
                ["META-INF/neoforge.mods.toml"] = """
                [[mods]]
                modId = "xaerominimap"
                version = "25.3.14"
                displayName = "Xaero's Minimap"
                """
            });

            try
            {
                // Act
                var result = await _service.AnalyzeAsync(jarPath);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("xaerominimap", result.ModId);
                Assert.AreEqual("Xaero's Minimap", result.Name);
                Assert.AreEqual("25.3.14", result.Version);
                Assert.AreEqual(ModLoaderType.NeoForge, result.LoaderType);
            }
            finally
            {
                CleanupFile(jarPath);
            }
        }

        #endregion

        #region 优先级和边界测试

        [TestMethod]
        public async Task AnalyzeAsync_BothNeoForgeAndForge_ReturnsNeoForge()
        {
            // Arrange: JAR 同时包含 neoforge.mods.toml 和 mods.toml
            var jarPath = CreateTestJar(entries: new Dictionary<string, string>
            {
                ["META-INF/neoforge.mods.toml"] = """
                [[mods]]
                modId = "neoforge-mod"
                version = "1.0.0"
                displayName = "NeoForge Mod"
                """,
                ["META-INF/mods.toml"] = """
                [[mods]]
                modId = "forge-mod"
                version = "1.0.0"
                displayName = "Forge Mod"
                """
            });

            try
            {
                // Act
                var result = await _service.AnalyzeAsync(jarPath);

                // Assert: NeoForge 优先于 Forge
                Assert.IsNotNull(result);
                Assert.AreEqual(ModLoaderType.NeoForge, result.LoaderType);
                Assert.AreEqual("neoforge-mod", result.ModId);
            }
            finally
            {
                CleanupFile(jarPath);
            }
        }

        [TestMethod]
        public async Task AnalyzeAsync_UnknownModFormat_ReturnsNull()
        {
            // Arrange: JAR 不包含任何已知特征文件
            var jarPath = CreateTestJar(entries: new Dictionary<string, string>
            {
                ["META-INF/MANIFEST.MF"] = "Manifest-Version: 1.0\n"
            });

            try
            {
                // Act
                var result = await _service.AnalyzeAsync(jarPath);

                // Assert
                Assert.IsNull(result);
            }
            finally
            {
                CleanupFile(jarPath);
            }
        }

        [TestMethod]
        public async Task AnalyzeAsync_FileNotFound_ThrowsException()
        {
            // Act & Assert
            FileNotFoundException? caught = null;
            try
            {
                await _service.AnalyzeAsync("nonexistent_file.jar");
            }
            catch (FileNotFoundException ex)
            {
                caught = ex;
            }

            Assert.IsNotNull(caught, "应抛出 FileNotFoundException");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建包含指定文件条目的临时 JAR（ZIP）文件。
        /// </summary>
        /// <param name="entries">文件路径 → 文件内容的字典</param>
        /// <returns>临时文件路径</returns>
        private static string CreateTestJar(Dictionary<string, string> entries)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"test_mod_{Guid.NewGuid():N}.jar");

            using var archive = ZipFile.Open(tempPath, ZipArchiveMode.Create, Encoding.UTF8);

            foreach (var (entryPath, content) in entries)
            {
                var entry = archive.CreateEntry(entryPath);
                using var stream = entry.Open();
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                writer.Write(content);
            }

            return tempPath;
        }

        /// <summary>
        /// 安全删除临时文件。
        /// </summary>
        private static void CleanupFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // 忽略清理失败
            }
        }

        #endregion
    }
}