namespace ContaNexo.API.Models;

public class Supplier
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
