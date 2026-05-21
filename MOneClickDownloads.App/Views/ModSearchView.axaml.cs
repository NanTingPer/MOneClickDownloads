using Avalonia.Controls;
using Avalonia.Input;
using MOneClickDownloads.App.ViewModels;
using MOneClickDownloads.DataModel.Search;

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

        private void OnSearchResultSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ProjectHit hit)
            {
                // 先取消选中，以便下次点击同一项仍能触发
                listBox.SelectedItem = null;

                if (DataContext is ModSearchViewModel vm)
                {
                    vm.SelectModCommand.Execute(hit);
                }
            }
        }
    }
}
