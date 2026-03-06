using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using UrlVault.Models;
using UrlVault.Services;
using UrlVault.ViewModels;

namespace UrlVault.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ColumnLayoutService _columnLayoutService = new();
    private readonly MainWindowStateService _mainWindowStateService = new();
    private string? _lastSortColumnKey;
    private ListSortDirection _lastSortDirection = ListSortDirection.Ascending;

    // Drag/drop state
    private Point _dragStartPoint;
    private CategoryNodeViewModel? _dragSourceNode;
    private TreeViewItem? _dropTargetItem;
    private DropAction _currentDropAction = DropAction.None;

    private enum DropAction { None, ReorderBefore, ReorderAfter, Nest, Promote }

    public MainWindow()
    {
        InitializeComponent();
        ApplySavedWindowSize();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(GridViewColumnHeader_Click));
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
        ApplySavedColumnLayout();
        RestoreTreeExpandedState();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        SaveColumnLayout();
        SaveAllState();
    }

    private void TagFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.Tag is string tag)
        {
            if (cb.IsChecked == true)
            {
                if (!_viewModel.SelectedTagFilters.Contains(tag))
                    _viewModel.SelectedTagFilters.Add(tag);
            }
            else
            {
                _viewModel.SelectedTagFilters.Remove(tag);
            }
        }
    }

    private void CategoryTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel vm && e.NewValue is CategoryNodeViewModel node)
            vm.SelectedCategoryNode = node;
    }

    private void UrlListViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Select the row the user right-clicked so all context menu commands
        // operate on the correct entry before the menu opens.
        if (sender is ListViewItem item)
            item.IsSelected = true;
    }

    // ── Drag/drop re-parenting ──────────────────────────────────────────────

    private void CategoryTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        var (node, _) = GetNodeAndItem(e.OriginalSource as DependencyObject);
        // Any node except "All" can be dragged
        _dragSourceNode = (node != null && !node.IsAll) ? node : null;
    }

    private void CategoryTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragSourceNode == null) return;

        var pos = e.GetPosition(null);
        var diff = _dragStartPoint - pos;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var node = _dragSourceNode;
        _dragSourceNode = null;
        DragDrop.DoDragDrop(CategoryTreeView, new DataObject("CategoryNodePath", node.FullPath), DragDropEffects.Move);
    }

    // Height of a single TreeViewItem header row (px). Used to compute
    // reorder-vs-nest drop zones independent of whether the item is expanded.
    private const double RowHeight = 28.0;
    // Fraction of the row from the top that triggers "reorder before".
    // Everything below this line triggers "nest".
    private const double ReorderZoneFraction = 0.35;

    private void CategoryTreeView_DragOver(object sender, DragEventArgs e)
    {
        ClearDropHighlight();

        if (!e.Data.GetDataPresent("CategoryNodePath"))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var draggedPath = e.Data.GetData("CategoryNodePath") as string ?? "";
        var (target, targetItem) = GetNodeAndItem(e.OriginalSource as DependencyObject);

        if (target == null || targetItem == null ||
            target.FullPath.Equals(draggedPath, StringComparison.OrdinalIgnoreCase))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        // Subcategory dropped onto "All" → promote to top-level group
        if (target.IsAll && draggedPath.Contains('/'))
        {
            _currentDropAction = DropAction.Promote;
            _dropTargetItem = targetItem;
            targetItem.Background = new SolidColorBrush(Color.FromRgb(0xC8, 0xE6, 0xC9)); // green
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
            return;
        }

        if (target.IsAll)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        if (draggedPath.Contains('/'))
        {
            // ── Subcategory being dragged ──────────────────────────────────
            var sourceGroup = draggedPath.Split('/')[0];

            if (target.IsGroup)
            {
                // Drop onto a group → always re-parent, highlight target
                _currentDropAction = DropAction.Nest;
                _dropTargetItem = targetItem;
                targetItem.Background = new SolidColorBrush(Color.FromRgb(0xC5, 0xCA, 0xE9));
            }
            else
            {
                // Drop onto another subcategory
                var targetGroup = target.FullPath.Split('/')[0];

                if (sourceGroup.Equals(targetGroup, StringComparison.OrdinalIgnoreCase))
                {
                    // Same group → reorder: top 50% = before, bottom 50% = after
                    var pos = e.GetPosition(targetItem);
                    var effectiveHeight = Math.Min(targetItem.ActualHeight, RowHeight);
                    _currentDropAction = pos.Y < effectiveHeight / 2
                        ? DropAction.ReorderBefore
                        : DropAction.ReorderAfter;
                }
                else
                {
                    // Different group → treat as re-parent into target's group
                    _currentDropAction = DropAction.Nest;
                    _dropTargetItem = targetItem;
                    targetItem.Background = new SolidColorBrush(Color.FromRgb(0xC5, 0xCA, 0xE9));
                }
            }
        }
        else
        {
            // ── Group being dragged ────────────────────────────────────────
            if (!target.IsGroup)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            // Cap to header row height so expanded groups behave the same as collapsed ones.
            var pos = e.GetPosition(targetItem);
            var effectiveHeight = Math.Min(targetItem.ActualHeight, RowHeight);
            if (pos.Y < effectiveHeight * ReorderZoneFraction)
            {
                _currentDropAction = DropAction.ReorderBefore;
            }
            else
            {
                _currentDropAction = DropAction.Nest;
                _dropTargetItem = targetItem;
                targetItem.Background = new SolidColorBrush(Color.FromRgb(0xC5, 0xCA, 0xE9));
            }
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private async void CategoryTreeView_Drop(object sender, DragEventArgs e)
    {
        var action = _currentDropAction;  // capture before ClearDropHighlight resets it
        ClearDropHighlight();

        if (!e.Data.GetDataPresent("CategoryNodePath")) return;
        var draggedPath = e.Data.GetData("CategoryNodePath") as string;
        var (target, _) = GetNodeAndItem(e.OriginalSource as DependencyObject);
        if (string.IsNullOrEmpty(draggedPath) || target == null) return;

        if (action == DropAction.Promote && draggedPath.Contains('/'))
        {
            await _viewModel.ExecutePromoteSubcategoryAsync(draggedPath);
            return;
        }

        if (target.IsAll) return;  // no other operation valid on All

        if (draggedPath.Contains('/'))
        {
            // Subcategory drag
            if (action == DropAction.Nest)
            {
                var targetGroup = target.IsGroup ? target.Name : target.FullPath.Split('/')[0];
                await _viewModel.ExecuteReparentSubcategoryAsync(draggedPath, targetGroup);
            }
            else if (action is DropAction.ReorderBefore or DropAction.ReorderAfter)
            {
                await _viewModel.ExecuteReorderSubcategoryAsync(draggedPath, target.FullPath, action == DropAction.ReorderAfter);
            }
        }
        else
        {
            // Group drag
            if (action == DropAction.Nest)
            {
                if (target.IsGroup)
                    await _viewModel.ExecuteNestGroupAsSubcategoryAsync(draggedPath, target.Name);
            }
            else if (action == DropAction.ReorderBefore)
            {
                if (target.IsGroup)
                    await _viewModel.ExecuteReorderGroupAsync(draggedPath, target.FullPath);
            }
        }
    }

    private void CategoryTreeView_DragLeave(object sender, DragEventArgs e)
    {
        ClearDropHighlight();
    }

    private void ClearDropHighlight()
    {
        if (_dropTargetItem != null)
        {
            _dropTargetItem.Background = Brushes.Transparent;
            _dropTargetItem = null;
        }
        // Do NOT reset _currentDropAction here — DragLeave fires just before Drop
        // and would wipe the action before Drop can read it.
        // _currentDropAction is reset only by Drop itself.
    }

    private static (CategoryNodeViewModel? node, TreeViewItem? item) GetNodeAndItem(DependencyObject? element)
    {
        while (element != null)
        {
            if (element is TreeViewItem tvi && tvi.DataContext is CategoryNodeViewModel node)
                return (node, tvi);
            element = element is Visual
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }
        return (null, null);
    }

    // ── Expanded-state & window-size persistence ────────────────────────────

    private void RestoreTreeExpandedState()
    {
        var state = _mainWindowStateService.Load();
        if (state?.ExpandedCategoryNodes is { Count: > 0 } paths)
            _viewModel.RestoreExpandedState(paths);
    }

    private void SaveAllState()
    {
        var state = _mainWindowStateService.Load() ?? new MainWindowState();

        // Window dimensions
        var width = Width;
        var height = Height;
        if (WindowState != WindowState.Normal)
        {
            width = RestoreBounds.Width;
            height = RestoreBounds.Height;
        }
        if (width >= MinWidth && height >= MinHeight)
        {
            state.Width = width;
            state.Height = height;
        }

        // Expanded tree nodes
        state.ExpandedCategoryNodes = _viewModel.CategoryTree
            .Where(n => n.IsExpanded)
            .Select(n => n.FullPath)
            .ToList();

        _mainWindowStateService.Save(state);
    }

    private void ApplySavedColumnLayout()
    {
        if (UrlGridView == null || UrlGridView.Columns.Count == 0)
            return;

        var savedColumns = _columnLayoutService.LoadMainWindowColumns();
        if (savedColumns.Count == 0)
            return;

        var currentColumns = UrlGridView.Columns.Cast<GridViewColumn>().ToList();
        var reordered = new List<GridViewColumn>();
        var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var saved in savedColumns.OrderBy(c => c.Order))
        {
            var match = currentColumns.FirstOrDefault(c =>
                !usedKeys.Contains(GetColumnKey(c)) &&
                GetColumnKey(c).Equals(saved.Key, StringComparison.OrdinalIgnoreCase));

            if (match == null)
                continue;

            reordered.Add(match);
            usedKeys.Add(saved.Key);
        }

        foreach (var column in currentColumns)
        {
            if (!usedKeys.Contains(GetColumnKey(column)))
                reordered.Add(column);
        }

        UrlGridView.Columns.Clear();
        foreach (var column in reordered)
            UrlGridView.Columns.Add(column);

        var savedWidths = savedColumns.ToDictionary(c => c.Key, c => c.Width, StringComparer.OrdinalIgnoreCase);
        foreach (var column in UrlGridView.Columns.Cast<GridViewColumn>())
        {
            if (!savedWidths.TryGetValue(GetColumnKey(column), out var width) || !width.HasValue || width <= 0)
                continue;

            column.Width = width.Value;
        }
    }

    private void SaveColumnLayout()
    {
        if (UrlGridView == null || UrlGridView.Columns.Count == 0)
            return;

        var columns = UrlGridView.Columns
            .Cast<GridViewColumn>()
            .Select((column, index) => new ColumnLayoutEntry
            {
                Key = GetColumnKey(column),
                Order = index,
                Width = double.IsNaN(column.Width) ? null : column.Width
            })
            .Where(c => !string.IsNullOrWhiteSpace(c.Key))
            .ToList();

        _columnLayoutService.SaveMainWindowColumns(columns);
    }

    private static string GetColumnKey(GridViewColumn column)
    {
        return (column.Header?.ToString() ?? string.Empty).Trim();
    }

    private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.EntriesView == null)
            return;

        if (e.OriginalSource is not GridViewColumnHeader header || header.Role == GridViewColumnHeaderRole.Padding)
            return;

        var column = header.Column;
        if (column == null)
            return;

        var columnKey = GetColumnKey(column);
        if (string.IsNullOrWhiteSpace(columnKey))
            return;

        var direction = ListSortDirection.Ascending;
        if (string.Equals(_lastSortColumnKey, columnKey, StringComparison.OrdinalIgnoreCase) &&
            _lastSortDirection == ListSortDirection.Ascending)
        {
            direction = ListSortDirection.Descending;
        }

        ApplyColumnSort(columnKey, direction);
        _lastSortColumnKey = columnKey;
        _lastSortDirection = direction;
    }

    private void ApplyColumnSort(string columnKey, ListSortDirection direction)
    {
        var view = _viewModel.EntriesView;
        if (view == null)
            return;

        if (view is ListCollectionView listView)
            listView.CustomSort = null;

        view.SortDescriptions.Clear();

        if (string.Equals(columnKey, "Tags", StringComparison.OrdinalIgnoreCase))
        {
            if (view is ListCollectionView tagsView)
            {
                tagsView.CustomSort = new TagsComparer(direction);
                tagsView.Refresh();
            }

            return;
        }

        var property = ResolveSortProperty(columnKey);
        if (string.IsNullOrWhiteSpace(property))
            return;

        view.SortDescriptions.Add(new SortDescription(property, direction));
        view.Refresh();
    }

    private static string? ResolveSortProperty(string columnKey)
    {
        return columnKey switch
        {
            "Title" => nameof(UrlEntry.Title),
            "URL" => nameof(UrlEntry.Url),
            "Category" => nameof(UrlEntry.Category),
            "Comment" => nameof(UrlEntry.Comment),
            "Date Saved" => nameof(UrlEntry.DateSaved),
            _ => null
        };
    }

    private void ApplySavedWindowSize()
    {
        var state = _mainWindowStateService.Load();
        if (state == null)
            return;

        if (state.Width >= MinWidth)
            Width = state.Width;

        if (state.Height >= MinHeight)
            Height = state.Height;
    }

    private sealed class TagsComparer : IComparer
    {
        private readonly int _direction;

        public TagsComparer(ListSortDirection direction)
        {
            _direction = direction == ListSortDirection.Ascending ? 1 : -1;
        }

        public int Compare(object? x, object? y)
        {
            var left = x as UrlEntry;
            var right = y as UrlEntry;

            var leftTags = left == null ? string.Empty : string.Join(", ", left.Tags);
            var rightTags = right == null ? string.Empty : string.Join(", ", right.Tags);

            return _direction * StringComparer.OrdinalIgnoreCase.Compare(leftTags, rightTags);
        }
    }
}
