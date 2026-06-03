using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/payroll-provisions")]
[Authorize]
public class PayrollProvisionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PayrollProvisionsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<object>>> List([FromQuery] int? year, [FromQuery] int? employeeId)
    {
        var query = _db.PayrollProvisions.AsQueryable();
        if (year.HasValue) query = query.Where(p => p.Year == year.Value);
        if (employeeId.HasValue) query = query.Where(p => p.EmployeeId == employeeId.Value);
        var list = await query.OrderBy(p => p.Year).ThenBy(p => p.Month).ThenBy(p => p.EmployeeName).ToListAsync();
        return Ok(list.Select(p => new
        {
            p.Id, p.EmployeeId, p.EmployeeName, p.Year, p.Month, p.PeriodLabel,
            p.BaseSalary, p.TransportAllowance,
            p.PrimaProvision, p.CesantiasProvision, p.CesantiasInterestProvision, p.VacationProvision,
            p.TotalProvision,
            p.AccumulatedPrima, p.AccumulatedCesantias, p.AccumulatedCesantiasInterest, p.AccumulatedVacations,
            p.AccumulatedTotal,
            p.PayrollId, p.CreatedAt
        }));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<List<object>>> Summary([FromQuery] int? year)
    {
        var y = year ?? DateTime.UtcNow.Year;
        var list = await _db.PayrollProvisions
            .Where(p => p.Year == y)
            .GroupBy(p => new { p.EmployeeId, p.EmployeeName })
            .Select(g => new
            {
                EmployeeId = g.Key.EmployeeId,
                EmployeeName = g.Key.EmployeeName,
                Year = y,
                Prima = g.Sum(x => x.AccumulatedPrima),
                Cesantias = g.Sum(x => x.AccumulatedCesantias),
                CesantiasInterest = g.Sum(x => x.AccumulatedCesantiasInterest),
                Vacaciones = g.Sum(x => x.AccumulatedVacations),
                Total = g.Sum(x => x.AccumulatedTotal)
            })
            .ToListAsync();
        return Ok(list);
    }
}
