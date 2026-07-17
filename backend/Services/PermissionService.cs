using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

/// <summary>
/// Catálogo maestro de permisos y matriz sembrada por defecto.
/// Cada rol tiene permisos pre-asignados, editables desde la UI admin.
/// Convención de clave: "modulo.accion".
/// </summary>
public class PermissionService
{
    private readonly AppDbContext _db;

    public PermissionService(AppDbContext db) => _db = db;

    /// <summary>Catálogo completo de permisos del sistema (inmutable, sembrado en migration 006).</summary>
    public static readonly IReadOnlyList<Permission> Catalog = new List<Permission>
    {
        // ──── Usuarios y seguridad ────
        new() { Key = "users.view",       Module = "users",       Action = "view",     Description = "Ver listado de usuarios del sistema" },
        new() { Key = "users.create",     Module = "users",       Action = "create",   Description = "Crear nuevos usuarios" },
        new() { Key = "users.edit",       Module = "users",       Action = "edit",     Description = "Editar usuarios existentes" },
        new() { Key = "users.delete",     Module = "users",       Action = "delete",   Description = "Desactivar usuarios" },
        new() { Key = "users.permissions",Module = "users",       Action = "permissions", Description = "Configurar matriz de permisos por rol" },

        // ──── Empresa ────
        new() { Key = "company.view",     Module = "company",     Action = "view",     Description = "Ver configuración de la empresa" },
        new() { Key = "company.edit",     Module = "company",     Action = "edit",     Description = "Editar datos de la empresa" },

        // ──── Productos y categorías ────
        new() { Key = "products.view",    Module = "products",    Action = "view",     Description = "Ver productos" },
        new() { Key = "products.create",  Module = "products",    Action = "create",   Description = "Crear productos" },
        new() { Key = "products.edit",    Module = "products",    Action = "edit",     Description = "Editar productos" },
        new() { Key = "products.delete",  Module = "products",    Action = "delete",   Description = "Eliminar o desactivar productos" },
        new() { Key = "categories.delete",Module = "categories",  Action = "delete",   Description = "Eliminar categorías" },
        new() { Key = "categories.view",  Module = "categories",  Action = "view",     Description = "Ver categorías" },
        new() { Key = "categories.manage",Module = "categories",  Action = "manage",   Description = "Crear y editar categorías" },

        // ──── Inventario ────
        new() { Key = "inventory.view",   Module = "inventory",   Action = "view",     Description = "Ver movimientos y kardex" },
        new() { Key = "inventory.adjust", Module = "inventory",   Action = "adjust",   Description = "Realizar ajustes manuales de inventario" },

        // ──── Terceros ────
        new() { Key = "suppliers.view",   Module = "suppliers",   Action = "view",     Description = "Ver proveedores" },
        new() { Key = "suppliers.manage", Module = "suppliers",   Action = "manage",   Description = "Crear y editar proveedores" },
        new() { Key = "suppliers.delete", Module = "suppliers",   Action = "delete",   Description = "Eliminar o desactivar proveedores" },
        new() { Key = "customers.view",   Module = "customers",   Action = "view",     Description = "Ver clientes" },
        new() { Key = "customers.manage", Module = "customers",   Action = "manage",   Description = "Crear y editar clientes" },
        new() { Key = "customers.delete", Module = "customers",   Action = "delete",   Description = "Eliminar o desactivar clientes" },

        // ──── Compras ────
        new() { Key = "purchases.view",   Module = "purchases",   Action = "view",     Description = "Ver compras registradas" },
        new() { Key = "purchases.create", Module = "purchases",   Action = "create",   Description = "Registrar compras" },
        new() { Key = "purchases.edit",   Module = "purchases",   Action = "edit",     Description = "Editar compras" },
        new() { Key = "purchases.delete", Module = "purchases",   Action = "delete",   Description = "Eliminar o anular compras" },

        // ──── Ventas y facturación electrónica ────
        new() { Key = "sales.view",       Module = "sales",       Action = "view",     Description = "Ver ventas" },
        new() { Key = "sales.create",     Module = "sales",       Action = "create",   Description = "Registrar ventas" },
        new() { Key = "sales.edit",       Module = "sales",       Action = "edit",     Description = "Editar ventas" },
        new() { Key = "sales.delete",     Module = "sales",       Action = "delete",   Description = "Eliminar o anular ventas" },
        new() { Key = "sales.emit_invoice",Module = "sales",       Action = "emit_invoice", Description = "Emitir factura electrónica ante la DIAN" },

        // ──── E-commerce ────
        new() { Key = "catalog.manage",  Module = "catalog",    Action = "manage",   Description = "Administrar galería de productos públicos" },
        new() { Key = "orders.view",     Module = "orders",     Action = "view",     Description = "Ver pedidos del e-commerce" },
        new() { Key = "orders.manage",   Module = "orders",     Action = "manage",   Description = "Actualizar estado de pedidos" },

        // ──── Contabilidad ────
        new() { Key = "accounting.view",  Module = "accounting",  Action = "view",     Description = "Ver libro diario y cuentas" },
        new() { Key = "accounting.manage",Module = "accounting",  Action = "manage",   Description = "Crear asientos manuales y cuentas PUC" },
        new() { Key = "accounting.period_close",Module = "accounting",Action = "period_close",Description = "Cerrar periodos contables" },
        new() { Key = "reports.view",     Module = "reports",     Action = "view",     Description = "Ver reportes financieros y fiscales" },

        // ──── Suscripción ────
        new() { Key = "subscription.manage",Module = "subscription", Action = "manage", Description = "Gestionar estado de suscripción (activar/inactivar admin)" },

        // ──── Pasarelas de pago ────
        new() { Key = "payments.manage", Module = "payments", Action = "manage", Description = "Configurar pasarelas de pago" },

        // ──── Nómina ────
        new() { Key = "payroll.view",     Module = "payroll",     Action = "view",     Description = "Ver nóminas" },
        new() { Key = "payroll.manage",   Module = "payroll",     Action = "manage",   Description = "Crear y editar nóminas" },
        new() { Key = "payroll.approve",  Module = "payroll",     Action = "approve",  Description = "Aprobar nóminas para pago" },
        new() { Key = "payroll.settlement",Module = "payroll",    Action = "settlement", Description = "Liquidar empleados (prestaciones sociales)" },
        new() { Key = "social_security.view",Module = "social_security",Action="view",  Description = "Ver pagos de seguridad social" },
        new() { Key = "social_security.pay",Module = "social_security",Action = "pay",   Description = "Registrar pagos de seguridad social" },
        new() { Key = "social_security.pila",Module = "social_security",Action = "pila", Description = "Generar archivo plano PILA" },
        new() { Key = "legal_params.manage",Module = "legal_params",Action = "manage", Description = "Editar parámetros legales (UVT, SMLMV, etc.)" },
    };

