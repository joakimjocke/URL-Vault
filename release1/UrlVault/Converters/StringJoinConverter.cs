using System.Globalization;
using System.Windows.Data;

namespace UrlVault.Converters;

public class StringJoinConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<string> strings)
            return string.Join(", ", strings);
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
