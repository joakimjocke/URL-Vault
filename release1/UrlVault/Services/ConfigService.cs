using System.IO;
using System.Text.Json;
using UrlVault.Models;

namespace UrlVault.Services;

public class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string DataDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
    private static string ConfigFile => Path.Combine(DataDirectory, "config.json");

    public async Task<AppConfig> LoadConfigAsync()
    {
        try
        {
            if (!File.Exists(ConfigFile))
            {
                var defaultConfig = new AppConfig
                {
                    Categories = new List<string> { "Work", "Personal", "Hacking", "Dev", "Infra" },
                    Tags = new List<string> { "C#", "React", "Security", "Docker", "Neo4j" }
                };
                await SaveConfigAsync(defaultConfig).ConfigureAwait(false);
                return defaultConfig;
            }

            var json = await File.ReadAllTextAsync(ConfigFile).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig
            {
                Categories = new List<string> { "Work", "Personal", "Hacking", "Dev", "Infra" },
                Tags = new List<string> { "C#", "React", "Security", "Docker", "Neo4j" }
            };
        }
    }

    public async Task SaveConfigAsync(AppConfig config)
    {
        Directory.CreateDirectory(DataDirectory);
        var tmpFile = ConfigFile + ".tmp";
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(tmpFile, json).ConfigureAwait(false);
        File.Move(tmpFile, ConfigFile, overwrite: true);
    }
}
