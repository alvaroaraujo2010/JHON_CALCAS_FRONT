using ContaNexo.API.Data;
using ContaNexo.API.DTOs;
using ContaNexo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SupplierDto>>> GetAll([FromQuery] string? search)
    {
        var q = db.Suppliers.Where(s => s.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(s => s.Name.Contains(search));
        return Ok(await q.OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierDto>> Get(int id)
    {
        var s = await db.Suppliers.FindAsync(id);
        if (s == null) return NotFound();
        return Ok(ToDto(s));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,Almacen")]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] SupplierRequest req)
    {
        var s = Map(req);
        db.Suppliers.Add(s);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = s.Id }, ToDto(s));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador,Almacen")]
    public async Task<ActionResult<SupplierDto>> Update(int id, [FromBody] SupplierRequest req)
    {
        var s = await db.Suppliers.FindAsync(id);
        if (s == null) return NotFound();
        Apply(s, req);
        await db.SaveChangesAsync();
        return Ok(ToDto(s));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await db.Suppliers.FindAsync(id);
        if (s == null) return NotFound();
        s.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static Supplier Map(SupplierRequest r) => new()
    {
        Name = r.Name, TaxId = r.TaxId, ContactName = r.ContactName,
        Phone = r.Phone, Email = r.Email, Address = r.Address, IsActive = r.IsActive
    };

    private static void Apply(Supplier s, SupplierRequest r)
    {
        s.Name = r.Name; s.TaxId = r.TaxId; s.ContactName = r.ContactName;
        s.Phone = r.Phone; s.Email = r.Email; s.Address = r.Address; s.IsActive = r.IsActive;
    }

    private static SupplierDto ToDto(Supplier s) =>
        new(s.Id, s.Name, s.TaxId, s.ContactName, s.Phone, s.Email, s.Address, s.IsActive);
}
