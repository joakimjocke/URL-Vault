using System.Windows;
using System.Windows.Controls;
using UrlVault.ViewModels;

namespace UrlVault.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
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
}
