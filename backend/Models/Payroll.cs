namespace ContaNexo.API.Models;

public class Payroll
{
    public int Id { get; set; }
    public string PeriodStart { get; set; } = string.Empty;
    public string PeriodEnd { get; set; } = string.Empty;
    public string Status { get; set; } = "draft"; // draft | processed | paid | cancelled
    public string? PaymentDate { get; set; }

    // ─── Totales de la nómina ─────────────────────────────────
    public decimal TotalGross { get; set; }       // Total devengado bruto (salarios + auxilios)
    public decimal TotalTransportAllowance { get; set; } // Auxilio de transporte total
    public decimal TotalDeductions { get; set; }  // Total deducciones (salud, pensión, fondo, retención, etc.)
    public decimal TotalNet { get; set; }         // Total neto a pagar
    public decimal TotalEmployerCost { get; set; } // Costo total para el empleador (incluye aportes)

    // ─── Provisiones del período ───────────────────────────────
    public decimal TotalPrimaProvision { get; set; }
    public decimal TotalCesantiasProvision { get; set; }
    public decimal TotalCesantiasInterestProvision { get; set; }
    public decimal TotalVacationProvision { get; set; }

    // ─── Aportes patronales (informativo) ──────────────────────
    public decimal TotalEmployerHealth { get; set; }
    public decimal TotalEmployerPension { get; set; }
    public decimal TotalArl { get; set; }
    public decimal TotalCompensationFund { get; set; }
    public decimal TotalSena { get; set; }
    public decimal TotalIcbf { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<PayrollDetail> Details { get; set; } = new List<PayrollDetail>();
}

public class PayrollDetail
{
    public int Id { get; set; }
    public int PayrollId { get; set; }
    public Payroll? Payroll { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string EmployeeName { get; set; } = string.Empty;

    // ─── Devengados ───────────────────────────────────────────
    public decimal BaseSalary { get; set; }
    public decimal TransportAllowance { get; set; } // Auxilio de transporte
    public decimal TotalGross { get; set; } // Salario + Auxilio (Total devengado)

    // ─── IBC para aportes a seguridad social ──────────────────
    public decimal Ibc { get; set; } // Ingreso Base de Cotización

    // ─── Deducciones del empleado ─────────────────────────────
    public decimal EmployeeHealthDeduction { get; set; }  // 4% salud
    public decimal EmployeePensionDeduction { get; set; } // 4% pensión
    public decimal SolidarityFundDeduction { get; set; }  // Fondo Solidaridad Pensional (1% o 1.2%)
    public decimal WithholdingTax { get; set; }           // Retención en la fuente
    public decimal TotalDeductions { get; set; }

    // ─── Aportes del empleador ────────────────────────────────
    public decimal EmployerHealthContribution { get; set; }  // 8.5%
    public decimal EmployerPensionContribution { get; set; } // 12%
    public decimal ArlContribution { get; set; }             // Riesgo I: 0.522%
    public decimal CompensationFundContribution { get; set; } // 4%
    public decimal SenaContribution { get; set; }            // 2%
    public decimal IcbfContribution { get; set; }            // 3%
    public decimal TotalEmployerContributions { get; set; }

    // ─── Provisiones del mes ──────────────────────────────────
    public decimal PrimaProvision { get; set; }           // 1 mes / 12
    public decimal CesantiasProvision { get; set; }       // 1 mes / 12
    public decimal CesantiasInterestProvision { get; set; } // 12% anual / 12
    public decimal VacationProvision { get; set; }        // 15/360 del salario
    public decimal TotalProvisions { get; set; }

    // ─── Neto a pagar ─────────────────────────────────────────
    public decimal NetSalary { get; set; }

    // ─── Deducciones personalizadas adicionales ───────────────
    public ICollection<PayrollDeductionLine> Deductions { get; set; } = new List<PayrollDeductionLine>();
}

public class PayrollDeductionLine
{
    public int Id { get; set; }
    public int PayrollDetailId { get; set; }
    public PayrollDetail? PayrollDetail { get; set; }
    public int DeductionId { get; set; }
    public string DeductionName { get; set; } = string.Empty;
    public string Category { get; set; } = "other"; // social_security | tax | loan | other
    public decimal Amount { get; set; }
}
