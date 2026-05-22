using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MOneClickDownloads.App.ViewModels;
using MOneClickDownloads.DataModel.Search;

namespace MOneClickDownloads.App.Views
{
    public partial class ModSearchView : UserControl
    {
        private bool _isRightClick;

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

        /// <summary>
        /// 鼠标按下搜索结果时，检测是否为右键并设置标志位
        /// </summary>
        private void OnSearchResultPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                _isRightClick = true;
            }
        }

        private void OnSearchResultSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ProjectHit hit)
            {
                // 先取消选中，以便下次点击同一项仍能触发
                listBox.SelectedItem = null;

                // 如果是右键触发的选中，则跳过导航
                if (_isRightClick)
                {
                    _isRightClick = false;
                    return;
                }

                if (DataContext is ModSearchViewModel vm)
                {
                    vm.SelectModCommand.Execute(hit);
                }
            }
        }

        /// <summary>
        /// 右键点击搜索结果时，记录被右键的项目
        /// </summary>
        private void OnSearchResultContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (sender is Border border && border.DataContext is ProjectHit hit)
            {
                if (DataContext is ModSearchViewModel vm)
                {
                    vm.RightClickedItem = hit;
                }
            }
        }

        /// <summary>
        /// 右键菜单打开后，动态填充收藏夹子菜单
        /// </summary>
        private void OnContextMenuOpened(object? sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu contextMenu) return;
            if (DataContext is not ModSearchViewModel vm) return;

            // 找到"添加到收藏夹"菜单项（第一个 MenuItem）
            var addToFavItem = contextMenu.Items.Count > 0
                ? contextMenu.Items[0] as MenuItem
                : null;
            if (addToFavItem == null) return;

            // 刷新收藏夹列表
            vm.LoadFavoriteCollectionsCommand.Execute(null);

            // 清空子项
            addToFavItem.Items.Clear();

            // 为每个收藏夹添加子菜单项
            foreach (var collection in vm.FavoriteCollections)
            {
                var subItem = new MenuItem
                {
                    Header = collection.Name,
                    Tag = collection
                };
                subItem.Click += (_, _) =>
                {
                    vm.AddToFavoritesCommand.Execute(collection);
                };
                addToFavItem.Items.Add(subItem);
            }

            // 添加分隔线
            addToFavItem.Items.Add(new Separator());

            // 添加"创建新收藏夹"子菜单项
            var createItem = new MenuItem
            {
                Header = "➕ 创建新收藏夹"
            };
            createItem.Click += async (_, _) =>
            {
                await vm.CreateNewCollectionAndAddCommand.ExecuteAsync(null);
            };
            addToFavItem.Items.Add(createItem);
        }
    }
}