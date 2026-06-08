namespace ContaNexo.API.Models;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    /// <summary>
    /// Costo unitario promedio móvil (se recalcula con cada compra al recibir).
    /// Para PEPS, el UnitCost se mantiene como referencia pero el CMV real se
    /// calcula contra los lotes en <see cref="InventoryLot"/>.
    /// </summary>
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
    public int MinStock { get; set; } = 5;
    public string Unit { get; set; } = "UND";
    public bool IsActive { get; set; } = true;
    /// <summary>Método de valoración del inventario. Por defecto: promedio ponderado.</summary>
    public InventoryValuationMethod ValuationMethod { get; set; } = InventoryValuationMethod.WeightedAverage;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
