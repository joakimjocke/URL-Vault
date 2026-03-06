namespace UrlVault.Models;

public class AppConfig
{
    public List<CategoryGroup> CategoryGroups { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}
