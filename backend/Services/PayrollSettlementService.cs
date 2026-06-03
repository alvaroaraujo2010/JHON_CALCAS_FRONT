using ContaNexo.API.Models;

namespace ContaNexo.API.Services;

/// <summary>
/// Liquidación definitiva de prestaciones sociales (Art. 249 CST, Ley 50/1990, Ley 789/2002).
///   * Cesantías: días trabajados en el último año / 360 * (salario + aux)
///   * Intereses sobre cesantías: 12% anual sobre cesantías
///   * Prima de servicios: semestre en curso, proporcional (Art. 306 CST)
///   * Vacaciones: 15 días hábiles por año, proporcional
///   * Indemnización por despido sin justa causa:
///       - Contrato < 1 año: 1 mes de salario
///       - Contrato >= 1 año: >=30 días de salario por el primer año + >=20 días por año adicional
/// </summary>
public class PayrollSettlementService
{
    public PayrollSettlementResult Calculate(
        Employee employee,
        DateTime settlementDate,
        DateTime lastDayWorked,
        decimal variableAverage3Months = 0m)
    {
        var result = new PayrollSettlementResult { Employee = employee, SettlementDate = settlementDate };

        var workedDaysCurrentYear = (lastDayWorked - new DateTime(lastDayWorked.Year, 1, 1)).Days + 1;
        result.WorkedDaysCurrentYear = workedDaysCurrentYear;

        var semesterStart = lastDayWorked.Month <= 6 ? new DateTime(lastDayWorked.Year, 1, 1) : new DateTime(lastDayWorked.Year, 7, 1);
        result.WorkedDaysCurrentSemester = (lastDayWorked - semesterStart).Days + 1;
        result.SemesterStart = semesterStart;

        result.TotalWorkedDays = (int)(lastDayWorked - employee.HireDate).TotalDays;

        var baseForProvisions = employee.BaseSalary + variableAverage3Months;
        result.BaseForProvisions = baseForProvisions;

        var transportAllowance = employee.IntegralSalary ? 0m : 200_000m;
        if (!employee.IntegralSalary)
        {
            result.CesantiasAmount = Math.Round((employee.BaseSalary + transportAllowance + variableAverage3Months) * workedDaysCurrentYear / 360m, 0);
        }
        else
        {
            result.CesantiasAmount = Math.Round(employee.BaseSalary * 0.70m * workedDaysCurrentYear / 360m, 0);
        }

        result.CesantiasInterestAmount = Math.Round(result.CesantiasAmount * 0.12m, 0);

        var semesterTotalDays = DateTime.IsLeapYear(lastDayWorked.Year) ? 182 : 181;
        var primaBase = employee.IntegralSalary ? employee.BaseSalary * 0.70m : employee.BaseSalary + transportAllowance + variableAverage3Months;
        result.PrimaAmount = Math.Round(primaBase * result.WorkedDaysCurrentSemester / semesterTotalDays, 0);

        var vacationDaysAccrued = result.WorkedDaysCurrentYear * 15m / 360m;
        result.VacationDays = Math.Round(vacationDaysAccrued, 2);
        result.VacationAmount = Math.Round(employee.BaseSalary * (decimal)result.VacationDays / 30m, 0);

        if (employee.TerminationReason == "sin_justa_causa")
        {
            var yearsWorked = result.TotalWorkedDays / 365m;
            if (yearsWorked < 1m)
            {
                result.SeveranceAmount = employee.BaseSalary;
            }
            else
            {
                var firstYear = employee.BaseSalary;
                var additionalYears = (int)Math.Floor(yearsWorked) - 1;
                var additional = additionalYears > 0 ? employee.BaseSalary * (20m * additionalYears) / 30m : 0m;
                result.SeveranceAmount = Math.Round(firstYear + additional, 0);
            }
        }
        else
        {
            result.SeveranceAmount = 0m;
        }

        result.TotalGross = result.CesantiasAmount + result.CesantiasInterestAmount
            + result.PrimaAmount + result.VacationAmount + result.SeveranceAmount;

        result.NetToPay = result.TotalGross - result.RetencionFuente;

        return result;
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
}
