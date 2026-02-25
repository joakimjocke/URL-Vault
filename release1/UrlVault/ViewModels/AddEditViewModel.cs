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
    private string _category = "";
    private string _comment = "";
    private bool _isFetchingTitle;
    private string _duplicateWarning = "";

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

    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
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

    public bool CanSave => !string.IsNullOrWhiteSpace(Url);
    public bool IsEditMode { get; }

    public ObservableCollection<string> AvailableCategories { get; } = new();
    public ObservableCollection<string> AvailableTags { get; } = new();
    public ObservableCollection<TagSelection> TagSelections { get; } = new();

    public ICommand FetchTitleCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public Action? CloseAction { get; set; }

    public AddEditViewModel(AppConfig config, List<UrlEntry> existingEntries, UrlEntry? editEntry)
    {
        _existingEntries = existingEntries;
        _editEntry = editEntry;
        IsEditMode = editEntry != null;

        foreach (var cat in config.Categories)
            AvailableCategories.Add(cat);

        foreach (var tag in config.Tags)
        {
            AvailableTags.Add(tag);
            TagSelections.Add(new TagSelection { Tag = tag, IsSelected = false });
        }

        if (IsEditMode && editEntry != null)
        {
            Url = editEntry.Url;
            Title = editEntry.Title;
            Category = editEntry.Category;
            Comment = editEntry.Comment;

            foreach (var ts in TagSelections)
                ts.IsSelected = editEntry.Tags.Contains(ts.Tag);
        }
        else
        {
            Category = config.Categories.FirstOrDefault() ?? "";
        }

        FetchTitleCommand = new RelayCommand(
            async () => await ExecuteFetchTitleAsync(),
            () => !string.IsNullOrWhiteSpace(Url) && !IsFetchingTitle);

        SaveCommand = new RelayCommand(
            async () => await ExecuteSaveAsync(),
            () => CanSave);

        CancelCommand = new RelayCommand(ExecuteCancel);
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
            DuplicateWarning = "⚠️ This URL already exists in your vault.";
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
                Category = Category,
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
                Category = Category,
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
