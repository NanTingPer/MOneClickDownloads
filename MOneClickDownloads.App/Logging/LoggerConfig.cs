using Serilog;
using Serilog.Formatting.Compact;
using System;
using System.IO;

namespace MOneClickDownloads.App.Logging
{
    /// <summary>
    /// 日志配置，提供统一的 Serilog Logger 实例。
    /// </summary>
    public static class LoggerConfig
    {
        /// <summary>
        /// 创建并配置 Serilog Logger。
        /// 输出两个文件：
        /// 1. log_service_json_formatter.log - JSON 紧凑格式
        /// 2. log_service.log - 可读文本格式
        /// </summary>
        public static ILogger CreateLogger()
        {
            var logRootDir = Path.Combine(AppContext.BaseDirectory, "logs");

            return new LoggerConfiguration()
                .WriteTo.File(
                    path: Path.Combine(logRootDir, "log_service_json_formatter.log"),
                    formatter: new CompactJsonFormatter(),
                    rollingInterval: RollingInterval.Month,
                    retainedFileCountLimit: 20)
                .WriteTo.File(
                    path: Path.Combine(logRootDir, "log_service.log"),
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Month,
                    retainedFileCountLimit: 20)
                .CreateLogger();
        }
    }
}