using Avalonia.Controls;
using Avalonia.Input;
using MOneClickDownloads.App.ViewModels;

namespace MOneClickDownloads.App.Views
{
    public partial class ModSearchView : UserControl
    {
        public ModSearchView()
        {
            InitializeComponent();
        }

        private void OnSearchKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ModSearchViewModel vm)
            {
                vm.SearchCommand.Execute(null);
            }
        }
    }
}