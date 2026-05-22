using Avalonia.Controls;
using Avalonia.VisualTree;
using MOneClickDownloads.App.ViewModels;

namespace MOneClickDownloads.App.Views
{
    public partial class FavoritesView : UserControl
    {
        public FavoritesView()
        {
            InitializeComponent();
        }

        private async void OnCreateCollectionClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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
    }
}