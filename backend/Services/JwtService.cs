using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ContaNexo.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace ContaNexo.API.Services;

public class JwtService(IConfiguration config, PermissionService permissions)
{
    /// <summary>Tipo de claim usado para cada permiso del usuario en el JWT.</summary>
    public const string PermissionClaimType = "permission";

    public async Task<string> GenerateTokenAsync(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(double.Parse(config["Jwt:ExpireHours"] ?? "8"));

        var claimList = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        // Permisos del rol (uno por claim para que RequireClaim los acepte)
        var perms = await permissions.GetPermissionsForRoleAsync(user.Role.ToString());
        foreach (var p in perms)
            claimList.Add(new Claim(PermissionClaimType, p));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claimList,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
