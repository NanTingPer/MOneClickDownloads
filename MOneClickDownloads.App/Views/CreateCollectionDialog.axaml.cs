using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MOneClickDownloads.App.Views
{
    /// <summary>
    /// 创建收藏夹对话框，让用户输入新收藏夹名称。
    /// </summary>
    public partial class CreateCollectionDialog : Window
    {
        /// <summary>
        /// 用户输入的收藏夹名称。取消时为 null。
        /// </summary>
        public string? CollectionName { get; private set; }

        public CreateCollectionDialog()
        {
            InitializeComponent();

            CancelButton.Click += OnCancel;
            CreateButton.Click += OnCreate;
        }

        private void OnCancel(object? sender, RoutedEventArgs e)
        {
            CollectionName = null;
            Close(null);
        }

        private void OnCreate(object? sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                CollectionName = null;
                Close(null);
                return;
            }

            CollectionName = name;
            Close(name);
        }
    }
}