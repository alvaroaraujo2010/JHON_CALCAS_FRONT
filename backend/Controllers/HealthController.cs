using System.Diagnostics;
using ContaNexo.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await db.Database.CanConnectAsync();
            sw.Stop();
            return Ok(new
            {
                status = "healthy",
                database = "connected",
                responseTimeMs = sw.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "disconnected",
                responseTimeMs = sw.ElapsedMilliseconds,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
