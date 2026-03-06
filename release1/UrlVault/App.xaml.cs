using System.IO;
using System.Text.Json;
using System.Windows;
using UrlVault.Services;

namespace UrlVault;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplyInitialTheme();
    }

    /// <summary>
    /// Reads only the IsDarkMode flag from config.json synchronously so the
    /// correct theme is set before the first window renders (no flash).
    /// </summary>
    private static void ApplyInitialTheme()
    {
        try
        {
            var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "config.json");
            if (!File.Exists(configFile)) return;

            var json = File.ReadAllText(configFile);
            using var doc = JsonDocument.Parse(json);
            // Use case-insensitive search — System.Text.Json serialises the property
            // as "IsDarkMode" (PascalCase) but we want to be resilient to casing.
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name.Equals("IsDarkMode", StringComparison.OrdinalIgnoreCase) &&
                    prop.Value.ValueKind == JsonValueKind.True)
                {
                    ThemeService.Apply(isDark: true);
                    break;
                }
            }
        }
        catch { /* fall back to light theme already set in App.xaml */ }
    }
}
