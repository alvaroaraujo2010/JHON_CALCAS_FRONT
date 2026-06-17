using ContaNexo.API.Data;
using ContaNexo.API.DTOs;
using ContaNexo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        var sub = await db.Subscriptions.AsNoTracking().FirstOrDefaultAsync();
        if (sub == null) return NotFound();
        return Ok(new SubscriptionDto(sub.IsActive, sub.MonthlyFee, sub.DueDate, sub.UpdatedAt));
    }

    [HttpPut]
    [Authorize(Policy = "subscription.manage")]
    public async Task<IActionResult> Update([FromBody] UpdateSubscriptionRequest req)
    {
        var sub = await db.Subscriptions.FirstOrDefaultAsync();
        if (sub == null) return NotFound();
        sub.IsActive = req.IsActive;
        sub.MonthlyFee = req.MonthlyFee;
        sub.DueDate = req.DueDate;
        sub.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new SubscriptionDto(sub.IsActive, sub.MonthlyFee, sub.DueDate, sub.UpdatedAt));
    }
}
