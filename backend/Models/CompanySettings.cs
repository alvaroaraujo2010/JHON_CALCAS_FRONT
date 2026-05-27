namespace ContaNexo.API.Models;

public class CompanySettings
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = "ContaNexo";
    public string Tagline { get; set; } = "El nexo entre su contabilidad e inventario";
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string? TaxId { get; set; }
    public string Currency { get; set; } = "COP";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
