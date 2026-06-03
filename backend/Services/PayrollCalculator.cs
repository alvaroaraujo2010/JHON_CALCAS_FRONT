using ContaNexo.API.Models;

namespace ContaNexo.API.Services;

/// <summary>
/// Orquestador de cálculo de nómina colombiana con cumplimiento legal:
///   - Auxilio de transporte (Art. 17 Ley 1ª de 1963, Art. 22 Decreto 2731/1968)
///   - IBC para seguridad social
///   - Fondo de Solidaridad Pensional (Art. 8 Ley 797/2003)
///   - Provisiones de prestaciones (Art. 306 CST, Art. 249 CST, Art. 186 CST)
///   - Retención en la fuente (Art. 383 ET) vía WithholdingTaxService
/// </summary>
public class PayrollCalculator
{
    private readonly LegalParameterService _legal;
    private readonly WithholdingTaxService _withholding;

    public PayrollCalculator(LegalParameterService legal, WithholdingTaxService withholding)
    {
        _legal = legal;
        _withholding = withholding;
    }

    public async Task<PayrollComputation> ComputeAsync(EmployeeContext ctx, LegalParameter? paramOverride = null)
    {
        var param = paramOverride ?? await _legal.GetByYearAsync(LegalParameterService.GetYearFromDate(ctx.PayrollPeriodEnd));

        var calc = new PayrollComputation { Param = param };

        if (ctx.IntegralSalary)
        {
            calc.BaseSalary = ctx.BaseSalary;
            calc.TransportAllowance = 0;
            calc.GrossIncome = ctx.BaseSalary;
            calc.Ibc = Math.Round(ctx.BaseSalary * 0.70m, 0);
            calc.CappedIbc = calc.Ibc;
        }
        else
        {
            calc.BaseSalary = ctx.BaseSalary;
            calc.TransportAllowance = ComputeTransportAllowance(ctx, param);
            calc.GrossIncome = calc.BaseSalary + calc.TransportAllowance;
            calc.Ibc = calc.BaseSalary + ctx.VariableIncome;
            calc.CappedIbc = Math.Min(calc.Ibc, param.Smlmv * param.MaxHealthIbcSmlmv);
        }

        calc.EmployeeHealth = Round2(calc.CappedIbc * param.EmployeeHealthRate / 100m);
        calc.EmployeePension = Round2(calc.CappedIbc * param.EmployeePensionRate / 100m);
        calc.SolidarityFund = ComputeSolidarityFund(calc.Ibc, param, ctx.SolidarityFundOverride);

        calc.EmployerHealth = Round2(calc.CappedIbc * param.EmployerHealthRate / 100m);
        calc.EmployerPension = Round2(calc.CappedIbc * param.EmployerPensionRate / 100m);
        calc.Arl = Round2(calc.Ibc * param.ArlRiskOneRate / 100m);
        calc.CompensationFund = Round2(calc.Ibc * param.CompensationFundRate / 100m);
        calc.Sena = Round2(calc.Ibc * param.SenaRate / 100m);
        calc.Icbf = Round2(calc.Ibc * param.IcbfRate / 100m);

        if (ctx.IntegralSalary)
        {
            calc.PrimaProvision = 0;
            calc.CesantiasProvision = 0;
            calc.CesantiasInterestProvision = 0;
            calc.VacationProvision = 0;
        }
        else
        {
            var baseProvision = calc.BaseSalary + calc.TransportAllowance;
            calc.PrimaProvision = Round2(baseProvision * param.PrimaYearFraction / 12m);
            calc.CesantiasProvision = Round2(baseProvision * param.CesantiasYearFraction / 12m);
            calc.CesantiasInterestProvision = Round2(calc.CesantiasProvision * param.CesantiasInterestRate / 100m);
            calc.VacationProvision = Round2(calc.BaseSalary * param.VacationDaysPerYear / 360m);
        }

        var grossForTax = calc.BaseSalary + ctx.VariableIncome;
        var nonTaxable = calc.TransportAllowance + calc.EmployeeHealth + calc.EmployeePension;
        if (ctx.WithholdingProcedure2)
        {
            calc.WithholdingTax = _withholding.CalculateProcedureTwo(
                grossForTax, param, calc.EmployeeHealth + calc.EmployeePension + calc.SolidarityFund);
        }
        else
        {
            calc.WithholdingTax = _withholding.CalculateProcedureOne(grossForTax, param, nonTaxable);
        }

        calc.TotalEmployeeDeductions = calc.EmployeeHealth + calc.EmployeePension + calc.SolidarityFund
            + ctx.CustomDeductions.Sum(d => d.Amount) + calc.WithholdingTax;

        calc.NetPay = Math.Max(0, calc.GrossIncome - calc.TotalEmployeeDeductions);

        calc.TotalEmployerContributions = calc.EmployerHealth + calc.EmployerPension + calc.Arl
            + calc.CompensationFund + calc.Sena + calc.Icbf;

        calc.TotalProvisions = calc.PrimaProvision + calc.CesantiasProvision
            + calc.CesantiasInterestProvision + calc.VacationProvision;

        return calc;
    }

