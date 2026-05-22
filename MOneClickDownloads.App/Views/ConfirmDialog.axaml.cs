using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MOneClickDownloads.App.Views
{
    /// <summary>
    /// 通用确认对话框，用于确认删除等操作。
    /// </summary>
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog()
        {
            InitializeComponent();
            CancelButton.Click += OnCancel;
            ConfirmButton.Click += OnConfirm;
        }

        /// <summary>
        /// 设置确认对话框的标题和提示信息。
        /// </summary>
        /// <param name="title">标题文本</param>
        /// <param name="message">提示信息</param>
        public void SetContent(string title, string message)
        {
            TitleText.Text = title;
            MessageText.Text = message;
        }

        /// <summary>
        /// 设置确认按钮的文本。
        /// </summary>
        /// <param name="text">按钮文本</param>
        public void SetConfirmButtonText(string text)
        {
            ConfirmButton.Content = text;
        }

        private void OnCancel(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private void OnConfirm(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }
    }
}