namespace ContaNexo.API.Models;

/// <summary>
/// Provisión mensual de prestaciones sociales por empleado.
/// Se acumula para pagos semestrales/anuales (prima, cesantías, intereses, vacaciones).
/// </summary>
public class PayrollProvision
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; } // 1..12
    public string PeriodLabel { get; set; } = string.Empty; // "2025-01"
    public decimal BaseSalary { get; set; }
    public decimal TransportAllowance { get; set; }

    // Provisiones del mes
    public decimal PrimaProvision { get; set; }        // (salario + aux) / 12
    public decimal CesantiasProvision { get; set; }    // (salario + aux) / 12
    public decimal CesantiasInterestProvision { get; set; } // cesantias * 12% / 12
    public decimal VacationProvision { get; set; }     // salario * 15/360
    public decimal TotalProvision { get; set; }

    // Acumulados del año
    public decimal AccumulatedPrima { get; set; }
    public decimal AccumulatedCesantias { get; set; }
    public decimal AccumulatedCesantiasInterest { get; set; }
    public decimal AccumulatedVacations { get; set; }
    public decimal AccumulatedTotal { get; set; }

    public int? PayrollId { get; set; }
    public Payroll? Payroll { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
