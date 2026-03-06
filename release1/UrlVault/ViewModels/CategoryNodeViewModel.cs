using System.Collections.ObjectModel;

namespace UrlVault.ViewModels;

public class CategoryNodeViewModel : BaseViewModel
{
    private int _count;
    private bool _isExpanded;

    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string Color { get; set; } = "#3F51B5";
    public bool IsGroup { get; set; }
    public bool IsAll { get; set; }

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public ObservableCollection<CategoryNodeViewModel> Children { get; } = new();
}
