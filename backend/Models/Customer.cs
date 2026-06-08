namespace ContaNexo.API.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    /// <summary>NIT sin dígito de verificación (solo dígitos).</summary>
    public string? Nit { get; set; }
    /// <summary>Dígito de verificación calculado por módulo 11 (DIAN).</summary>
    public string? NitVerificationDigit { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Gran contribuyente / agente retenedor de renta.
    /// Si true, al vender se le practica retención en la fuente
    /// (2.5% sobre el subtotal, Art. 1.2.4.9.1 DUR 1625/2016 +
    ///  Art. 1.2.6.4 resolución DIAN).
    /// </summary>
    public bool IsRetentionAgent { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
