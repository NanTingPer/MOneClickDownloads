using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MOneClickDownloads.App.Models;
using MOneClickDownloads.App.ViewModels;

namespace MOneClickDownloads.App.Views
{
    public partial class LocalModsView : UserControl
    {
        public LocalModsView()
        {
            InitializeComponent();
        }

        private async void OnDeleteModFileClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            if (button.DataContext is not LocalModDisplayItem item) return;

            var vm = DataContext as LocalModsViewModel;
            await vm!.DeleteModFileCommand.ExecuteAsync(item);
        }
    }
}