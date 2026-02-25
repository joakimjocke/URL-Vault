using System.IO;
using System.Text.Json;
using UrlVault.Models;

namespace UrlVault.Services;

public class StorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string DataDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
    private static string UrlsFile => Path.Combine(DataDirectory, "urls.json");

    public async Task<List<UrlEntry>> LoadUrlsAsync()
    {
        try
        {
            if (!File.Exists(UrlsFile))
                return new List<UrlEntry>();

            var json = await File.ReadAllTextAsync(UrlsFile).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<UrlEntry>>(json, JsonOptions) ?? new List<UrlEntry>();
        }
        catch
        {
            return new List<UrlEntry>();
        }
    }

    public async Task SaveUrlsAsync(List<UrlEntry> entries)
    {
        Directory.CreateDirectory(DataDirectory);
        var tmpFile = UrlsFile + ".tmp";
        var json = JsonSerializer.Serialize(entries, JsonOptions);
        await File.WriteAllTextAsync(tmpFile, json).ConfigureAwait(false);
        File.Move(tmpFile, UrlsFile, overwrite: true);
    }
}
