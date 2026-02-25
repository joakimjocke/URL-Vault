namespace UrlVault.Models;

public class UrlEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string DateSaved { get; set; } = "";
    public string Category { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string Comment { get; set; } = "";
    public string LastModified { get; set; } = "";
}
