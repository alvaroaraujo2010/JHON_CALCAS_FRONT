using ContaNexo.API.Models;
using ContaNexo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/withholding-tax")]
[Authorize]
public class WithholdingTaxController : ControllerBase
{
    private readonly WithholdingTaxService _svc;
    private readonly LegalParameterService _legal;

    public WithholdingTaxController(WithholdingTaxService svc, LegalParameterService legal)
    {
        _svc = svc;
        _legal = legal;
    }

    [HttpGet("brackets")]
    public async Task<ActionResult<List<WithholdingTaxBracket>>> GetBrackets([FromQuery] int? year)
    {
        var y = year ?? DateTime.UtcNow.Year;
        var list = await _svc.GetTableForYearFromDbAsync(y);
        return Ok(list);
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<object>> Calculate([FromBody] WithholdingTaxRequest req)
    {
        var param = await _legal.GetByYearAsync(req.Year);
        decimal ret;
        var procedure = req.Procedure ?? (req.Procedure2 ? "2" : "1");
        if (procedure == "2")
        {
            ret = _svc.CalculateProcedureTwo(req.GrossIncome, param, req.MandatoryContributions ?? 0m);
        }
        else
        {
            var deductions = new Art387Deductions
            {
                HasDependents = req.HasDependents ?? false,
                HousingInterestEnabled = req.HousingInterestEnabled ?? false,
                PrepaidHealthEnabled = req.PrepaidHealthEnabled ?? false,
                AfcMonthlyAmount = req.AfcMonthlyAmount ?? 0m
            };
            ret = _svc.CalculateProcedureOne(req.GrossIncome, param, req.NonTaxableIncome ?? 0m, deductions);
        }
        var baseGravableUvt = (req.GrossIncome - (req.NonTaxableIncome ?? 0m)) / param.Uvt;
        return Ok(new
        {
            year = req.Year,
            procedure,
            grossIncome = req.GrossIncome,
            nonTaxableIncome = req.NonTaxableIncome ?? 0m,
            mandatoryContributions = req.MandatoryContributions ?? 0m,
            art387Applied = req.HasDependents == true || req.HousingInterestEnabled == true
                         || req.PrepaidHealthEnabled == true || (req.AfcMonthlyAmount ?? 0) > 0,
            smlmv = param.Smlmv,
            uvt = param.Uvt,
            transportAllowance = param.TransportAllowance,
            minimumWithholdingUvt = param.MinimumWithholdingUvt,
            exemptIncomeUvt = param.ExemptIncomeUvt,
            baseGravableUvt,
            applies = baseGravableUvt >= param.MinimumWithholdingUvt,
            withholdingTax = ret
        });
    }
}

public record WithholdingTaxRequest(
    int Year,
    decimal GrossIncome,
    decimal? NonTaxableIncome,
    decimal? MandatoryContributions,
    string? Procedure,
    bool Procedure2 = false,
    bool? HasDependents = null,
    bool? HousingInterestEnabled = null,
    bool? PrepaidHealthEnabled = null,
    decimal? AfcMonthlyAmount = null);
