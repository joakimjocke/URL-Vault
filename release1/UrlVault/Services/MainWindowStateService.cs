using System.IO;
using System.Text.Json;

namespace UrlVault.Services;

public sealed class MainWindowState
{
    public double Width { get; set; }
    public double Height { get; set; }
}

public sealed class MainWindowStateService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string DataDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
    private static string StateFile => Path.Combine(DataDirectory, "mainwindow-state.json");

    public MainWindowState? Load()
    {
        try
        {
            if (!File.Exists(StateFile))
                return null;

            var json = File.ReadAllText(StateFile);
            return JsonSerializer.Deserialize<MainWindowState>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Save(MainWindowState state)
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            var tmpFile = StateFile + ".tmp";
            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(tmpFile, json);
            File.Move(tmpFile, StateFile, overwrite: true);
        }
        catch
        {
            // Best effort persistence only.
        }
    }
}
