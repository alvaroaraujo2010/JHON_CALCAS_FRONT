using ContaNexo.API.Models;

namespace ContaNexo.API.Services;

/// <summary>
/// Liquidación definitiva de prestaciones sociales conforme a la normatividad colombiana:
///   * Art. 249 CST — Cesantías (proporcional al último año, 1 año = 360 días)
///   * Art. 1 Ley 52/1975 — Intereses sobre cesantías (12% anual)
///   * Art. 306 CST (subrogado Art. 15 Ley 52/1975) — Prima de servicios
///       (1 mes/año, 2 cuotas semestrales de 0.5 mes c/u, proporcional por días)
///   * Art. 186/192 CST — Vacaciones (15 días hábiles remunerados por año)
///   * Art. 64 CST (modificado Art. 28 Ley 789/2002) — Indemnización por despido sin justa causa
///       - < 1 año: 0
///       - 1 a < 5 años: 30 días + 1 día por año subsiguiente + proporcional por meses
///       - 5 a < 10 años: 20 días + 2 días por año subsiguiente al 5° + proporcional
///       - ≥ 10 años: 40 días + 1 día por año subsiguiente al 10° + proporcional
/// </summary>
public class PayrollSettlementService(LegalParameterService legal)
{
    /// <summary>
    /// Constante del año comercial colombiano (Art. 624 ET) usado como denominador
    /// para cálculo proporcional de cesantías, prima y vacaciones.
    /// </summary>
    public const int CommercialYearDays = 360;

    public async Task<PayrollSettlementResult> CalculateAsync(
        Employee employee,
        DateTime settlementDate,
        DateTime lastDayWorked,
        decimal variableAverage3Months = 0m,
        decimal primaAlreadyPaid = 0m,
        decimal vacationsAlreadyPaid = 0m)
    {
        var param = await legal.GetByYearAsync(settlementDate.Year);
        var result = new PayrollSettlementResult
        {
            Employee = employee,
            SettlementDate = settlementDate,
            PrimaAlreadyPaid = primaAlreadyPaid,
            VacationsAlreadyPaid = vacationsAlreadyPaid
        };

        var totalDaysSinceHire = (int)(lastDayWorked - employee.HireDate).TotalDays + 1;
        result.TotalWorkedDays = Math.Max(0, totalDaysSinceHire - 1);

        // Cesantías: tope 360 días = 1 año (Art. 249 CST)
        var workedDaysCurrentYear = Math.Min(totalDaysSinceHire, CommercialYearDays);
        result.WorkedDaysCurrentYear = workedDaysCurrentYear;

        // Prima: semestre en curso
        var semesterStart = lastDayWorked.Month <= 6
            ? new DateTime(lastDayWorked.Year, 1, 1)
            : new DateTime(lastDayWorked.Year, 7, 1);
        result.WorkedDaysCurrentSemester = (lastDayWorked - semesterStart).Days + 1;
        result.SemesterStart = semesterStart;

        var baseForProvisions = employee.BaseSalary + variableAverage3Months;
        result.BaseForProvisions = baseForProvisions;

        // Auxilio de transporte desde parámetros legales (Decreto 2731/1968, Art. 17 Ley 1/1963).
        // No aplica a salario integral (Art. 132 CST).
        var transportAllowance = employee.IntegralSalary ? 0m : param.TransportAllowance;

        // ── Cesantías (Art. 249 CST) ──
        if (!employee.IntegralSalary)
        {
            result.CesantiasAmount = Math.Round(
                (employee.BaseSalary + transportAllowance + variableAverage3Months)
                    * workedDaysCurrentYear / CommercialYearDays, 0);
        }
        else
        {
            result.CesantiasAmount = Math.Round(
                employee.BaseSalary * 0.70m * workedDaysCurrentYear / CommercialYearDays, 0);
        }
        result.CesantiasInterestAmount = Math.Round(result.CesantiasAmount * 0.12m, 0);

        // ── Prima de servicios (Art. 306 CST) ──
        // 1 mes por año = 0.5 mes por semestre.
        // Proporcional: base × días_semestre / 360 (año comercial, Art. 624 ET).
        var primaBase = employee.IntegralSalary
            ? employee.BaseSalary * 0.70m
            : employee.BaseSalary + transportAllowance + variableAverage3Months;
        var primaGross = Math.Round(primaBase * result.WorkedDaysCurrentSemester / CommercialYearDays, 0);
        result.PrimaAmount = Math.Max(0, primaGross - primaAlreadyPaid);

        // ── Vacaciones (Art. 186/192 CST) ──
        // 15 días hábiles remunerados por año, proporcional.
        var vacationDaysAccrued = workedDaysCurrentYear * 15m / CommercialYearDays;
        result.VacationDays = Math.Round(vacationDaysAccrued, 2);
        var vacationGross = Math.Round(employee.BaseSalary * (decimal)result.VacationDays / 30m, 0);
        result.VacationAmount = Math.Max(0, vacationGross - vacationsAlreadyPaid);

        // ── Indemnización por despido sin justa causa (Art. 64 CST, Ley 789/2002) ──
        if (employee.TerminationReason == "sin_justa_causa")
        {
            var yearsWorked = result.TotalWorkedDays / 365m;
            result.SeveranceAmount = CalculateSeverance(employee.BaseSalary, yearsWorked);
        }

        result.TotalGross = result.CesantiasAmount + result.CesantiasInterestAmount
            + result.PrimaAmount + result.VacationAmount + result.SeveranceAmount;

        result.NetToPay = result.TotalGross - result.RetencionFuente;

        return result;
    }

