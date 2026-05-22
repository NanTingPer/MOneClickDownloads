using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Serilog;

namespace MOneClickDownloads.App.Converters
{
    /// <summary>
    /// 支持从 URL 异步加载网络图片的 Image 控件。
    /// 当 Url 属性变化时，自动通过 HttpClient 下载图片并设置 Source。
    /// 使用静态缓存避免重复下载同一图标。
    /// </summary>
    public class AsyncImage : Image
    {
        private static readonly ILogger Logger = Log.ForContext<AsyncImage>();
        private static readonly HttpClient HttpClient = new();
        private static readonly ConcurrentDictionary<string, Bitmap?> Cache = new();

        /// <summary>
        /// 图片 URL 依赖属性
        /// </summary>
        public static readonly StyledProperty<string?> UrlProperty =
            AvaloniaProperty.Register<AsyncImage, string?>(nameof(Url));

        public string? Url
        {
            get => GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }

        static AsyncImage()
        {
            UrlProperty.Changed.AddClassHandler<AsyncImage>((x, _) => x.OnUrlChanged());
        }

        private async void OnUrlChanged()
        {
            var url = Url;
            if (string.IsNullOrWhiteSpace(url))
            {
                Source = null;
                return;
            }

            // 缓存命中
            if (Cache.TryGetValue(url, out var cached))
            {
                Source = cached;
                return;
            }

            try
            {
                var bytes = await HttpClient.GetByteArrayAsync(url);

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var ms = new MemoryStream(bytes);
                    var bitmap = new Bitmap(ms);
                    Cache[url] = bitmap;
                    Source = bitmap;
                });
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "加载图片失败: {Url}", url);
                Cache[url] = null;
            }
        }
    }
}