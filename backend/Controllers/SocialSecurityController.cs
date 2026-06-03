using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/social-security")]
[Authorize]
public class SocialSecurityController(AppDbContext db) : ControllerBase
{
    [HttpGet("payments")]
    public async Task<ActionResult<List<object>>> GetPayments([FromQuery] string? period)
    {
        var query = db.SocialSecurityPayments.AsQueryable();
        if (!string.IsNullOrEmpty(period)) query = query.Where(p => p.Period == period);
        var list = await query.OrderByDescending(p => p.Period).ThenBy(p => p.EmployeeName).ToListAsync();
        return Ok(list.Select(ToDto));
    }

    [HttpPost("pay")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<List<object>>> Pay([FromBody] PayRequest req)
    {
        foreach (var p in req.Payments)
        {
            var existing = await db.SocialSecurityPayments.FirstOrDefaultAsync(x =>
                x.Period == p.Period && x.EmployeeId == p.EmployeeId);
            if (existing == null)
            {
                existing = new SocialSecurityPayment
                {
                    Period = p.Period,
                    EmployeeId = p.EmployeeId,
                    EmployeeName = p.EmployeeName ?? string.Empty,
                    TaxId = p.TaxId,
                    BaseSalary = p.BaseSalary,
                    TransportAllowance = p.TransportAllowance ?? 0,
                    Ibc = p.Ibc ?? 0,
                    CapIbc = p.CapIbc ?? 0,
                    EmployeeHealthContribution = p.EmployeeHealthContribution,
                    EmployeePensionContribution = p.EmployeePensionContribution,
                    SolidarityFundContribution = p.SolidarityFundContribution ?? 0,
                    EmployerHealthContribution = p.EmployerHealthContribution,
                    EmployerPensionContribution = p.EmployerPensionContribution,
                    ArlContribution = p.ArlContribution,
                    CompensationFundContribution = p.CompensationFundContribution,
                    SenaContribution = p.SenaContribution,
                    IcbfContribution = p.IcbfContribution,
                    EmployeeContributionTotal = p.EmployeeContributionTotal,
                    EmployerContributionTotal = p.EmployerContributionTotal,
                    ContributionAmount = p.ContributionAmount,
                    PaymentDate = p.PaymentDate,
                    Status = "paid",
                    Reference = p.Reference,
                    Operator = p.Operator
                };
                db.SocialSecurityPayments.Add(existing);
            }
            else
            {
                existing.Status = "paid";
                existing.PaymentDate = p.PaymentDate;
                existing.Reference = p.Reference;
            }
        }
        await db.SaveChangesAsync();
        var updated = await db.SocialSecurityPayments.OrderByDescending(p => p.Period).ToListAsync();
        return Ok(updated.Select(ToDto));
    }

    private static object ToDto(SocialSecurityPayment p) => new
    {
        p.Id, p.Period, p.EmployeeId, p.EmployeeName, p.TaxId,
        p.BaseSalary, p.TransportAllowance, p.Ibc, p.CapIbc,
        p.EmployeeHealthContribution, p.EmployeePensionContribution, p.SolidarityFundContribution,
        p.EmployerHealthContribution, p.EmployerPensionContribution,
        p.ArlContribution, p.CompensationFundContribution, p.SenaContribution, p.IcbfContribution,
        p.EmployeeContributionTotal, p.EmployerContributionTotal,
        p.ContributionRate, p.ContributionAmount,
        p.PaymentDate, p.Status, p.Reference, p.Operator,
        p.CreatedAt
    };
}

public record PayRequest(List<PayPaymentDto> Payments);
public record PayPaymentDto(
    string Period, int EmployeeId, string? EmployeeName, string? TaxId,
    decimal BaseSalary, decimal? TransportAllowance, decimal? Ibc, decimal? CapIbc,
    decimal EmployeeHealthContribution, decimal EmployeePensionContribution, decimal? SolidarityFundContribution,
    decimal EmployerHealthContribution, decimal EmployerPensionContribution,
    decimal ArlContribution, decimal CompensationFundContribution,
    decimal SenaContribution, decimal IcbfContribution,
    decimal EmployeeContributionTotal, decimal EmployerContributionTotal,
    decimal ContributionAmount, string? PaymentDate, string? Reference, string? Operator);
