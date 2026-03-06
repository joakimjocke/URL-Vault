using System.Globalization;
using System.Windows.Data;

namespace UrlVault.Converters;

[ValueConversion(typeof(string), typeof(string))]
public sealed class DateSavedConverter : IValueConverter
{
    // Output format: "2026-02-25 10:15"
    private const string DisplayFormat = "yyyy-MM-dd HH:mm";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string raw || string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dto))
            return dto.ToLocalTime().ToString(DisplayFormat, CultureInfo.CurrentCulture);

        // Fallback: return as-is if it can't be parsed
        return raw;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
