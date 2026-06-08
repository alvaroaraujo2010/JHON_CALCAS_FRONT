using ContaNexo.API.Data;
using ContaNexo.API.DTOs;
using ContaNexo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompanyController(AppDbContext db) : ControllerBase
{
    [HttpGet("public")]
    public async Task<ActionResult<CompanyDto>> GetPublic()
    {
        var c = await db.CompanySettings.FirstOrDefaultAsync();
        if (c == null) return NotFound();
        return Ok(ToDto(c));
    }

    [HttpGet]
    [Authorize(Policy = "company.view")]
    public async Task<ActionResult<CompanyDto>> Get() => await GetPublic();

    [HttpPut]
    [Authorize(Policy = "company.edit")]
    public async Task<ActionResult<CompanyDto>> Update([FromBody] UpdateCompanyRequest req)
    {
        var c = await db.CompanySettings.FirstOrDefaultAsync();
        if (c == null)
        {
            c = new Models.CompanySettings();
            db.CompanySettings.Add(c);
        }
        c.BusinessName = req.BusinessName;
        c.Tagline = req.Tagline;
        c.Description = req.Description;
        c.Address = req.Address;
        c.Phone = req.Phone;
        c.Email = req.Email;
        c.Website = req.Website;
        c.LogoUrl = req.LogoUrl;
        c.TaxId = req.TaxId;
        ApplyNit(c, req.Nit, req.NitVerificationDigit);
        c.Currency = req.Currency;
        c.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(c));
    }

    /// <summary>Si viene NIT, normaliza y siempre recalcula el DV con el algoritmo oficial DIAN.</summary>
    private static void ApplyNit(Models.CompanySettings c, string? nit, string? dv)
    {
        if (string.IsNullOrWhiteSpace(nit))
        {
            c.Nit = null;
            c.NitVerificationDigit = null;
            return;
        }
        c.Nit = NitValidator.NormalizeNit(nit);
        c.NitVerificationDigit = NitValidator.CalculateDv(c.Nit);
    }

    private static CompanyDto ToDto(Models.CompanySettings c) =>
        new(c.Id, c.BusinessName, c.Tagline, c.Description, c.Address, c.Phone, c.Email, c.Website, c.LogoUrl,
            c.TaxId, c.Nit, c.NitVerificationDigit, NitValidator.Format(c.Nit, c.NitVerificationDigit), c.Currency);
}
