using ContaNexo.API.Models;
using ContaNexo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController(PermissionService svc) : ControllerBase
{
    public record PermissionDto(string Key, string Module, string Action, string Description);

    public record RoleMatrixDto(string Role, List<string> Keys);

    public record UpdateMatrixRequest(string Role, List<string> Keys);

    /// <summary>Catálogo completo de permisos disponibles (solo lectura).</summary>
    [HttpGet("catalog")]
    public ActionResult<List<PermissionDto>> GetCatalog()
    {
        return Ok(PermissionService.Catalog
            .Select(p => new PermissionDto(p.Key, p.Module, p.Action, p.Description))
            .ToList());
    }

    /// <summary>Lista los roles predefinidos del sistema.</summary>
    [HttpGet("roles")]
    public ActionResult<List<string>> GetRoles()
    {
        return Ok(Enum.GetNames<UserRole>());
    }

    /// <summary>Devuelve la matriz completa: { role: [keys] }.</summary>
    [HttpGet("matrix")]
    public async Task<ActionResult<Dictionary<string, List<string>>>> GetMatrix()
    {
        var roles = Enum.GetNames<UserRole>();
        var result = new Dictionary<string, List<string>>();
        foreach (var role in roles)
            result[role] = await svc.GetPermissionsForRoleAsync(role);
        return Ok(result);
    }

    /// <summary>Reemplaza la matriz de un rol. Solo Administrador.</summary>
    [HttpPut("matrix")]
    [Authorize(Policy = "users.permissions")]
    public async Task<IActionResult> UpdateMatrix([FromBody] UpdateMatrixRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Role))
            return BadRequest(new { message = "Rol requerido" });
        if (!Enum.TryParse<UserRole>(req.Role, out _))
            return BadRequest(new { message = $"Rol desconocido: {req.Role}" });

        await svc.SetPermissionsForRoleAsync(req.Role, req.Keys);
        return Ok(new { message = $"Matriz del rol {req.Role} actualizada ({req.Keys.Count} permisos)" });
    }
}
