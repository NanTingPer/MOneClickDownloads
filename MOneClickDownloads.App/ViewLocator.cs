using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MOneClickDownloads.App.ViewModels;
using System;
using System.Diagnostics.CodeAnalysis;

namespace MOneClickDownloads.App
{
    /// <summary>
    /// Given a view model, returns the corresponding view if possible.
    /// 支持通过 IServiceProvider 解析 View 实例，以便利用依赖注入。
    /// </summary>
    [RequiresUnreferencedCode(
        "Default implementation of ViewLocator involves reflection which may be trimmed away.",
        Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
    public class ViewLocator : IDataTemplate
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 构造 ViewLocator。
        /// </summary>
        /// <param name="serviceProvider">DI 容器的服务提供者</param>
        public ViewLocator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(name);

            if (type != null) {
                return (Control)Activator.CreateInstance(type)!;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
