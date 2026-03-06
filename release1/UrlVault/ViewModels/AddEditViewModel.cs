using System.Collections.ObjectModel;
using System.Windows.Input;
using UrlVault.Models;
using UrlVault.Services;

namespace UrlVault.ViewModels;

public class TagSelection : BaseViewModel
{
    private bool _isSelected;
    public string Tag { get; set; } = "";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class AddEditViewModel : BaseViewModel
{
    private readonly TitleFetcherService _titleFetcher = new();
    private readonly List<UrlEntry> _existingEntries;
    private readonly UrlEntry? _editEntry;

    private string _url = "";
    private string _title = "";
    private string _comment = "";
    private bool _isFetchingTitle;
    private string _duplicateWarning = "";
    private CategoryGroup? _selectedGroup;
    private string? _selectedSubcategory;
    private string _newSubcategoryName = "";

    public event Func<UrlEntry, Task>? OnSaved;

    public string Url
    {
        get => _url;
        set
        {
            if (SetProperty(ref _url, value))
            {
                OnPropertyChanged(nameof(CanSave));
                DuplicateWarning = "";
            }
        }
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value);
    }

    public bool IsFetchingTitle
    {
        get => _isFetchingTitle;
        set => SetProperty(ref _isFetchingTitle, value);
    }

    public string DuplicateWarning
    {
        get => _duplicateWarning;
        set => SetProperty(ref _duplicateWarning, value);
    }

    public bool CanSave => !string.IsNullOrWhiteSpace(Url) && SelectedGroup != null;
    public bool IsEditMode { get; }

    public ObservableCollection<CategoryGroup> AvailableGroups { get; } = new();
    public ObservableCollection<string> AvailableSubcategories { get; } = new();
    public ObservableCollection<string> AvailableTags { get; } = new();
    public ObservableCollection<TagSelection> TagSelections { get; } = new();

    public CategoryGroup? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (!SetProperty(ref _selectedGroup, value))
                return;

            RefreshAvailableSubcategories();
            OnPropertyChanged(nameof(CategoryPath));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    public string? SelectedSubcategory
    {
        get => _selectedSubcategory;
        set
        {
            if (SetProperty(ref _selectedSubcategory, value))
                OnPropertyChanged(nameof(CategoryPath));
        }
    }

    public string NewSubcategoryName
    {
        get => _newSubcategoryName;
        set => SetProperty(ref _newSubcategoryName, value);
    }

    public bool HasAvailableSubcategories => AvailableSubcategories.Count > 0;

    public string CategoryPath =>
        SelectedGroup == null
            ? string.Empty
            : string.IsNullOrWhiteSpace(SelectedSubcategory)
                ? SelectedGroup.Name
                : $"{SelectedGroup.Name}/{SelectedSubcategory}";

    public ICommand FetchTitleCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddSubcategoryCommand { get; }

    public Action? CloseAction { get; set; }

    public AddEditViewModel(AppConfig config, List<UrlEntry> existingEntries, UrlEntry? editEntry)
    {
        _existingEntries = existingEntries;
        _editEntry = editEntry;
        IsEditMode = editEntry != null;

        foreach (var group in config.CategoryGroups)
            AvailableGroups.Add(group);

        foreach (var tag in config.Tags)
        {
            AvailableTags.Add(tag);
            TagSelections.Add(new TagSelection { Tag = tag, IsSelected = false });
        }

        if (IsEditMode && editEntry != null)
        {
            Url = editEntry.Url;
            Title = editEntry.Title;
            Comment = editEntry.Comment;
            ApplyCategoryPath(editEntry.Category);

            foreach (var ts in TagSelections)
                ts.IsSelected = editEntry.Tags.Contains(ts.Tag);
        }
        else
        {
            SelectedGroup = AvailableGroups.FirstOrDefault();
        }

        FetchTitleCommand = new RelayCommand(
            async () => await ExecuteFetchTitleAsync().ConfigureAwait(false),
            () => !string.IsNullOrWhiteSpace(Url) && !IsFetchingTitle);

        SaveCommand = new RelayCommand(
            async () => await ExecuteSaveAsync().ConfigureAwait(false),
            () => CanSave);

        CancelCommand = new RelayCommand(ExecuteCancel);
        AddSubcategoryCommand = new RelayCommand(ExecuteAddSubcategory, () => SelectedGroup != null && !string.IsNullOrWhiteSpace(NewSubcategoryName));
    }

