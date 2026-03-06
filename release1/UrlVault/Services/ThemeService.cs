using System.Windows;

namespace UrlVault.Services;

/// <summary>
/// Swaps the active theme dictionary at the application resource level.
/// All DynamicResource bindings across every open window update instantly.
/// </summary>
public static class ThemeService
{
    private const string LightThemeUri = "/Themes/LightTheme.xaml";
    private const string DarkThemeUri  = "/Themes/DarkTheme.xaml";

    public static void Apply(bool isDark)
    {
        var uri     = new Uri(isDark ? DarkThemeUri : LightThemeUri, UriKind.Relative);
        var newDict = new ResourceDictionary { Source = uri };
        var merged  = Application.Current.Resources.MergedDictionaries;

        if (merged.Count > 0)
            merged[0] = newDict;   // replace the slot reserved in App.xaml
        else
            merged.Add(newDict);
    }
}
