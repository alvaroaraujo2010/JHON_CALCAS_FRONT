namespace ContaNexo.API.Models;

public class SocialSecurityPayment
{
    public int Id { get; set; }
    public string Period { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? TaxId { get; set; }

    // ─── IBC (Ingreso Base de Cotización) ────────────────────
    public decimal BaseSalary { get; set; }
    public decimal TransportAllowance { get; set; }
    public decimal Ibc { get; set; }
    public decimal CapIbc { get; set; } // Tope 25 SMLMV (lo que realmente se aplica)

    // ─── Aportes del empleado ─────────────────────────────────
    public decimal EmployeeHealthContribution { get; set; }  // 4%
    public decimal EmployeePensionContribution { get; set; } // 4%
    public decimal SolidarityFundContribution { get; set; }  // 1% / 1.2% (si IBC > 4 SMLMV)

    // ─── Aportes del empleador ────────────────────────────────
    public decimal EmployerHealthContribution { get; set; }  // 8.5%
    public decimal EmployerPensionContribution { get; set; } // 12%
    public decimal ArlContribution { get; set; }             // Riesgo laboral
    public decimal CompensationFundContribution { get; set; } // Caja de compensación
    public decimal SenaContribution { get; set; }            // SENA
    public decimal IcbfContribution { get; set; }            // ICBF

    // ─── Totales ──────────────────────────────────────────────
    public decimal EmployeeContributionTotal { get; set; }
    public decimal EmployerContributionTotal { get; set; }
    public decimal ContributionRate { get; set; } // % informativo
    public decimal ContributionAmount { get; set; }

    public string? PaymentDate { get; set; }
    public string Status { get; set; } = "pending"; // pending | paid
    public string? Reference { get; set; } // N° de planilla PILA
    public string? Operator { get; set; } // EPS, AFP, ARL, Caja
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