    private void ApplyCategoryPath(string categoryPath)
    {
        if (string.IsNullOrWhiteSpace(categoryPath))
        {
            SelectedGroup = AvailableGroups.FirstOrDefault();
            return;
        }

        var parts = categoryPath.Split('/', 2);
        var group = AvailableGroups.FirstOrDefault(g => g.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase));
        SelectedGroup = group ?? AvailableGroups.FirstOrDefault();

        if (parts.Length == 2)
        {
            SelectedSubcategory = AvailableSubcategories.FirstOrDefault(s => s.Equals(parts[1], StringComparison.OrdinalIgnoreCase));
            if (SelectedSubcategory == null && SelectedGroup != null)
            {
                SelectedGroup.Subcategories.Add(parts[1]);
                RefreshAvailableSubcategories();
                SelectedSubcategory = parts[1];
            }
        }
    }

    private void RefreshAvailableSubcategories()
    {
        var current = SelectedSubcategory;
        AvailableSubcategories.Clear();

        if (SelectedGroup != null)
        {
            foreach (var sub in SelectedGroup.Subcategories)
                AvailableSubcategories.Add(sub);
        }

        if (!string.IsNullOrWhiteSpace(current) &&
            AvailableSubcategories.Any(s => s.Equals(current, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedSubcategory = AvailableSubcategories.First(s => s.Equals(current, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            SelectedSubcategory = null;
        }

        OnPropertyChanged(nameof(HasAvailableSubcategories));
    }

    private void ExecuteAddSubcategory()
    {
        if (SelectedGroup == null)
            return;

        var sub = NewSubcategoryName.Trim();
        if (string.IsNullOrWhiteSpace(sub))
            return;

        if (SelectedGroup.Subcategories.Any(s => s.Equals(sub, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedSubcategory = SelectedGroup.Subcategories.First(s => s.Equals(sub, StringComparison.OrdinalIgnoreCase));
            NewSubcategoryName = "";
            return;
        }

        SelectedGroup.Subcategories.Add(sub);
        RefreshAvailableSubcategories();
        SelectedSubcategory = sub;
        NewSubcategoryName = "";
    }

    private async Task ExecuteFetchTitleAsync()
    {
        IsFetchingTitle = true;
        try
        {
            Title = await _titleFetcher.FetchTitleAsync(Url).ConfigureAwait(false);
        }
        finally
        {
            IsFetchingTitle = false;
        }
    }

    private async Task ExecuteSaveAsync()
    {
        if (!CanSave) return;

        var isDuplicate = _existingEntries.Any(e =>
            e.Url.Equals(Url, StringComparison.OrdinalIgnoreCase) &&
            (_editEntry == null || e.Id != _editEntry.Id));

        if (isDuplicate)
        {
            DuplicateWarning = "This URL already exists in your vault.";
            return;
        }

        var now = DateTime.UtcNow.ToString("o");
        var selectedTags = TagSelections.Where(t => t.IsSelected).Select(t => t.Tag).ToList();

        UrlEntry entry;
        if (IsEditMode && _editEntry != null)
        {
            entry = new UrlEntry
            {
                Id = _editEntry.Id,
                Url = Url,
                Title = Title,
                Category = CategoryPath,
                Tags = selectedTags,
                Comment = Comment,
                DateSaved = _editEntry.DateSaved,
                LastModified = now
            };
        }
        else
        {
            entry = new UrlEntry
            {
                Url = Url,
                Title = Title,
                Category = CategoryPath,
                Tags = selectedTags,
                Comment = Comment,
                DateSaved = now,
                LastModified = now
            };
        }

        if (OnSaved != null)
            await OnSaved.Invoke(entry).ConfigureAwait(false);

        System.Windows.Application.Current.Dispatcher.Invoke(() => CloseAction?.Invoke());
    }

    private void ExecuteCancel()
    {
        CloseAction?.Invoke();
    }
}
