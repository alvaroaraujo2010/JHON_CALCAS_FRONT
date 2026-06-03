namespace ContaNexo.API.Models;

/// <summary>
/// Liquidación definitiva de prestaciones sociales (Art. 249 CST, Ley 50/1990).
/// Generada al terminar el contrato laboral, incluye:
///   - Cesantías (proporcional al tiempo laborado en el año)
///   - Intereses sobre cesantías (proporcional)
///   - Prima de servicios (proporcional, semestre)
///   - Vacaciones (proporcional al tiempo laborado)
///   - Indemnización por despido sin justa causa (si aplica)
/// </summary>
public class PayrollSettlement
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;

    public DateTime SettlementDate { get; set; } // Fecha de liquidación
    public DateTime HireDate { get; set; }       // Fecha de ingreso
    public DateTime? LastContractDate { get; set; } // Último día trabajado
    public string TerminationReason { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public decimal TransportAllowance { get; set; }
    public decimal AverageVariableIncome { get; set; } // Promedio últimos 3 meses de variables

    // ─── Tiempo laborado ─────────────────────────────────────────
    public int WorkedDays { get; set; }      // Días trabajados en el último año
    public int WorkedDaysCurrentSemester { get; set; } // Para prima

    // ─── Conceptos a pagar ──────────────────────────────────────
    public decimal CesantiasAmount { get; set; }
    public decimal CesantiasInterestAmount { get; set; }
    public decimal PrimaAmount { get; set; }
    public decimal VacationAmount { get; set; }
    public decimal SeveranceAmount { get; set; } // Indemnización por despido sin justa causa
    public decimal OtherAmounts { get; set; }
    public decimal TotalGross { get; set; }

    // ─── Deducciones ────────────────────────────────────────────
    public decimal RetencionFuente { get; set; } // Retención en la fuente sobre prestaciones
    public decimal TotalDeductions { get; set; }
    public decimal NetToPay { get; set; }

    public string Status { get; set; } = "draft"; // draft | paid | cancelled
    public string? PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
