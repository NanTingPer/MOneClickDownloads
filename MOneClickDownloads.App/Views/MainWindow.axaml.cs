using System;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MOneClickDownloads.App.ViewModels;
using Serilog;

namespace MOneClickDownloads.App.Views
{
    public partial class MainWindow : Window
    {
        private static readonly ILogger Logger = Log.ForContext<MainWindow>();

        /// <summary>
        /// 侧边栏展开时的目标宽度。
        /// </summary>
        private const double SidebarOpenWidth = 220;

        /// <summary>
        /// 侧边栏收起时的目标宽度。
        /// </summary>
        private const double SidebarClosedWidth = 0;

        /// <summary>
        /// 用于取消进行中的侧边栏动画。
        /// </summary>
        private CancellationTokenSource? _sidebarAnimCts;

        /// <summary>
        /// 用于取消进行中的遮罩层动画。
        /// </summary>
        private CancellationTokenSource? _overlayAnimCts;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 当 ViewModel 的 IsSidebarOpen 属性变化时，触发侧边栏滑入/滑出动画。
        /// 在 Loaded 事件中订阅 DataContext 变化。
        /// </summary>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged -= OnViewModelPropertyChanged;
            }

            base.OnUnloaded(e);
        }

        /// <summary>
        /// 监听 IsSidebarOpen 属性变化，播放对应的 KeyFrame 动画。
        /// 使用 CubicEaseOut（Lerp 缓动）实现平滑过渡。
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MainWindowViewModel.IsSidebarOpen))
                return;

            if (DataContext is not MainWindowViewModel vm)
                return;

            AnimateSidebar(vm.IsSidebarOpen);
        }

        /// <summary>
        /// 使用 Avalonia KeyFrame 动画对侧边栏宽度进行缓动过渡。
        /// 打开：0 → 220px (CubicEaseOut)
        /// 关闭：220px → 0 (CubicEaseIn)
        /// 
        /// 关键：使用 CancellationTokenSource 取消前一个动画，
        /// 并基于元素当前实际宽度计算动画起始值，避免快速切换时状态冲突。
        /// </summary>
        private void AnimateSidebar(bool isOpen)
        {
            var sidebarPanel = this.FindControl<Border>("SidebarPanel");
            var overlay = this.FindControl<Border>("SidebarOverlay");

            if (sidebarPanel == null)
                return;

            // 取消前一个动画
            _sidebarAnimCts?.Cancel();
            _sidebarAnimCts?.Dispose();
            _overlayAnimCts?.Cancel();
            _overlayAnimCts?.Dispose();

            var sidebarCts = new CancellationTokenSource();
            _sidebarAnimCts = sidebarCts;
            var overlayCts = new CancellationTokenSource();
            _overlayAnimCts = overlayCts;

            // 获取当前实际宽度作为动画起始值，而非硬编码
            var currentWidth = sidebarPanel.Width;
            // 如果当前宽度为 NaN（首次），使用目标方向的起始值
            if (double.IsNaN(currentWidth))
            {
                currentWidth = isOpen ? SidebarClosedWidth : SidebarOpenWidth;
            }

            var toWidth = isOpen ? SidebarOpenWidth : SidebarClosedWidth;
            var duration = isOpen ? TimeSpan.FromMilliseconds(350) : TimeSpan.FromMilliseconds(250);
            var easing = isOpen ? new CubicEaseOut() : (Easing)new CubicEaseIn();

            // 遮罩层：打开时显示，关闭时隐藏
            if (overlay != null)
            {
                overlay.IsVisible = true;
                var currentOpacity = overlay.Opacity;
                var toOpacity = isOpen ? 0.3d : 0d;

                var overlayAnim = new Animation
                {
                    Duration = duration,
                    Easing = easing,
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters =
                            {
                                new Setter(OpacityProperty, currentOpacity)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1d),
                            Setters =
                            {
                                new Setter(OpacityProperty, toOpacity)
                            }
                        }
                    }
                };

                overlayAnim.RunAsync(overlay, overlayCts.Token).ContinueWith(t =>
                {
                    if (t.IsCanceled) return;
                    Dispatcher.UIThread.Post(() =>
                    {
                        overlay.Opacity = toOpacity;
                        if (!isOpen)
                        {
                            overlay.IsVisible = false;
                        }
                    });
                });
            }

            // 侧边栏宽度动画：使用 KeyFrame + Lerp 缓动
            var sidebarAnim = new Animation
            {
                Duration = duration,
                Easing = easing,
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter(WidthProperty, currentWidth)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters =
                        {
                            new Setter(WidthProperty, toWidth)
                        }
                    }
                }
            };

            sidebarAnim.RunAsync(sidebarPanel, sidebarCts.Token).ContinueWith(t =>
            {
                if (t.IsCanceled) return;
                Dispatcher.UIThread.Post(() =>
                {
                    sidebarPanel.Width = toWidth;
                });
            });
        }

        /// <summary>
        /// 侧边栏导航按钮点击事件。
        /// 根据 Tag 值执行对应导航，然后自动收起侧边栏。
        /// </summary>
        private void OnSidebarNavClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || DataContext is not MainWindowViewModel vm)
                return;

            var tag = btn.Tag as string;
            Logger.Information("侧边栏导航点击: {Tag}", tag);

            switch (tag)
            {
                case "Search":
                    vm.NavigateToSearch();
                    vm.CurrentPageName = "搜索";
                    break;
                case "Favorites":
                    vm.NavigateToFavorites();
                    vm.CurrentPageName = "收藏夹";
                    break;
            }

            // 导航后自动收起侧边栏
            vm.IsSidebarOpen = false;
        }

        /// <summary>
        /// 点击遮罩层关闭侧边栏。
        /// </summary>
        private void OnOverlayPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.IsSidebarOpen = false;
            }
        }
    }
}