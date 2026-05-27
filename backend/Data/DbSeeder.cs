using System.Data;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await EnsureElectronicInvoiceColumnsAsync(db);

        var adminHash = BCrypt.Net.BCrypt.HashPassword("ingAlv4r0");
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Administrador);
        if (admin == null)
        {
            db.Users.Add(new User
            {
                FullName = "Administrador",
                Email = "administrador@contanexo.com",
                PasswordHash = adminHash,
                Role = UserRole.Administrador
            });
        }
        else
        {
            admin.FullName = "Administrador";
            admin.Email = "administrador@contanexo.com";
            admin.PasswordHash = adminHash;
            admin.IsActive = true;
        }

        if (!await db.CompanySettings.AnyAsync())
        {
            db.CompanySettings.Add(new CompanySettings
            {
                BusinessName = "ContaNexo",
                Tagline = "El nexo entre su contabilidad e inventario",
                Description = "Plataforma empresarial para gestionar contabilidad, inventario, ventas y compras de su negocio en un solo lugar.",
                Address = "Calle Principal #100, Centro Empresarial",
                Phone = "+57 300 000 0000",
                Email = "contacto@contanexo.com",
                Website = "https://www.contanexo.com",
                TaxId = "900.000.000-1"
            });
        }

        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Name = "General", Description = "Productos generales" },
                new Category { Name = "Insumos", Description = "Materias primas e insumos" },
                new Category { Name = "Mercancía", Description = "Productos para reventa" }
            );
        }

        if (!await db.Accounts.AnyAsync())
        {
            var accounts = new List<Account>
            {
                new() { Code = "1", Name = "Activo", Type = AccountType.Activo },
                new() { Code = "11", Name = "Disponible", Type = AccountType.Activo },
                new() { Code = "1105", Name = "Caja", Type = AccountType.Activo },
                new() { Code = "1110", Name = "Bancos", Type = AccountType.Activo },
                new() { Code = "14", Name = "Inventarios", Type = AccountType.Activo },
                new() { Code = "1435", Name = "Mercancías no fabricadas", Type = AccountType.Activo },
                new() { Code = "2", Name = "Pasivo", Type = AccountType.Pasivo },
                new() { Code = "22", Name = "Proveedores", Type = AccountType.Pasivo },
                new() { Code = "2205", Name = "Proveedores nacionales", Type = AccountType.Pasivo },
                new() { Code = "3", Name = "Patrimonio", Type = AccountType.Patrimonio },
                new() { Code = "31", Name = "Capital social", Type = AccountType.Patrimonio },
                new() { Code = "4", Name = "Ingresos", Type = AccountType.Ingreso },
                new() { Code = "41", Name = "Operacionales", Type = AccountType.Ingreso },
                new() { Code = "4135", Name = "Comercio al por mayor y menor", Type = AccountType.Ingreso },
                new() { Code = "5", Name = "Gastos", Type = AccountType.Gasto },
                new() { Code = "61", Name = "Costo de ventas", Type = AccountType.Gasto },
                new() { Code = "6135", Name = "Comercio al por mayor y menor", Type = AccountType.Gasto }
            };
            db.Accounts.AddRange(accounts);
        }

        await db.SaveChangesAsync();

        if (!await db.Products.AnyAsync())
        {
            var cat = await db.Categories.FirstAsync();
            db.Products.AddRange(
                new Product { Sku = "PRD-001", Name = "Producto demo A", CategoryId = cat.Id, UnitCost = 5000, UnitPrice = 8500, Stock = 50, MinStock = 10 },
                new Product { Sku = "PRD-002", Name = "Producto demo B", CategoryId = cat.Id, UnitCost = 12000, UnitPrice = 18900, Stock = 25, MinStock = 5 },
                new Product { Sku = "PRD-003", Name = "Producto demo C", CategoryId = cat.Id, UnitCost = 3000, UnitPrice = 5500, Stock = 8, MinStock = 10 }
            );
        }

        if (!await db.Suppliers.AnyAsync())
        {
            db.Suppliers.Add(new Supplier
            {
                Name = "Proveedor Demo S.A.S.",
                TaxId = "800.111.222-3",
                ContactName = "Juan Pérez",
                Phone = "+57 310 111 2222",
                Email = "ventas@proveedordemo.com"
            });
        }

        if (!await db.Customers.AnyAsync())
        {
            db.Customers.Add(new Customer
            {
                Name = "Cliente Demo Ltda.",
                TaxId = "900.333.444-5",
                ContactName = "María García",
                Phone = "+57 320 333 4444",
                Email = "compras@clientedemo.com"
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Bases creadas antes de factura electronica: agrega columnas solo si faltan (sin errores en consola).
    /// </summary>
    private static async Task EnsureElectronicInvoiceColumnsAsync(AppDbContext db)
    {
        if (!await ColumnExistsAsync(db, "Sales", "ElectronicInvoiceStatus"))
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE Sales ADD COLUMN ElectronicInvoiceStatus VARCHAR(20) NOT NULL DEFAULT 'Borrador'");

        if (!await ColumnExistsAsync(db, "Sales", "ElectronicInvoiceNumber"))
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE Sales ADD COLUMN ElectronicInvoiceNumber VARCHAR(50) NULL");

        if (!await ColumnExistsAsync(db, "Sales", "Cufe"))
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE Sales ADD COLUMN Cufe VARCHAR(128) NULL");

        if (!await ColumnExistsAsync(db, "Sales", "ElectronicInvoiceIssuedAt"))
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE Sales ADD COLUMN ElectronicInvoiceIssuedAt DATETIME(6) NULL");
    }

    private static async Task<bool> ColumnExistsAsync(AppDbContext db, string table, string column)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = @table
                  AND COLUMN_NAME = @column
                """;

            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@table";
            tableParam.Value = table;
            command.Parameters.Add(tableParam);

            var columnParam = command.CreateParameter();
            columnParam.ParameterName = "@column";
            columnParam.Value = column;
            command.Parameters.Add(columnParam);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }
        finally
        {
            if (openedHere) await connection.CloseAsync();
        }
    }
}
