using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using MOneClickDownloads.App.ViewModels;
using MOneClickDownloads.App.Views;
using Serilog;
using System.Linq;

namespace MOneClickDownloads.App
{
    public partial class App : Application
    {
        private static readonly ILogger Logger = Log.ForContext<App>();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Logger.Information("框架初始化完成，应用生命周期类型: {LifetimeType}", 
                ApplicationLifetime?.GetType().Name ?? "null");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();

            Logger.Information("应用启动成功");
        }
    }
}