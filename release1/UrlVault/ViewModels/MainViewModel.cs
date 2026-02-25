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
    private string _selectedCategoryFilter = "All";
    private AppConfig _config = new();
    private UrlEntry? _selectedEntry;
    private bool _isBusy;

    public ObservableCollection<string> SelectedTagFilters { get; } = new();
    public ObservableCollection<string> CategoryFilters { get; } = new();

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

    public string SelectedCategoryFilter
    {
        get => _selectedCategoryFilter;
        set
        {
            if (SetProperty(ref _selectedCategoryFilter, value))
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

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public int DisplayedCount => _entriesView?.Cast<object>().Count() ?? 0;

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CopyUrlCommand { get; }
    public ICommand OpenUrlCommand { get; }
    public ICommand RefreshFiltersCommand { get; }

    public MainViewModel()
    {
        AddCommand = new RelayCommand(ExecuteAdd);
        EditCommand = new RelayCommand(ExecuteEdit, () => SelectedEntry != null);
        DeleteCommand = new RelayCommand(ExecuteDelete, () => SelectedEntry != null);
        CopyUrlCommand = new RelayCommand(ExecuteCopyUrl, () => SelectedEntry != null);
        OpenUrlCommand = new RelayCommand(ExecuteOpenUrl, () => SelectedEntry != null);
        RefreshFiltersCommand = new RelayCommand(() => _entriesView?.Refresh());

        SelectedTagFilters.CollectionChanged += (_, _) => _entriesView?.Refresh();
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Config = await _configService.LoadConfigAsync().ConfigureAwait(false);
            var entries = await _storageService.LoadUrlsAsync().ConfigureAwait(false);

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Build category filter list
                CategoryFilters.Clear();
                CategoryFilters.Add("All");
                foreach (var cat in Config.Categories)
                    CategoryFilters.Add(cat);

                _allEntries = new ObservableCollection<UrlEntry>(entries);
                var view = CollectionViewSource.GetDefaultView(_allEntries);
                view.Filter = FilterEntry;
                view.SortDescriptions.Add(new SortDescription(nameof(UrlEntry.DateSaved), ListSortDirection.Descending));
                EntriesView = view;
                EntriesView.CollectionChanged += (_, _) => OnPropertyChanged(nameof(DisplayedCount));
                OnPropertyChanged(nameof(DisplayedCount));
            });
        }
        finally
        {
            IsBusy = false;
        }
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

        if (!string.IsNullOrEmpty(SelectedCategoryFilter) && SelectedCategoryFilter != "All")
        {
            if (entry.Category != SelectedCategoryFilter) return false;
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
        vm.OnSaved += async (entry) =>
        {
            _allEntries.Add(entry);
            await _storageService.SaveUrlsAsync(_allEntries.ToList()).ConfigureAwait(false);
            Application.Current.Dispatcher.Invoke(() => OnPropertyChanged(nameof(DisplayedCount)));
        };
        var window = CreateOwnedAddEditWindow(vm);
        window.ShowDialog();
    }

    private void ExecuteEdit()
    {
        if (SelectedEntry == null) return;
        var original = SelectedEntry;
        var vm = new AddEditViewModel(_config, _allEntries.ToList(), original);
        vm.OnSaved += async (entry) =>
        {
            var idx = _allEntries.IndexOf(original);
            if (idx >= 0)
                _allEntries[idx] = entry;
            await _storageService.SaveUrlsAsync(_allEntries.ToList()).ConfigureAwait(false);
            Application.Current.Dispatcher.Invoke(() => _entriesView?.Refresh());
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
            OnPropertyChanged(nameof(DisplayedCount));
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
        catch { }
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
}

