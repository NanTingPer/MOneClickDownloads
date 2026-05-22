using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MOneClickDownloads.App.Models;
using MOneClickDownloads.App.ViewModels;

namespace MOneClickDownloads.App.Views
{
    public partial class FavoritesView : UserControl
    {
        public FavoritesView()
        {
            InitializeComponent();
        }

        private async void OnCreateCollectionClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var window = this.FindAncestorOfType<Window>();
            if (window == null) return;

            var dialog = new CreateCollectionDialog();
            var result = await dialog.ShowDialog<string?>(window);

            if (!string.IsNullOrWhiteSpace(result))
            {
                var vm = DataContext as FavoritesViewModel;
                vm?.CreateCollectionCommand.Execute(result);
            }
        }

        private async void OnDeleteItemClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            if (button.DataContext is not FavoriteDisplayItem item) return;

            var window = this.FindAncestorOfType<Window>();
            if (window == null) return;

            var dialog = new ConfirmDialog();
            dialog.SetContent("确认删除", $"确定要从收藏夹中移除「{item.DisplayName}」吗？");
            var confirmed = await dialog.ShowDialog<bool>(window);

            if (confirmed)
            {
                var vm = DataContext as FavoritesViewModel;
                vm?.RemoveFromFavoritesCommand.Execute(item);
            }
        }
    }
}