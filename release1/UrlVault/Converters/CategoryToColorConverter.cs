using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using UrlVault.ViewModels;

namespace UrlVault.Converters;

public class CategoryToColorConverter : IValueConverter
{
    private static readonly Brush DefaultBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F51B5"));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var categoryPath = (value as string ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(categoryPath))
            return DefaultBrush;

        var groupName = categoryPath.Split('/')[0];
        MainViewModel? vm = parameter as MainViewModel;
        if (vm == null && parameter is FrameworkElement element)
            vm = element.DataContext as MainViewModel;

        if (vm != null)
        {
            var group = vm.Config.CategoryGroups
                .FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            if (group != null)
                return CreateBrush(group.Color);
        }

        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }

    private static Brush CreateBrush(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch
        {
            return DefaultBrush;
        }
    }
}
