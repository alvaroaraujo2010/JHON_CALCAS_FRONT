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
public class CustomersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll([FromQuery] string? search)
    {
        var q = db.Customers.Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(c => c.Name.Contains(search));
        return Ok(await q.OrderBy(c => c.Name).Select(c => ToDto(c)).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> Get(int id)
    {
        var c = await db.Customers.FindAsync(id);
        if (c == null) return NotFound();
        return Ok(ToDto(c));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,Vendedor")]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CustomerRequest req)
    {
        var c = new Customer
        {
            Name = req.Name, TaxId = req.TaxId, ContactName = req.ContactName,
            Phone = req.Phone, Email = req.Email, Address = req.Address, IsActive = req.IsActive
        };
        db.Customers.Add(c);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = c.Id }, ToDto(c));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador,Vendedor")]
    public async Task<ActionResult<CustomerDto>> Update(int id, [FromBody] CustomerRequest req)
    {
        var c = await db.Customers.FindAsync(id);
        if (c == null) return NotFound();
        c.Name = req.Name; c.TaxId = req.TaxId; c.ContactName = req.ContactName;
        c.Phone = req.Phone; c.Email = req.Email; c.Address = req.Address; c.IsActive = req.IsActive;
        await db.SaveChangesAsync();
        return Ok(ToDto(c));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Customers.FindAsync(id);
        if (c == null) return NotFound();
        c.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static CustomerDto ToDto(Customer c) =>
        new(c.Id, c.Name, c.TaxId, c.ContactName, c.Phone, c.Email, c.Address, c.IsActive);
}
