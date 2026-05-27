namespace ContaNexo.API.Models;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
    public int MinStock { get; set; } = 5;
    public string Unit { get; set; } = "UND";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
