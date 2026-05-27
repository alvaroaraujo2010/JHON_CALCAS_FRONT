namespace ContaNexo.API.Models;

public class Sale
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "Efectivo";
    public string Status { get; set; } = "Completada";
    /// <summary>Borrador | Emitida (factura electronica interna, lista para DIAN).</summary>
    public string ElectronicInvoiceStatus { get; set; } = "Borrador";
    public string? ElectronicInvoiceNumber { get; set; }
    public string? Cufe { get; set; }
    public DateTime? ElectronicInvoiceIssuedAt { get; set; }
    public string? Notes { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
}

public class SaleDetail
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
