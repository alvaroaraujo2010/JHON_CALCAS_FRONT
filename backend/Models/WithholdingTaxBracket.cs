namespace ContaNexo.API.Models;

/// <summary>
/// Tabla de retención en la fuente para asalariados (Procedimiento 1, Art. 383 ET).
/// Rangos en UVT, tarifa marginal y subtotal de retención.
/// </summary>
public class WithholdingTaxBracket
{
    public int Id { get; set; }
    public int Year { get; set; }
    /// <summary>Rango desde (en UVT). Inclusivo.</summary>
    public decimal FromUvt { get; set; }
    /// <summary>Rango hasta (en UVT). Inclusivo. null = sin tope superior.</summary>
    public decimal? ToUvt { get; set; }
    /// <summary>Tarifa marginal (% sobre el excedente).</summary>
    public decimal MarginalRate { get; set; }
    /// <summary>Impuesto base en UVT (a sumar al excedente por la tarifa marginal).</summary>
    public decimal BaseTaxUvt { get; set; }
    public string Procedure { get; set; } = "1"; // 1 = general, 2 = simplificado
}
