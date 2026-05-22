using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MOneClickDownloads.App.Models;

namespace MOneClickDownloads.App.Views
{
    /// <summary>
    /// 合集下载兼容性警告对话框。<br />
    /// <br />
    /// 当合集中部分模组在选定 MC版本/加载器 下不兼容或仅有预览版时，
    /// 弹出此对话框列出这些模组，让用户选择是否继续下载。
    /// </summary>
    public partial class CollectionWarningDialog : Window
    {
        /// <summary>
        /// 用户是否选择继续下载
        /// </summary>
        public bool ShouldContinue { get; private set; }

        public CollectionWarningDialog()
        {
            InitializeComponent();

            ContinueButton.Click += OnContinue;
            CancelButton.Click += OnCancel;
        }

        /// <summary>
        /// 设置警告信息
        /// </summary>
        /// <param name="title">警告标题</param>
        /// <param name="description">警告描述</param>
        /// <param name="warningMods">需要警告的模组列表</param>
        public void SetWarningInfo(string title, string description, List<CollectionModStatus> warningMods)
        {
            WarningTitle.Text = title;
            WarningDescription.Text = description;
            ModList.ItemsSource = warningMods;
        }

        private void OnContinue(object? sender, RoutedEventArgs e)
        {
            ShouldContinue = true;
            Close(true);
        }

        private void OnCancel(object? sender, RoutedEventArgs e)
        {
            ShouldContinue = false;
            Close(false);
        }
    }
}