using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MOneClickDownloads.App.DI;
using MOneClickDownloads.App.ViewModels;
using MOneClickDownloads.App.Views;
using Serilog;
using System;
using System.Linq;

namespace MOneClickDownloads.App
{
    public partial class App : Application
    {
        private static readonly ILogger Logger = Log.ForContext<App>();

        /// <summary>
        /// 应用级 DI 容器的服务提供者，供全局使用。
        /// </summary>
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Logger.Information("框架初始化完成，应用生命周期类型: {LifetimeType}", 
                ApplicationLifetime?.GetType().Name ?? "null");

            // 构建 DI 容器
            var services = new ServiceCollection();
            services.AddAppServices();
            ServiceProvider = services.BuildServiceProvider();

            // 在代码中添加 ViewLocator（需要 IServiceProvider 参数）
            var viewLocator = new ViewLocator(ServiceProvider);
            Application.Current!.DataTemplates.Add(viewLocator);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                // 从 DI 容器解析 MainWindowViewModel
                var mainVm = ServiceProvider.GetRequiredService<MainWindowViewModel>();
                // 容器构建完成后触发初始导航（避免构造函数中的循环依赖）
                mainVm.NavigateToSearch();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainVm,
                };
            }

            base.OnFrameworkInitializationCompleted();

            Logger.Information("应用启动成功");
        }
    }
}
