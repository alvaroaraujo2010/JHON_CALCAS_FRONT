using ContaNexo.API.Data;
using ContaNexo.API.Models;
using ContaNexo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/legal-parameters")]
[Authorize]
public class LegalParametersController : ControllerBase
{
    private readonly LegalParameterService _svc;

    public LegalParametersController(LegalParameterService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<List<LegalParameter>>> List()
    {
        var list = await _svc.ListAllAsync();
        return Ok(list);
    }

    [HttpGet("{year}")]
    public async Task<ActionResult<LegalParameter>> GetByYear(int year)
    {
        var p = await _svc.GetByYearAsync(year);
        return Ok(p);
    }

    [HttpGet("current")]
    public async Task<ActionResult<LegalParameter>> GetCurrent()
    {
        var p = await _svc.GetByYearAsync(DateTime.UtcNow.Year);
        return Ok(p);
    }

    [HttpPut("{year}")]
    [Authorize(Policy = "legal_params.manage")]
    public async Task<ActionResult<LegalParameter>> Upsert(int year, [FromBody] LegalParameter param)
    {
        if (year != param.Year) return BadRequest(new { message = "El año de la URL no coincide con el del cuerpo" });
        var saved = await _svc.UpsertAsync(param);
        return Ok(saved);
    }
}
