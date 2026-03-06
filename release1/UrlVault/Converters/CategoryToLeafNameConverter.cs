using System.Globalization;
using System.Windows.Data;

namespace UrlVault.Converters;

public class CategoryToLeafNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var path = (value as string ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? string.Empty : parts[^1];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
