using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/payment-records")]
[Authorize]
public class PaymentRecordsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<object>>> GetAll([FromQuery] int? payrollId)
    {
        var query = db.PaymentRecords.AsQueryable();
        if (payrollId.HasValue) query = query.Where(p => p.PayrollId == payrollId.Value);
        var list = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
        return Ok(list.Select(ToDto));
    }

    private static object ToDto(PaymentRecord r) => new
    {
        r.Id, r.PayrollId, r.PeriodStart, r.PeriodEnd, r.PaymentDate,
        r.TotalAmount, r.PaymentMethod, r.Reference, r.Status, r.Notes,
        r.CreatedAt
    };
}
