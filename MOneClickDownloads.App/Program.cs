using Avalonia;
using MOneClickDownloads.App.Logging;
using Serilog;
using System;

namespace MOneClickDownloads.App
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // 初始化 Serilog
            Log.Logger = LoggerConfig.CreateLogger();

            try
            {
                Log.Information("应用程序启动");

                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "应用程序发生致命错误");
                throw;
            }
            finally
            {
                Log.Information("应用程序关闭");
                Log.CloseAndFlush();
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .LogToTrace();
    }
}