    public decimal ComputeTransportAllowance(EmployeeContext ctx, LegalParameter param)
    {
        if (ctx.IntegralSalary) return 0m;
        if (ctx.TransportAllowanceOverride == false) return 0m;
        if (ctx.TransportAllowanceOverride == true) return param.TransportAllowance;
        var top = param.Smlmv * param.TransportAllowanceTop;
        return ctx.BaseSalary < top ? param.TransportAllowance : 0m;
    }

    public decimal ComputeSolidarityFund(decimal ibc, LegalParameter param, decimal? overrideValue)
    {
        if (overrideValue.HasValue) return Round2(overrideValue.Value);
        var threshold = param.Smlmv * 4m;
        if (ibc <= threshold) return 0m;
        var excess = ibc - threshold;
        var highThreshold = param.Smlmv * 19m;
        if (ibc <= highThreshold)
        {
            return Round2(excess * param.SolidarityFundLowRate / 100m);
        }
        return Round2(excess * param.SolidarityFundHighRate / 100m);
    }

    private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}

public class EmployeeContext
{
    public decimal BaseSalary { get; set; }
    public decimal VariableIncome { get; set; }
    public bool IntegralSalary { get; set; }
    public bool? TransportAllowanceOverride { get; set; }
    public decimal? SolidarityFundOverride { get; set; }
    public bool WithholdingProcedure2 { get; set; }
    public List<DeductionInput> CustomDeductions { get; set; } = new();
    public DateTime PayrollPeriodEnd { get; set; }
}

public class DeductionInput
{
    public int DeductionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PayrollComputation
{
    public LegalParameter Param { get; set; } = null!;
    public decimal BaseSalary { get; set; }
    public decimal TransportAllowance { get; set; }
    public decimal GrossIncome { get; set; }
    public decimal Ibc { get; set; }
    public decimal CappedIbc { get; set; }

    public decimal EmployeeHealth { get; set; }
    public decimal EmployeePension { get; set; }
    public decimal SolidarityFund { get; set; }

    public decimal EmployerHealth { get; set; }
    public decimal EmployerPension { get; set; }
    public decimal Arl { get; set; }
    public decimal CompensationFund { get; set; }
    public decimal Sena { get; set; }
    public decimal Icbf { get; set; }
    public decimal TotalEmployerContributions { get; set; }

    public decimal PrimaProvision { get; set; }
    public decimal CesantiasProvision { get; set; }
    public decimal CesantiasInterestProvision { get; set; }
    public decimal VacationProvision { get; set; }
    public decimal TotalProvisions { get; set; }

    public decimal WithholdingTax { get; set; }

    public decimal TotalEmployeeDeductions { get; set; }
    public decimal NetPay { get; set; }
}
