using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        SaveWindowSize();
        SaveColumnLayout();
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

    private void SaveWindowSize()
    {
        var width = Width;
        var height = Height;

        if (WindowState != WindowState.Normal)
        {
            width = RestoreBounds.Width;
            height = RestoreBounds.Height;
        }

        if (width < MinWidth || height < MinHeight)
            return;

        _mainWindowStateService.Save(new MainWindowState
        {
            Width = width,
            Height = height
        });
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
