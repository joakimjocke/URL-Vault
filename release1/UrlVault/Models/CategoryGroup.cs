namespace UrlVault.Models;

public class CategoryGroup
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#3B80F7";  // Architect Blue
    public List<string> Subcategories { get; set; } = new();
}
