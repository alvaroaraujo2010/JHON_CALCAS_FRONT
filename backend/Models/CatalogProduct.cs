namespace ContaNexo.API.Models;

public class CatalogProduct
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string ProductLine { get; set; } = string.Empty;   // tank-protectors, wheel-decals, bike-decals, emblems
    public string Brand { get; set; } = string.Empty;          // Honda, Yamaha, AKT, etc.
    public string? Model { get; set; }                         // Modelo específico: CB125F, NAVI REF 001, etc.
    public string? MenuModel { get; set; }                     // Modelo del menú: CB, NAVI, FZ 25, etc.
    public string? DesignRef { get; set; }                     // REF 001, V1, V2, etc.
    public string? Color { get; set; }                         // Azul, Rojo, etc.
    public string Title { get; set; } = string.Empty;
    public int? InternalProductId { get; set; }                // Vínculo opcional con products (inventario)
    public Product? InternalProduct { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string ImageFileName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public string? City { get; set; }
    public string? Department { get; set; }
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "pending";  // pending, paid, processing, shipped, delivered, cancelled
    public string? MpPaymentId { get; set; }
    public string? MpPaymentStatus { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? StockDeductedAt { get; set; }
    /// <summary>Venta ERP generada al marcar el pedido como pagado (idempotente).</summary>
    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int CatalogProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
