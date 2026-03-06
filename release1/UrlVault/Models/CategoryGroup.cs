namespace UrlVault.Models;

public class CategoryGroup
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#3F51B5";
    public List<string> Subcategories { get; set; } = new();
}
