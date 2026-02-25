using System.Windows;
using UrlVault.ViewModels;

namespace UrlVault.Views;

public partial class AddEditWindow : Window
{
    public AddEditWindow(AddEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseAction = Close;
        Title = viewModel.IsEditMode ? "Edit URL" : "Add URL";
    }
}
