using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeductionsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<object>>> GetAll()
    {
        var list = await db.PayrollDeductions.OrderBy(d => d.Name).ToListAsync();
        return Ok(list.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<object>> Create([FromBody] DeductionRequest req)
    {
        var ded = new PayrollDeduction
        {
            Name = req.Name,
            Type = req.Type,
            Value = req.Value,
            Description = req.Description,
            IsActive = req.IsActive
        };
        db.PayrollDeductions.Add(ded);
        await db.SaveChangesAsync();
        return Ok(ToDto(ded));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<object>> Update(int id, [FromBody] DeductionRequest req)
    {
        var ded = await db.PayrollDeductions.FindAsync(id);
        if (ded == null) return NotFound();
        ded.Name = req.Name;
        ded.Type = req.Type;
        ded.Value = req.Value;
        ded.Description = req.Description;
        ded.IsActive = req.IsActive;
        await db.SaveChangesAsync();
        return Ok(ToDto(ded));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var ded = await db.PayrollDeductions.FindAsync(id);
        if (ded == null) return NotFound();
        db.PayrollDeductions.Remove(ded);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static object ToDto(PayrollDeduction d) => new
    {
        d.Id, d.Name, d.Type, d.Value, d.Description, d.IsActive
    };
}

public record DeductionRequest(string Name, string Type, decimal Value, string? Description, bool IsActive);
