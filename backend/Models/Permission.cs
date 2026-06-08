namespace ContaNexo.API.Models;

/// <summary>
/// Catálogo de permisos disponibles en el sistema.
/// Convención de clave: "modulo.accion" (minúsculas, sin espacios).
/// </summary>
public class Permission
{
    public string Key { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Relación N-N entre el rol (string del enum UserRole) y un permiso.
/// Sembrada por defecto en migration 006; editable desde /api/permissions.
/// </summary>
public class RolePermission
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string PermissionKey { get; set; } = string.Empty;
}
