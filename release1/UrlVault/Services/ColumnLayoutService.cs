using System.IO;
using System.Text.Json;

namespace UrlVault.Services;

public sealed class ColumnLayoutEntry
{
    public string Key { get; set; } = "";
    public int Order { get; set; }
    public double? Width { get; set; }
}

internal sealed class MainWindowColumnLayoutState
{
    public List<ColumnLayoutEntry> Columns { get; set; } = new();
}

public sealed class ColumnLayoutService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string DataDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
    private static string LayoutFile => Path.Combine(DataDirectory, "mainwindow-columns.json");

    public IReadOnlyList<ColumnLayoutEntry> LoadMainWindowColumns()
    {
        try
        {
            if (!File.Exists(LayoutFile))
                return Array.Empty<ColumnLayoutEntry>();

            var json = File.ReadAllText(LayoutFile);
            var state = JsonSerializer.Deserialize<MainWindowColumnLayoutState>(json, JsonOptions);
            return state?.Columns ?? new List<ColumnLayoutEntry>();
        }
        catch
        {
            return Array.Empty<ColumnLayoutEntry>();
        }
    }

    public void SaveMainWindowColumns(IEnumerable<ColumnLayoutEntry> columns)
    {
        try
        {
            var state = new MainWindowColumnLayoutState
            {
                Columns = columns
                    .Where(c => !string.IsNullOrWhiteSpace(c.Key))
                    .OrderBy(c => c.Order)
                    .ToList()
            };

            Directory.CreateDirectory(DataDirectory);
            var tmpFile = LayoutFile + ".tmp";
            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(tmpFile, json);
            File.Move(tmpFile, LayoutFile, overwrite: true);
        }
        catch
        {
            // Best effort persistence only.
        }
    }
}
