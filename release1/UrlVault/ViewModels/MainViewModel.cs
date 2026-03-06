using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using UrlVault.Models;
using UrlVault.Services;
using UrlVault.Views;

namespace UrlVault.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly StorageService _storageService = new();
    private readonly ConfigService _configService = new();

    private ObservableCollection<UrlEntry> _allEntries = new();
    private ICollectionView? _entriesView;
    private string _searchText = "";
    private AppConfig _config = new();
    private UrlEntry? _selectedEntry;
    private bool _isBusy;
    private bool _isDarkMode;
    private CategoryNodeViewModel? _selectedCategoryNode;

    public ObservableCollection<string> SelectedTagFilters { get; } = new();
    public ObservableCollection<CategoryNodeViewModel> CategoryTree { get; } = new();

    public ICollectionView? EntriesView
    {
        get => _entriesView;
        private set => SetProperty(ref _entriesView, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                _entriesView?.Refresh();
        }
    }

    public AppConfig Config
    {
        get => _config;
        private set => SetProperty(ref _config, value);
    }

    public UrlEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value);
    }

    public CategoryNodeViewModel? SelectedCategoryNode
    {
        get => _selectedCategoryNode;
        set
        {
            if (SetProperty(ref _selectedCategoryNode, value))
                _entriesView?.Refresh();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set => SetProperty(ref _isDarkMode, value);
    }

    public int DisplayedCount => _entriesView?.Cast<object>().Count() ?? 0;

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CopyUrlCommand { get; }
    public ICommand OpenUrlCommand { get; }
    public ICommand RefreshFiltersCommand { get; }
    public ICommand NewCategoryCommand { get; }
    public ICommand RenameCategoryCommand { get; }
    public ICommand AddSubcategoryCommand { get; }
    public ICommand DeleteCategoryCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    public MainViewModel()
    {
        AddCommand = new RelayCommand(ExecuteAdd);
        EditCommand = new RelayCommand(ExecuteEdit, () => SelectedEntry != null);
        DeleteCommand = new RelayCommand(ExecuteDelete, () => SelectedEntry != null);
        CopyUrlCommand = new RelayCommand(ExecuteCopyUrl, () => SelectedEntry != null);
        OpenUrlCommand = new RelayCommand(ExecuteOpenUrl, () => SelectedEntry != null);
        RefreshFiltersCommand = new RelayCommand(() => _entriesView?.Refresh());
        NewCategoryCommand = new RelayCommand(async () => await ExecuteNewCategoryAsync().ConfigureAwait(false));
        RenameCategoryCommand = new RelayCommand(async node => await ExecuteRenameCategoryNodeAsync(node as CategoryNodeViewModel).ConfigureAwait(false));
        AddSubcategoryCommand = new RelayCommand(async node => await ExecuteAddSubcategoryAsync(node as CategoryNodeViewModel).ConfigureAwait(false));
        DeleteCategoryCommand = new RelayCommand(async node => await ExecuteDeleteCategoryNodeAsync(node as CategoryNodeViewModel).ConfigureAwait(false));
        ToggleThemeCommand = new RelayCommand(ExecuteToggleTheme);

        SelectedTagFilters.CollectionChanged += (_, _) => _entriesView?.Refresh();
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Config = await _configService.LoadConfigAsync().ConfigureAwait(false);
            IsDarkMode = Config.IsDarkMode;   // keep VM in sync; theme already applied by App.OnStartup
            var entries = await _storageService.LoadUrlsAsync().ConfigureAwait(false);

            Application.Current.Dispatcher.Invoke(() =>
            {
                _allEntries = new ObservableCollection<UrlEntry>(entries);
                var view = CollectionViewSource.GetDefaultView(_allEntries);
                view.Filter = FilterEntry;
                view.SortDescriptions.Add(new SortDescription(nameof(UrlEntry.DateSaved), ListSortDirection.Descending));
                EntriesView = view;
                EntriesView.CollectionChanged += (_, _) => OnPropertyChanged(nameof(DisplayedCount));
                OnPropertyChanged(nameof(DisplayedCount));

                RebuildCategoryTree();
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void RebuildCategoryTree()
    {
        var previouslySelectedPath = SelectedCategoryNode?.FullPath;
        var expandedPaths = CategoryTree
            .Where(n => n.IsExpanded)
            .Select(n => n.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        CategoryTree.Clear();

        var allNode = new CategoryNodeViewModel
        {
            Name = "All",
            FullPath = "All",
            Color = "#3F51B5",
            IsAll = true,
            IsGroup = false,
            Count = _allEntries.Count
        };
        CategoryTree.Add(allNode);

        foreach (var group in Config.CategoryGroups)
        {
            var groupNode = new CategoryNodeViewModel
            {
                Name = group.Name,
                FullPath = group.Name,
                Color = group.Color,
                IsGroup = true,
                Count = CountEntriesForNode(group.Name)
            };

            foreach (var sub in group.Subcategories)
            {
                var subPath = $"{group.Name}/{sub}";
                groupNode.Children.Add(new CategoryNodeViewModel
                {
                    Name = sub,
                    FullPath = subPath,
                    Color = group.Color,
                    IsGroup = false,
                    Count = CountEntriesForNode(subPath)
                });
            }

            CategoryTree.Add(groupNode);
        }

        SelectedCategoryNode = FindNodeByPath(previouslySelectedPath) ?? allNode;

        foreach (var node in CategoryTree.Where(n => expandedPaths.Contains(n.FullPath)))
            node.IsExpanded = true;

        OnPropertyChanged(nameof(DisplayedCount));
    }

    private bool FilterEntry(object obj)
    {
        if (obj is not UrlEntry entry) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            bool matchesSearch =
                entry.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                entry.Url.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                entry.Comment.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                entry.Tags.Any(t => t.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            if (!matchesSearch) return false;
        }

        var selectedPath = SelectedCategoryNode?.FullPath;
        if (!string.IsNullOrWhiteSpace(selectedPath) && !selectedPath.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (!selectedPath.Contains('/'))
            {
                if (!(entry.Category.Equals(selectedPath, StringComparison.OrdinalIgnoreCase) ||
                      entry.Category.StartsWith(selectedPath + "/", StringComparison.OrdinalIgnoreCase)))
                    return false;
            }
            else
            {
                if (!entry.Category.Equals(selectedPath, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }

        if (SelectedTagFilters.Count > 0)
        {
            if (!SelectedTagFilters.All(t => entry.Tags.Contains(t))) return false;
        }

        return true;
    }

    private void ExecuteAdd()
    {
        var vm = new AddEditViewModel(_config, _allEntries.ToList(), null);
        vm.OnSaved += async entry =>
        {
            _allEntries.Add(entry);
            await _configService.SaveConfigAsync(_config).ConfigureAwait(false);
            await _storageService.SaveUrlsAsync(_allEntries.ToList()).ConfigureAwait(false);
            Application.Current.Dispatcher.Invoke(() =>
            {
                RebuildCategoryTree();
                OnPropertyChanged(nameof(DisplayedCount));
            });
        };
        var window = CreateOwnedAddEditWindow(vm);
        window.ShowDialog();
    }

    private void ExecuteEdit()
    {
        if (SelectedEntry == null) return;
        var original = SelectedEntry;
        var vm = new AddEditViewModel(_config, _allEntries.ToList(), original);
        vm.OnSaved += async entry =>
        {
            var idx = _allEntries.IndexOf(original);
            if (idx >= 0)
                _allEntries[idx] = entry;

            await _configService.SaveConfigAsync(_config).ConfigureAwait(false);
            await _storageService.SaveUrlsAsync(_allEntries.ToList()).ConfigureAwait(false);
            Application.Current.Dispatcher.Invoke(() =>
            {
                _entriesView?.Refresh();
                RebuildCategoryTree();
            });
        };
        var window = CreateOwnedAddEditWindow(vm);
        window.ShowDialog();
    }

    private async void ExecuteDelete()
    {
        if (SelectedEntry == null) return;
        var result = MessageBox.Show(
            $"Delete '{SelectedEntry.Title}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _allEntries.Remove(SelectedEntry);
            await _storageService.SaveUrlsAsync(_allEntries.ToList()).ConfigureAwait(false);
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(DisplayedCount));
                RebuildCategoryTree();
            });
        }
    }

    private void ExecuteCopyUrl()
    {
        if (SelectedEntry != null)
            Clipboard.SetText(SelectedEntry.Url);
    }

    private void ExecuteOpenUrl()
    {
        if (SelectedEntry == null) return;
        try
        {
            Process.Start(new ProcessStartInfo(SelectedEntry.Url) { UseShellExecute = true });
        }
        catch
        {
        }
    }

    private async Task ExecuteNewCategoryAsync()
    {
        var input = PromptDialogService.ShowTextInput("New Category", "Category group name:");
        var name = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (Config.CategoryGroups.Any(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("A category group with that name already exists.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Config.CategoryGroups.Add(new CategoryGroup { Name = name, Color = "#3F51B5" });
        await _configService.SaveConfigAsync(Config).ConfigureAwait(false);
        Application.Current.Dispatcher.Invoke(RebuildCategoryTree);
    }

    private async Task ExecuteRenameCategoryNodeAsync(CategoryNodeViewModel? node)
    {
        if (node == null || node.IsAll)
            return;

        var input = PromptDialogService.ShowTextInput("Rename Category", "New name:", node.Name);
        var newName = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(newName) || newName.Equals(node.Name, StringComparison.Ordinal))
            return;

        if (node.IsGroup)
        {
            var group = FindGroup(node.FullPath);
            if (group == null)
                return;

            if (Config.CategoryGroups.Any(g => g != group && g.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("A category group with that name already exists.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var oldGroupName = group.Name;
            group.Name = newName;
            foreach (var entry in _allEntries)
            {
                if (entry.Category.Equals(oldGroupName, StringComparison.OrdinalIgnoreCase))
                {
                    entry.Category = newName;
                }
                else if (entry.Category.StartsWith(oldGroupName + "/", StringComparison.OrdinalIgnoreCase))
                {
                    entry.Category = newName + entry.Category[oldGroupName.Length..];
                }
            }
        }
        else
        {
            var parts = node.FullPath.Split('/', 2);
            if (parts.Length != 2)
                return;

            var group = FindGroup(parts[0]);
            if (group == null)
                return;

            if (group.Subcategories.Any(s => s.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("That subcategory already exists in this group.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var oldSub = parts[1];
            var index = group.Subcategories.FindIndex(s => s.Equals(oldSub, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                return;

            group.Subcategories[index] = newName;

            var oldPath = $"{parts[0]}/{oldSub}";
            var newPath = $"{parts[0]}/{newName}";
            foreach (var entry in _allEntries.Where(e => e.Category.Equals(oldPath, StringComparison.OrdinalIgnoreCase)))
                entry.Category = newPath;
        }

        await _configService.SaveConfigAsync(Config).ConfigureAwait(false);
        await _storageService.SaveUrlsAsync(_allEntries.ToList()).ConfigureAwait(false);
        Application.Current.Dispatcher.Invoke(() =>
        {
            _entriesView?.Refresh();
            RebuildCategoryTree();
        });
    }

    private async Task ExecuteAddSubcategoryAsync(CategoryNodeViewModel? node)
    {
        if (node == null || node.IsAll)
            return;

        var groupPath = node.IsGroup ? node.FullPath : node.FullPath.Split('/')[0];
        var group = FindGroup(groupPath);
        if (group == null)
            return;

        var input = PromptDialogService.ShowTextInput("New Subcategory", $"Subcategory name for '{group.Name}':");
        var subName = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(subName))
            return;

        if (group.Subcategories.Any(s => s.Equals(subName, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("That subcategory already exists in this group.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        group.Subcategories.Add(subName);
        await _configService.SaveConfigAsync(Config).ConfigureAwait(false);
        Application.Current.Dispatcher.Invoke(RebuildCategoryTree);
    }

    private async Task ExecuteDeleteCategoryNodeAsync(CategoryNodeViewModel? node)
    {
        if (node == null || node.IsAll)
            return;

        if (node.IsGroup)
        {
            var group = FindGroup(node.FullPath);
            if (group == null)
                return;

            var result = MessageBox.Show(
                $"Delete group '{group.Name}' and its subcategories? Existing entries keep their category text.",
                "Delete Group",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            Config.CategoryGroups.Remove(group);
        }
        else
        {
            var parts = node.FullPath.Split('/', 2);
            if (parts.Length != 2)
                return;

            var group = FindGroup(parts[0]);
            if (group == null)
                return;

            var result = MessageBox.Show(
                $"Delete subcategory '{parts[1]}' from '{parts[0]}'? Existing entries keep their category text.",
                "Delete Subcategory",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            group.Subcategories.RemoveAll(s => s.Equals(parts[1], StringComparison.OrdinalIgnoreCase));
        }

        await _configService.SaveConfigAsync(Config).ConfigureAwait(false);
        Application.Current.Dispatcher.Invoke(RebuildCategoryTree);
    }

    private int CountEntriesForNode(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || fullPath.Equals("All", StringComparison.OrdinalIgnoreCase))
            return _allEntries.Count;

        if (!fullPath.Contains('/'))
        {
            return _allEntries.Count(e =>
                e.Category.Equals(fullPath, StringComparison.OrdinalIgnoreCase) ||
                e.Category.StartsWith(fullPath + "/", StringComparison.OrdinalIgnoreCase));
        }

        return _allEntries.Count(e => e.Category.Equals(fullPath, StringComparison.OrdinalIgnoreCase));
    }

    private CategoryNodeViewModel? FindNodeByPath(string? fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
            return CategoryTree.FirstOrDefault();

        foreach (var node in CategoryTree)
        {
            if (node.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                return node;

            var child = node.Children.FirstOrDefault(c => c.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase));
            if (child != null)
                return child;
        }

        return CategoryTree.FirstOrDefault();
    }

    private CategoryGroup? FindGroup(string groupName)
    {
        return Config.CategoryGroups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
    }

    public void RestoreExpandedState(IEnumerable<string> expandedPaths)
    {
        var set = new HashSet<string>(expandedPaths, StringComparer.OrdinalIgnoreCase);
        foreach (var node in CategoryTree)
        {
            if (set.Contains(node.FullPath))
                node.IsExpanded = true;
        }
    }

    public async Task ExecuteNestGroupAsSubcategoryAsync(string sourceGroupName, string targetGroupName)
    {
        var source = FindGroup(sourceGroupName);
        var target = FindGroup(targetGroupName);
        if (source == null || target == null || ReferenceEquals(source, target)) return;

        if (target.Subcategories.Any(s => s.Equals(sourceGroupName, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(
                $"'{targetGroupName}' already has a subcategory named '{sourceGroupName}'.",
                "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (source.Subcategories.Count > 0)
        {
            var subList = string.Join(", ", source.Subcategories);
            var confirm = MessageBox.Show(
                $"'{sourceGroupName}' has subcategories ({subList}).\n" +
                $"All entries under '{sourceGroupName}' will be moved to '{targetGroupName}/{sourceGroupName}'.\n\nContinue?",
                "Confirm Nest", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;
        }

        // Move all entries that lived under sourceGroup → targetGroup/sourceGroup
        foreach (var entry in _allEntries)
        {
            if (entry.Category.Equals(sourceGroupName, StringComparison.OrdinalIgnoreCase) ||
                entry.Category.StartsWith(sourceGroupName + "/", StringComparison.OrdinalIgnoreCase))
            {
                entry.Category = $"{targetGroupName}/{sourceGroupName}";
            }
        }

        target.Subcategories.Add(sourceGroupName);
        Config.CategoryGroups.Remove(source);

        await _configService.SaveConfigAsync(Config).ConfigureAwait(false);
        await _storageService.SaveUrlsAsync(_allEntries.ToList()).ConfigureAwait(false);
        Application.Current.Dispatcher.Invoke(() =>
        {
            _entriesView?.Refresh();
            RebuildCategoryTree();
        });
    }

    public async Task ExecutePromoteSubcategoryAsync(string subFullPath)
    {
        var parts = subFullPath.Split('/', 2);
        if (parts.Length != 2) return;

        var sourceGroupName = parts[0];
        var subName = parts[1];

        var sourceGroup = FindGroup(sourceGroupName);
        if (sourceGroup == null) return;

        if (Config.CategoryGroups.Any(g => g.Name.Equals(subName, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(
                $"A top-level category named '{subName}' already exists.",
                "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        sourceGroup.Subcategories.RemoveAll(s => s.Equals(subName, StringComparison.OrdinalIgnoreCase));

        var newGroup = new CategoryGroup { Name = subName, Color = "#3F51B5" };
        Config.CategoryGroups.Add(newGroup);

        foreach (var entry in _allEntries.Where(e => e.Category.Equals(subFullPath, StringComparison.OrdinalIgnoreCase)))
            entry.Category = subName;

        await _configService.SaveConfigAsync(Config).ConfigureAwait(false);
        await _storageService.SaveUrlsAsync(_allEntries.ToList()).ConfigureAwait(false);
        Application.Current.Dispatcher.Invoke(() =>
        {
            _entriesView?.Refresh();
            RebuildCategoryTree();
        });
    }

    public async Task ExecuteReorderSubcategoryAsync(string sourceFullPath, string targetFullPath, bool insertAfter)
    {
        var srcParts = sourceFullPath.Split('/', 2);
        var tgtParts = targetFullPath.Split('/', 2);
        if (srcParts.Length != 2 || tgtParts.Length != 2) return;

        var srcGroupName = srcParts[0];
        var srcSubName  = srcParts[1];
        var tgtGroupName = tgtParts[0];
        var tgtSubName  = tgtParts[1];

        var srcGroup = FindGroup(srcGroupName);
        var tgtGroup = FindGroup(tgtGroupName);
        if (srcGroup == null || tgtGroup == null) return;

        if (srcGroupName.Equals(tgtGroupName, StringComparison.OrdinalIgnoreCase))
        {
            // Same group — reorder within the list
            var list = srcGroup.Subcategories;
            var srcIdx = list.FindIndex(s => s.Equals(srcSubName, StringComparison.OrdinalIgnoreCase));
            var tgtIdx = list.FindIndex(s => s.Equals(tgtSubName, StringComparison.OrdinalIgnoreCase));
            if (srcIdx < 0 || tgtIdx < 0 || srcIdx == tgtIdx) return;

            list.RemoveAt(srcIdx);
            // Recalculate target index after removal
            tgtIdx = list.FindIndex(s => s.Equals(tgtSubName, StringComparison.OrdinalIgnoreCase));
            list.Insert(insertAfter ? tgtIdx + 1 : tgtIdx, srcSubName);
        }
        else
        {
            // Different group — move subcategory to target group, insert near the target sub
            if (tgtGroup.Subcategories.Any(s => s.Equals(srcSubName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(
                    $"'{tgtGroupName}' already has a subcategory named '{srcSubName}'.",
                    "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            srcGroup.Subcategories.RemoveAll(s => s.Equals(srcSubName, StringComparison.OrdinalIgnoreCase));

            var tgtIdx = tgtGroup.Subcategories.FindIndex(s => s.Equals(tgtSubName, StringComparison.OrdinalIgnoreCase));
            if (tgtIdx < 0)
                tgtGroup.Subcategories.Add(srcSubName);
            else
                tgtGroup.Subcategories.Insert(insertAfter ? tgtIdx + 1 : tgtIdx, srcSubName);

            var newPath = $"{tgtGroupName}/{srcSubName}";
            foreach (var entry in _allEntries.Where(en => en.Category.Equals(sourceFullPath, StringComparison.OrdinalIgnoreCase)))
                entry.Category = newPath;

            await _storageService.SaveUrlsAsync(_allEntries.ToList()).ConfigureAwait(false);
        }

        await _configService.SaveConfigAsync(Config).ConfigureAwait(false);
        Application.Current.Dispatcher.Invoke(() =>
        {
            _entriesView?.Refresh();
            RebuildCategoryTree();
        });
    }

    public async Task ExecuteReorderGroupAsync(string sourceGroupName, string targetGroupName)
    {
        var source = FindGroup(sourceGroupName);
        var target = FindGroup(targetGroupName);
        if (source == null || target == null || ReferenceEquals(source, target)) return;

        var targetIndex = Config.CategoryGroups.IndexOf(target);
        Config.CategoryGroups.Remove(source);
        // Re-fetch index after removal (target may have shifted)
        targetIndex = Config.CategoryGroups.IndexOf(target);
        Config.CategoryGroups.Insert(targetIndex, source);

        await _configService.SaveConfigAsync(Config).ConfigureAwait(false);
        Application.Current.Dispatcher.Invoke(RebuildCategoryTree);
    }

    public async Task ExecuteReparentSubcategoryAsync(string subFullPath, string targetGroupName)
    {
        var parts = subFullPath.Split('/', 2);
        if (parts.Length != 2) return;

        var sourceGroupName = parts[0];
        var subName = parts[1];

        if (sourceGroupName.Equals(targetGroupName, StringComparison.OrdinalIgnoreCase)) return;

        var sourceGroup = FindGroup(sourceGroupName);
        var targetGroup = FindGroup(targetGroupName);
        if (sourceGroup == null || targetGroup == null) return;

        if (targetGroup.Subcategories.Any(s => s.Equals(subName, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(
                $"'{targetGroupName}' already has a subcategory named '{subName}'.",
                "Duplicate",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        sourceGroup.Subcategories.RemoveAll(s => s.Equals(subName, StringComparison.OrdinalIgnoreCase));
        targetGroup.Subcategories.Add(subName);

        var newPath = $"{targetGroupName}/{subName}";
        foreach (var entry in _allEntries.Where(e => e.Category.Equals(subFullPath, StringComparison.OrdinalIgnoreCase)))
            entry.Category = newPath;

        await _configService.SaveConfigAsync(Config).ConfigureAwait(false);
        await _storageService.SaveUrlsAsync(_allEntries.ToList()).ConfigureAwait(false);
        Application.Current.Dispatcher.Invoke(() =>
        {
            _entriesView?.Refresh();
            RebuildCategoryTree();
        });
    }

    private static AddEditWindow CreateOwnedAddEditWindow(AddEditViewModel viewModel)
    {
        var window = new AddEditWindow(viewModel);
        var owner = Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;

        if (owner != null && !ReferenceEquals(owner, window))
            window.Owner = owner;

        return window;
    }

    private void ExecuteToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        Config.IsDarkMode = IsDarkMode;
        ThemeService.Apply(IsDarkMode);
        _ = _configService.SaveConfigAsync(Config);
    }
}