    /// <summary>
    /// Indemnización por despido sin justa causa (Art. 64 CST modificado por Art. 28 Ley 789/2002).
    /// Tramos progresivos con proporcionalidad por meses cumplidos.
    /// </summary>
    /// <param name="baseSalary">Salario base mensual del empleado.</param>
    /// <param name="yearsWorked">Años laborados (fraccional: ej. 4.5).</param>
    /// <returns>Monto en pesos de la indemnización.</returns>
    public static decimal CalculateSeverance(decimal baseSalary, decimal yearsWorked)
    {
        var monthsExact = yearsWorked * 12m;

        if (monthsExact <= 12m) return 0m; // ≤ 1 año sin derecho a indemnización

        decimal indemnityDays;
        if (monthsExact < 60m) // 1 a < 5 años
        {
            indemnityDays = 30m + (monthsExact - 12m) / 12m;
        }
        else if (monthsExact < 120m) // 5 a < 10 años
        {
            indemnityDays = 20m + (monthsExact - 60m) * 2m / 12m;
        }
        else // ≥ 10 años
        {
            indemnityDays = 40m + (monthsExact - 120m) / 12m;
        }

        return Math.Round(baseSalary * indemnityDays / 30m, 0);
    }
}

public class PayrollSettlementResult
{
    public Employee Employee { get; set; } = null!;
    public DateTime SettlementDate { get; set; }
    public int WorkedDaysCurrentYear { get; set; }
    public int WorkedDaysCurrentSemester { get; set; }
    public int TotalWorkedDays { get; set; }
    public DateTime SemesterStart { get; set; }
    public decimal BaseForProvisions { get; set; }
    public decimal CesantiasAmount { get; set; }
    public decimal CesantiasInterestAmount { get; set; }
    public decimal PrimaAmount { get; set; }
    public decimal VacationDays { get; set; }
    public decimal VacationAmount { get; set; }
    public decimal SeveranceAmount { get; set; }
    public decimal TotalGross { get; set; }
    public decimal RetencionFuente { get; set; }
    public decimal NetToPay { get; set; }
    public decimal PrimaAlreadyPaid { get; set; }
    public decimal VacationsAlreadyPaid { get; set; }
}
