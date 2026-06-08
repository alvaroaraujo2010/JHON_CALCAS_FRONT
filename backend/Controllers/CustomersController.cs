using ContaNexo.API.Data;
using ContaNexo.API.DTOs;
using ContaNexo.API.Models;
using ContaNexo.API.Services;
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
    [Authorize(Policy = "customers.manage")]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CustomerRequest req)
    {
        var c = new Customer
        {
            Name = req.Name, TaxId = req.TaxId, ContactName = req.ContactName,
            Phone = req.Phone, Email = req.Email, Address = req.Address, IsActive = req.IsActive,
            IsRetentionAgent = req.IsRetentionAgent
        };
        ApplyNit(c, req.Nit, req.NitVerificationDigit);
        db.Customers.Add(c);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = c.Id }, ToDto(c));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "customers.manage")]
    public async Task<ActionResult<CustomerDto>> Update(int id, [FromBody] CustomerRequest req)
    {
        var c = await db.Customers.FindAsync(id);
        if (c == null) return NotFound();
        c.Name = req.Name; c.TaxId = req.TaxId; c.ContactName = req.ContactName;
        c.Phone = req.Phone; c.Email = req.Email; c.Address = req.Address;
        c.IsActive = req.IsActive; c.IsRetentionAgent = req.IsRetentionAgent;
        ApplyNit(c, req.Nit, req.NitVerificationDigit);
        await db.SaveChangesAsync();
        return Ok(ToDto(c));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "customers.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Customers.FindAsync(id);
        if (c == null) return NotFound();
        c.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static void ApplyNit(Customer c, string? nit, string? dv)
    {
        if (string.IsNullOrWhiteSpace(nit))
        {
            c.Nit = null;
            c.NitVerificationDigit = null;
            return;
        }
        c.Nit = NitValidator.NormalizeNit(nit);
        // Siempre recalcular el DV con el algoritmo oficial DIAN; el provisto se ignora
        // para garantizar integridad contable.
        c.NitVerificationDigit = NitValidator.CalculateDv(c.Nit);
    }

    private static CustomerDto ToDto(Customer c) =>
        new(c.Id, c.Name, c.TaxId, c.Nit, c.NitVerificationDigit,
            NitValidator.Format(c.Nit, c.NitVerificationDigit),
            c.ContactName, c.Phone, c.Email, c.Address, c.IsActive, c.IsRetentionAgent);
}
