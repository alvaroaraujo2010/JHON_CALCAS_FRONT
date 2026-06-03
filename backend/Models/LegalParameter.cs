namespace ContaNexo.API.Models;

/// <summary>
/// Parámetros legales colombianos por año gravable.
/// Almacena SMLMV, UVT, Subsidio de Transporte, topes y porcentajes
/// establecidos por el gobierno nacional cada año.
/// </summary>
public class LegalParameter
{
    public int Id { get; set; }
    public int Year { get; set; } // 2025, 2026, ...
    public decimal Smlmv { get; set; }             // Salario mínimo legal mensual vigente
    public decimal Uvt { get; set; }               // Unidad de Valor Tributario
    public decimal TransportAllowance { get; set; } // Auxilio de transporte mensual
    public decimal TransportAllowanceTop { get; set; } // Tope salarial para auxilio (multiplicador SMLMV, normalmente 2)
    public decimal MinimumWithholdingUvt { get; set; } // UVT desde la cual aplica retención (depende procedimiento)
    public decimal ExemptIncomeUvt { get; set; }   // 25% del ingreso laboral exento (UVT)
    public decimal MaxHealthIbcSmlmv { get; set; } // Tope IBC salud/pensión (normalmente 25 SMLMV)
    public decimal ArlRiskOneRate { get; set; }    // % ARL riesgo I (default 0.522%)
    public decimal EmployerHealthRate { get; set; } // 8.5%
    public decimal EmployerPensionRate { get; set; } // 12.0%
    public decimal CompensationFundRate { get; set; } // 4% (caja)
    public decimal SenaRate { get; set; }          // 2%
    public decimal IcbfRate { get; set; }          // 3%
    public decimal EmployeeHealthRate { get; set; } // 4%
    public decimal EmployeePensionRate { get; set; } // 4%
    public decimal SolidarityFundLowRate { get; set; } // 1.0% (entre 4 y 16-19 SMLMV)
    public decimal SolidarityFundHighRate { get; set; } // 1.2% (>16-19 SMLMV)
    public decimal PrimaYearFraction { get; set; } // 1 (1 mes por año)
    public decimal CesantiasYearFraction { get; set; } // 1
    public decimal CesantiasInterestRate { get; set; } // 12% anual
    public decimal VacationDaysPerYear { get; set; } // 15 hábiles
    public DateTime EffectiveFrom { get; set; } // Fecha de vigencia
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
