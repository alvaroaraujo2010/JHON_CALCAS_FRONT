namespace ContaNexo.API.Models;

/// <summary>
/// Lote de inventario. Cada compra genera un lote con su costo unitario.
/// Se utiliza para valoración PEPS (FIFO): se consumen primero los lotes
/// más antiguos. Para promedio ponderado, los lotes también se registran
/// pero el CMV se calcula contra el costo promedio móvil del producto.
/// </summary>
public class InventoryLot
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }
    /// <summary>Cantidad original ingresada en el lote.</summary>
    public int OriginalQuantity { get; set; }
    /// <summary>Cantidad aún disponible en el lote (OriginalQuantity - QuantityConsumed).</summary>
    public int RemainingQuantity { get; set; }
    /// <summary>Costo unitario de este lote.</summary>
    public decimal UnitCost { get; set; }
    /// <summary>Fecha de ingreso del lote (para PEPS).</summary>
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    /// <summary>Documento de referencia (ej: número de factura de compra).</summary>
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Método de valoración de inventarios soportado por producto.
/// (Art. 65 ET, Art. 1.2.1.20.6 DUR 1625/2016).
/// </summary>
public enum InventoryValuationMethod
{
    /// <summary>Promedio Ponderado Móvil (recomendado para alta rotación).</summary>
    WeightedAverage = 1,
    /// <summary>PEPS — Primero en Entrar, Primero en Salir (FIFO).</summary>
    FIFO = 2,
    /// <summary>UEPS — Último en Entrar, Primero en Salir (LIFO). No aceptado fiscalmente en Colombia desde 2004.</summary>
    LIFO = 3,
}
