using ContaNexo.API.Data;
using ContaNexo.API.DTOs;
using ContaNexo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "users.view")]
public class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var list = await db.Users.OrderBy(u => u.FullName).ToListAsync();
        return Ok(list.Select(u => new UserDto(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive)).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { message = "Email ya registrado" });
        if (!Enum.TryParse<UserRole>(req.Role, true, out var role))
            return BadRequest(new { message = "Rol inválido" });

        var user = new User
        {
            FullName = req.FullName, Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password), Role = role
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.IsActive));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserRequest req)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (await db.Users.AnyAsync(u => u.Email == req.Email && u.Id != id))
            return BadRequest(new { message = "Email ya registrado" });
        if (!Enum.TryParse<UserRole>(req.Role, true, out var role))
            return BadRequest(new { message = "Rol inválido" });

        user.FullName = req.FullName;
        user.Email = req.Email;
        user.Role = role;
        user.IsActive = req.IsActive;
        if (!string.IsNullOrWhiteSpace(req.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        await db.SaveChangesAsync();
        return Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.IsActive));
    }

    [HttpPut("{id}/toggle-active")]
    public async Task<ActionResult<UserDto>> ToggleActive(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync();
        return Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.IsActive));
    }

    [HttpPut("{id}/reset-password")]
    public async Task<ActionResult<UserDto>> ResetPassword(int id, [FromBody] ResetPasswordRequest req)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (req.NewPassword.Length < 6)
            return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();
        return Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.IsActive));
    }
}
