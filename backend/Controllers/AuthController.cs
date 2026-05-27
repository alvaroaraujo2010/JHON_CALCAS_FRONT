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
public class AuthController(AppDbContext db, JwtService jwt) : ControllerBase
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
        return Ok(new LoginResponse(jwt.GenerateToken(user), user.FullName, user.Email, user.Role.ToString(), expires));
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
}
