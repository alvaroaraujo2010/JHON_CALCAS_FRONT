using System.Security.Claims;
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
public class AuthController(AppDbContext db, JwtService jwt, PermissionService permissions) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var login = request.Username.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.IsActive &&
            (u.Email.ToLower() == login || u.Email.ToLower().StartsWith(login + "@")));
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciales inválidas" });

        var expires = DateTime.UtcNow.AddHours(8);
        var token = await jwt.GenerateTokenAsync(user);
        var perms = await permissions.GetPermissionsForRoleAsync(user.Role.ToString());
        return Ok(new LoginResponse(token, user.FullName, user.Email, user.Role.ToString(), perms, expires));
    }

    [HttpGet("permissions")]
    [Authorize]
    public async Task<ActionResult<List<string>>> Permissions()
    {
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        return Ok(await permissions.GetPermissionsForRoleAsync(role));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();
        return Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.IsActive));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Contraseña actual incorrecta" });

        if (req.NewPassword.Length < 6)
            return BadRequest(new { message = "La nueva contraseña debe tener al menos 6 caracteres" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();
        return Ok(new { message = "Contraseña actualizada correctamente" });
    }
}