    /// <summary>
    /// Matriz por defecto sembrada en la primera ejecución.
    /// El Administrador recibe TODO; los demás roles se ciñen al principio
    /// de separación de funciones (Art. 45 Ley 222/1995).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultMatrix = new Dictionary<string, IReadOnlyList<string>>
    {
        ["Administrador"] = Catalog.Select(p => p.Key).ToList(),

        ["Contador"] = new[]
        {
            "company.view",
            "customers.view",
            "suppliers.view",
            "purchases.view",
            "sales.view",
            "sales.emit_invoice",
            "accounting.view", "accounting.manage", "accounting.period_close",
            "reports.view",
            "payroll.view", "payroll.manage", "payroll.approve", "payroll.settlement",
            "social_security.view", "social_security.pay", "social_security.pila",
            "legal_params.manage",
            "orders.view", "orders.manage",
        },

        ["Vendedor"] = new[]
        {
            "company.view",
            "products.view",
            "categories.view",
            "inventory.view",
            "customers.view", "customers.manage",
            "sales.view", "sales.create", "sales.edit", "sales.emit_invoice",
            "reports.view",
            "orders.view",
        },

        ["Almacen"] = new[]
        {
            "company.view",
            "products.view", "products.create", "products.edit",
            "categories.view", "categories.manage",
            "inventory.view", "inventory.adjust",
            "suppliers.view", "suppliers.manage",
            "purchases.view", "purchases.create", "purchases.edit",
            "catalog.manage",
        },
    };

    /// <summary>Lista los permisos actualmente asignados al rol (vacío si no hay).</summary>
    public async Task<List<string>> GetPermissionsForRoleAsync(string role)
    {
        return await _db.RolePermissions
            .Where(r => r.Role == role)
            .Select(r => r.PermissionKey)
            .ToListAsync();
    }

    /// <summary>Reemplaza toda la matriz de un rol.</summary>
    public async Task SetPermissionsForRoleAsync(string role, IEnumerable<string> keys)
    {
        var distinctKeys = keys.Distinct().ToList();
        // Valida que cada Key exista en el catálogo
        var validKeys = Catalog.Select(p => p.Key).ToHashSet();
        foreach (var k in distinctKeys)
            if (!validKeys.Contains(k))
                throw new ArgumentException($"Permiso desconocido: {k}");

        var existing = await _db.RolePermissions.Where(r => r.Role == role).ToListAsync();
        _db.RolePermissions.RemoveRange(existing);
        foreach (var key in distinctKeys)
            _db.RolePermissions.Add(new RolePermission { Role = role, PermissionKey = key });
        await _db.SaveChangesAsync();
    }
}
