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

    private sealed class LegacyConfig
    {
        public List<string> Categories { get; set; } = new();
        public List<string> Tags { get; set; } = new();
    }

    private static string DataDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
    private static string ConfigFile => Path.Combine(DataDirectory, "config.json");

    public async Task<AppConfig> LoadConfigAsync()
    {
        try
        {
            if (!File.Exists(ConfigFile))
            {
                var defaultConfig = CreateDefaultConfig();
                await SaveConfigAsync(defaultConfig).ConfigureAwait(false);
                return defaultConfig;
            }

            var json = await File.ReadAllTextAsync(ConfigFile).ConfigureAwait(false);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);

            if (config?.CategoryGroups?.Count > 0)
            {
                return NormalizeConfig(config);
            }

            if (HasLegacyCategories(json))
            {
                var migrated = MigrateLegacyConfig(json);
                await SaveConfigAsync(migrated).ConfigureAwait(false);
                return migrated;
            }

            return NormalizeConfig(config ?? new AppConfig());
        }
        catch
        {
            return CreateDefaultConfig();
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

    private static bool HasLegacyCategories(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name.Equals("categories", StringComparison.OrdinalIgnoreCase) &&
                    prop.Value.ValueKind == JsonValueKind.Array)
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static AppConfig MigrateLegacyConfig(string json)
    {
        var legacy = JsonSerializer.Deserialize<LegacyConfig>(json, JsonOptions) ?? new LegacyConfig();
        var migrated = new AppConfig
        {
            CategoryGroups = legacy.Categories
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => new CategoryGroup
                {
                    Name = c.Trim(),
                    Color = "#3F51B5",
                    Subcategories = new List<string>()
                })
                .ToList(),
            Tags = legacy.Tags
        };

        return NormalizeConfig(migrated);
    }

    private static AppConfig NormalizeConfig(AppConfig config)
    {
        config.CategoryGroups ??= new List<CategoryGroup>();
        config.Tags ??= new List<string>();

        config.CategoryGroups = config.CategoryGroups
            .Where(g => !string.IsNullOrWhiteSpace(g.Name))
            .Select(g => new CategoryGroup
            {
                Name = g.Name.Trim(),
                Color = string.IsNullOrWhiteSpace(g.Color) ? "#3F51B5" : g.Color.Trim(),
                Subcategories = (g.Subcategories ?? new List<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        return config;
    }

    private static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            CategoryGroups = new List<CategoryGroup>
            {
                new() { Name = "Work", Color = "#3F51B5", Subcategories = new List<string> { "Projects", "Docs" } },
                new() { Name = "Personal", Color = "#00897B", Subcategories = new List<string> { "Shopping", "Finance" } },
                new() { Name = "Dev", Color = "#6D4C41", Subcategories = new List<string> { "CSharp", "Tools" } },
                new() { Name = "Infra", Color = "#455A64", Subcategories = new List<string>() }
            },
            Tags = new List<string> { "C#", "React", "Security", "Docker", "Neo4j" }
        };
    }
}
