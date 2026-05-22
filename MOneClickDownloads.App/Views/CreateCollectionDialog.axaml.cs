using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MOneClickDownloads.App.Views
{
    /// <summary>
    /// 创建/重命名收藏夹对话框，让用户输入收藏夹名称。
    /// </summary>
    public partial class CreateCollectionDialog : Window
    {
        /// <summary>
        /// 用户输入的收藏夹名称。取消时为 null。
        /// </summary>
        public string? CollectionName { get; private set; }

        /// <summary>
        /// 创建模式（默认）
        /// </summary>
        public CreateCollectionDialog() : this(false, null) { }

        /// <summary>
        /// 支持创建和重命名两种模式。
        /// </summary>
        /// <param name="isRename">是否为重命名模式</param>
        /// <param name="currentName">重命名时的当前名称</param>
        public CreateCollectionDialog(bool isRename, string? currentName)
        {
            InitializeComponent();

            if (isRename)
            {
                Title = "重命名收藏夹";
                TitleText.Text = "请输入新的收藏夹名称";
                CreateButton.Content = "确认";
                if (!string.IsNullOrEmpty(currentName))
                {
                    NameTextBox.Text = currentName;
                    NameTextBox.SelectAll();
                }
            }

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