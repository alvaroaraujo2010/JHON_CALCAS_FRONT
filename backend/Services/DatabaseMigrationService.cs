using ContaNexo.API.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ContaNexo.API.Services;

/// <summary>
/// Aplica migraciones SQL ad-hoc al inicio de la aplicación, sin requerir
/// que el operador las ejecute manualmente ni migrar de EnsureCreated() a
/// Migrate(). Cada migración es idempotente: detecta si ya fue aplicada.
///
/// Las migraciones se declaran en <see cref="Migrations"/> con su número
/// de versión. Se persisten en la tabla <c>__schema_migrations</c>.
/// </summary>
public class DatabaseMigrationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseMigrationService> _log;

    public DatabaseMigrationService(AppDbContext db, ILogger<DatabaseMigrationService> log)
    {
        _db = db;
        _log = log;
    }

    public record Migration(string Version, string Description, Func<AppDbContext, Task> Apply);

    /// <summary>
    /// Registro de migraciones. Agregar aquí cada nueva migración con número
    /// incremental en el formato "NNN_descripcion_corta".
    /// </summary>
    public static readonly List<Migration> Migrations = new()
    {
        new("001_ley_2277_2022_deductions",
            "Deducciones Art. 387 ET (Ley 2277/2022): HasDependents, HousingInterestEnabled, PrepaidHealthEnabled, AfcMonthlyAmount",
            Apply001),
        new("002_customer_retention_agent",
            "Bandera IsRetentionAgent en Customers (gran contribuyente / agente retenedor) y campos Company de NIT con DV",
            Apply002),
        new("003_nit_dv_general",
            "NIT + NitVerificationDigit en Customers (DV calculado por módulo 11 DIAN)",
            Apply003),
        new("004_pila_operators",
            "PILA: CotizanteTipo/Subtipo, operadores EPS/AFP/ARL/CCF, ArlRiskClass en Employees; Novedad* y operadores en SocialSecurityPayments",
            Apply004),
        new("005_inventory_valuation",
            "Inventario: tabla inventorylots + ValuationMethod en products (PEPS/Promedio Ponderado)",
            Apply005),
        new("006_rbac_permissions",
            "RBAC: tablas permissions + role_permissions, catálogo y matriz por defecto sembrada",
            Apply006),
    };

    public async Task ApplyPendingAsync()
    {
        await EnsureMigrationTableAsync();
        var applied = await GetAppliedVersionsAsync();
        foreach (var m in Migrations)
        {
            if (applied.Contains(m.Version))
            {
                _log.LogDebug("Migración {Version} ya aplicada, se omite.", m.Version);
                continue;
            }
            try
            {
                _log.LogInformation("Aplicando migración {Version}: {Description}", m.Version, m.Description);
                await m.Apply(_db);
                await MarkAppliedAsync(m.Version);
                _log.LogInformation("Migración {Version} aplicada con éxito.", m.Version);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Falló la migración {Version}. La aplicación no continuará.", m.Version);
                throw;
            }
        }
    }

    private async Task EnsureMigrationTableAsync()
    {
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS __schema_migrations (
                version VARCHAR(64) NOT NULL PRIMARY KEY,
                description VARCHAR(255) NOT NULL,
                applied_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
    }

    private async Task<HashSet<string>> GetAppliedVersionsAsync()
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
        var set = new HashSet<string>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT version FROM __schema_migrations";
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) set.Add(r.GetString(0));
        return set;
    }

    private async Task MarkAppliedAsync(string version)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO __schema_migrations(version, description) VALUES (@v, @d)";
        var pv = cmd.CreateParameter(); pv.ParameterName = "@v"; pv.Value = version; cmd.Parameters.Add(pv);
        var pd = cmd.CreateParameter(); pd.ParameterName = "@d";
        pd.Value = Migrations.First(m => m.Version == version).Description;
        cmd.Parameters.Add(pd);
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Migraciones individuales ─────────────────────────────────────────

    /// <summary>
    /// 001 — Deducciones Art. 387 ET (Ley 2277/2022). Idempotente.
    /// </summary>
    private static async Task Apply001(AppDbContext db)
    {
        // Usar una conexión cruda para evitar parsing de EF sobre DDL.
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        var statements = new[]
        {
            ("HasDependents",          "ALTER TABLE Employees ADD COLUMN HasDependents TINYINT(1) NOT NULL DEFAULT 0 AFTER SolidarityFundOverride"),
            ("HousingInterestEnabled", "ALTER TABLE Employees ADD COLUMN HousingInterestEnabled TINYINT(1) NOT NULL DEFAULT 0 AFTER HasDependents"),
            ("PrepaidHealthEnabled",   "ALTER TABLE Employees ADD COLUMN PrepaidHealthEnabled TINYINT(1) NOT NULL DEFAULT 0 AFTER HousingInterestEnabled"),
            ("AfcMonthlyAmount",       "ALTER TABLE Employees ADD COLUMN AfcMonthlyAmount DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER PrepaidHealthEnabled"),
        };

        foreach (var (column, ddl) in statements)
        {
            await using var check = conn.CreateCommand();
            check.CommandText = @"SELECT COUNT(*) FROM information_schema.columns
                                  WHERE table_schema = DATABASE()
                                    AND table_name = 'Employees'
                                    AND column_name = @col";
            var p = check.CreateParameter();
            p.ParameterName = "@col";
            p.Value = column;
            check.Parameters.Add(p);
            var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
            if (exists) continue;

            await using var alter = conn.CreateCommand();
            alter.CommandText = ddl;
            await alter.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 002 — Customer.IsRetentionAgent y campos Company/Supplier de NIT con DV.
    /// Idempotente y tolerante: si la tabla no existe (caso de BD inconsistente),
    /// registra warning y continúa con las demás.
    /// </summary>
    private static async Task Apply002(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        var statements = new (string Table, string Column, string DDL)[]
        {
            ("customers", "IsRetentionAgent",
                "ALTER TABLE customers ADD COLUMN IsRetentionAgent TINYINT(1) NOT NULL DEFAULT 0 AFTER IsActive"),
            ("companysettings", "Nit",
                "ALTER TABLE companysettings ADD COLUMN Nit VARCHAR(20) NULL AFTER TaxId"),
            ("companysettings", "NitVerificationDigit",
                "ALTER TABLE companysettings ADD COLUMN NitVerificationDigit VARCHAR(1) NULL AFTER Nit"),
            ("suppliers", "Nit",
                "ALTER TABLE suppliers ADD COLUMN Nit VARCHAR(20) NULL AFTER TaxId"),
            ("suppliers", "NitVerificationDigit",
                "ALTER TABLE suppliers ADD COLUMN NitVerificationDigit VARCHAR(1) NULL AFTER Nit"),
        };

        foreach (var (table, column, ddl) in statements)
        {
            // Verifica primero que la tabla exista.
            await using var tblCheck = conn.CreateCommand();
            tblCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                     WHERE table_schema = DATABASE() AND table_name = @tbl";
            var pt = tblCheck.CreateParameter(); pt.ParameterName = "@tbl"; pt.Value = table; tblCheck.Parameters.Add(pt);
            var tblExists = Convert.ToInt32(await tblCheck.ExecuteScalarAsync()) > 0;
            if (!tblExists)
            {
                // La tabla no existe en esta BD. La creación corre por EnsureCreated();
                // saltamos sin error.
                continue;
            }

            await using var check = conn.CreateCommand();
            check.CommandText = @"SELECT COUNT(*) FROM information_schema.columns
                                  WHERE table_schema = DATABASE()
                                    AND table_name = @tbl
                                    AND column_name = @col";
            var pc = check.CreateParameter(); pc.ParameterName = "@tbl"; pc.Value = table; check.Parameters.Add(pc);
            var pcol = check.CreateParameter(); pcol.ParameterName = "@col"; pcol.Value = column; check.Parameters.Add(pcol);
            var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
            if (exists) continue;

            await using var alter = conn.CreateCommand();
            alter.CommandText = ddl;
            await alter.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 003 — Campos Nit/NitVerificationDigit en Customers (los de Company/Supplier
    /// ya están en 002). Idempotente.
    /// </summary>
    private static async Task Apply003(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        var statements = new (string Table, string Column, string DDL)[]
        {
            ("customers", "Nit",
                "ALTER TABLE customers ADD COLUMN Nit VARCHAR(20) NULL AFTER TaxId"),
            ("customers", "NitVerificationDigit",
                "ALTER TABLE customers ADD COLUMN NitVerificationDigit VARCHAR(1) NULL AFTER Nit"),
        };

        foreach (var (table, column, ddl) in statements)
        {
            await using var tblCheck = conn.CreateCommand();
            tblCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                     WHERE table_schema = DATABASE() AND table_name = @tbl";
            var pt = tblCheck.CreateParameter(); pt.ParameterName = "@tbl"; pt.Value = table; tblCheck.Parameters.Add(pt);
            if (Convert.ToInt32(await tblCheck.ExecuteScalarAsync()) == 0) continue;

            await using var check = conn.CreateCommand();
            check.CommandText = @"SELECT COUNT(*) FROM information_schema.columns
                                  WHERE table_schema = DATABASE()
                                    AND table_name = @tbl
                                    AND column_name = @col";
            var pc = check.CreateParameter(); pc.ParameterName = "@tbl"; pc.Value = table; check.Parameters.Add(pc);
            var pcol = check.CreateParameter(); pcol.ParameterName = "@col"; pcol.Value = column; check.Parameters.Add(pcol);
            if (Convert.ToInt32(await check.ExecuteScalarAsync()) > 0) continue;

            await using var alter = conn.CreateCommand();
            alter.CommandText = ddl;
            await alter.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 004 — PILA: tipo/subtipo cotizante, operadores EPS/AFP/ARL/CCF, riesgo ARL en Employees.
    /// Novedad* y operadores en SocialSecurityPayments. Idempotente.
    /// </summary>
    private static async Task Apply004(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        var statements = new (string Table, string Column, string DDL)[]
        {
            ("employees", "CotizanteTipo",        "ALTER TABLE employees ADD COLUMN CotizanteTipo VARCHAR(2) NOT NULL DEFAULT '01' AFTER AfcMonthlyAmount"),
            ("employees", "CotizanteSubtipo",     "ALTER TABLE employees ADD COLUMN CotizanteSubtipo VARCHAR(2) NOT NULL DEFAULT '00' AFTER CotizanteTipo"),
            ("employees", "OperatorEps",          "ALTER TABLE employees ADD COLUMN OperatorEps VARCHAR(10) NULL AFTER CotizanteSubtipo"),
            ("employees", "OperatorPension",      "ALTER TABLE employees ADD COLUMN OperatorPension VARCHAR(10) NULL AFTER OperatorEps"),
            ("employees", "OperatorArl",          "ALTER TABLE employees ADD COLUMN OperatorArl VARCHAR(10) NULL AFTER OperatorPension"),
            ("employees", "OperatorCcf",          "ALTER TABLE employees ADD COLUMN OperatorCcf VARCHAR(10) NULL AFTER OperatorArl"),
            ("employees", "ArlRiskClass",         "ALTER TABLE employees ADD COLUMN ArlRiskClass INT NOT NULL DEFAULT 1 AFTER OperatorCcf"),
            ("socialsecuritypayments", "NovedadTipo",         "ALTER TABLE socialsecuritypayments ADD COLUMN NovedadTipo VARCHAR(2) NOT NULL DEFAULT 'N' AFTER Reference"),
            ("socialsecuritypayments", "NovedadFechaInicio",  "ALTER TABLE socialsecuritypayments ADD COLUMN NovedadFechaInicio DATETIME(6) NULL AFTER NovedadTipo"),
            ("socialsecuritypayments", "NovedadFechaFin",     "ALTER TABLE socialsecuritypayments ADD COLUMN NovedadFechaFin DATETIME(6) NULL AFTER NovedadFechaInicio"),
            ("socialsecuritypayments", "OperatorEps",         "ALTER TABLE socialsecuritypayments ADD COLUMN OperatorEps VARCHAR(10) NULL AFTER NovedadFechaFin"),
            ("socialsecuritypayments", "OperatorPension",     "ALTER TABLE socialsecuritypayments ADD COLUMN OperatorPension VARCHAR(10) NULL AFTER OperatorEps"),
            ("socialsecuritypayments", "OperatorArl",         "ALTER TABLE socialsecuritypayments ADD COLUMN OperatorArl VARCHAR(10) NULL AFTER OperatorPension"),
            ("socialsecuritypayments", "OperatorCcf",         "ALTER TABLE socialsecuritypayments ADD COLUMN OperatorCcf VARCHAR(10) NULL AFTER OperatorArl"),
            ("socialsecuritypayments", "CotizanteTipo",       "ALTER TABLE socialsecuritypayments ADD COLUMN CotizanteTipo VARCHAR(2) NULL AFTER OperatorCcf"),
            ("socialsecuritypayments", "CotizanteSubtipo",    "ALTER TABLE socialsecuritypayments ADD COLUMN CotizanteSubtipo VARCHAR(2) NULL AFTER CotizanteTipo"),
        };

        foreach (var (table, column, ddl) in statements)
        {
            await using var tblCheck = conn.CreateCommand();
            tblCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                     WHERE table_schema = DATABASE() AND table_name = @tbl";
            var pt = tblCheck.CreateParameter(); pt.ParameterName = "@tbl"; pt.Value = table; tblCheck.Parameters.Add(pt);
            if (Convert.ToInt32(await tblCheck.ExecuteScalarAsync()) == 0) continue;

            await using var check = conn.CreateCommand();
            check.CommandText = @"SELECT COUNT(*) FROM information_schema.columns
                                  WHERE table_schema = DATABASE()
                                    AND table_name = @tbl
                                    AND column_name = @col";
            var pc = check.CreateParameter(); pc.ParameterName = "@tbl"; pc.Value = table; check.Parameters.Add(pc);
            var pcol = check.CreateParameter(); pcol.ParameterName = "@col"; pcol.Value = column; check.Parameters.Add(pcol);
            if (Convert.ToInt32(await check.ExecuteScalarAsync()) > 0) continue;

            await using var alter = conn.CreateCommand();
            alter.CommandText = ddl;
            await alter.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 005 — Inventario: crea tabla inventorylots y agrega ValuationMethod a products.
    /// Idempotente.
    /// </summary>
    private static async Task Apply005(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        // 1) Tabla inventorylots
        await using (var tblCheck = conn.CreateCommand())
        {
            tblCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                     WHERE table_schema = DATABASE() AND table_name = 'inventorylots'";
            if (Convert.ToInt32(await tblCheck.ExecuteScalarAsync()) == 0)
            {
                await using var create = conn.CreateCommand();
                create.CommandText = @"
CREATE TABLE inventorylots (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ProductId INT NOT NULL,
    PurchaseId INT NULL,
    OriginalQuantity INT NOT NULL,
    RemainingQuantity INT NOT NULL,
    UnitCost DECIMAL(18,4) NOT NULL,
    EntryDate DATETIME(6) NOT NULL,
    Reference VARCHAR(100) NULL,
    CreatedAt DATETIME(6) NOT NULL,
    CONSTRAINT FK_inventorylots_products FOREIGN KEY (ProductId) REFERENCES products(Id),
    CONSTRAINT FK_inventorylots_purchases FOREIGN KEY (PurchaseId) REFERENCES purchases(Id) ON DELETE SET NULL,
    INDEX IX_inventorylots_ProductId (ProductId),
    INDEX IX_inventorylots_EntryDate (EntryDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await create.ExecuteNonQueryAsync();
            }
        }

        // 2) Columna ValuationMethod en products
        await using (var check = conn.CreateCommand())
        {
            check.CommandText = @"SELECT COUNT(*) FROM information_schema.columns
                                  WHERE table_schema = DATABASE()
                                    AND table_name = 'products'
                                    AND column_name = 'ValuationMethod'";
            if (Convert.ToInt32(await check.ExecuteScalarAsync()) == 0)
            {
                await using var alter = conn.CreateCommand();
                alter.CommandText = "ALTER TABLE products ADD COLUMN ValuationMethod INT NOT NULL DEFAULT 1 AFTER IsActive";
                await alter.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>
    /// 006 — RBAC: crea tablas permissions y role_permissions, siembra catálogo
    /// y matriz por defecto para los 4 roles (Administrador, Contador, Vendedor, Almacen).
    /// </summary>
    private static async Task Apply006(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        // 1) Tabla permissions
        await using (var tblCheck = conn.CreateCommand())
        {
            tblCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                     WHERE table_schema = DATABASE() AND table_name = 'permissions'";
            if (Convert.ToInt32(await tblCheck.ExecuteScalarAsync()) == 0)
            {
                await using var create = conn.CreateCommand();
                create.CommandText = @"
CREATE TABLE permissions (
    `Key`       VARCHAR(60)  NOT NULL PRIMARY KEY,
    Module      VARCHAR(40)  NOT NULL,
    Action      VARCHAR(20)  NOT NULL,
    Description VARCHAR(200) NOT NULL,
    INDEX IX_permissions_Module (Module)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await create.ExecuteNonQueryAsync();
            }
        }

        // 2) Tabla rolepermissions (coincide con DbSet<RolePermission> de EF)
        await using (var tblCheck2 = conn.CreateCommand())
        {
            tblCheck2.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                      WHERE table_schema = DATABASE() AND table_name = 'rolepermissions'";
            if (Convert.ToInt32(await tblCheck2.ExecuteScalarAsync()) == 0)
            {
                await using var create = conn.CreateCommand();
                create.CommandText = @"
CREATE TABLE rolepermissions (
    Id            INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Role          VARCHAR(40)  NOT NULL,
    PermissionKey VARCHAR(60)  NOT NULL,
    CONSTRAINT FK_rolepermissions_permissions FOREIGN KEY (PermissionKey) REFERENCES permissions(`Key`) ON DELETE CASCADE,
    CONSTRAINT UQ_rolepermissions_Role_Key UNIQUE (Role, PermissionKey),
    INDEX IX_rolepermissions_Role (Role)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await create.ExecuteNonQueryAsync();
            }
        }

        // 3) Sembrar catálogo (idempotente: INSERT IGNORE)
        foreach (var p in PermissionService.Catalog)
        {
            await using var ins = conn.CreateCommand();
            ins.CommandText = @"INSERT IGNORE INTO permissions (`Key`, Module, Action, Description)
                                VALUES (@k, @m, @a, @d)";
            var pk = ins.CreateParameter(); pk.ParameterName = "@k"; pk.Value = p.Key; ins.Parameters.Add(pk);
            var pm = ins.CreateParameter(); pm.ParameterName = "@m"; pm.Value = p.Module; ins.Parameters.Add(pm);
            var pa = ins.CreateParameter(); pa.ParameterName = "@a"; pa.Value = p.Action; ins.Parameters.Add(pa);
            var pd = ins.CreateParameter(); pd.ParameterName = "@d"; pd.Value = p.Description; ins.Parameters.Add(pd);
            await ins.ExecuteNonQueryAsync();
        }

        // 4) Sembrar matriz por defecto solo si el rol aún no tiene permisos asignados.
        foreach (var (role, keys) in PermissionService.DefaultMatrix)
        {
            await using var count = conn.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM rolepermissions WHERE Role = @r";
            var pr = count.CreateParameter(); pr.ParameterName = "@r"; pr.Value = role; count.Parameters.Add(pr);
            if (Convert.ToInt32(await count.ExecuteScalarAsync()) > 0) continue;

            foreach (var key in keys)
            {
                await using var ins = conn.CreateCommand();
                ins.CommandText = @"INSERT IGNORE INTO rolepermissions (Role, PermissionKey)
                                    VALUES (@r, @k)";
                var pr2 = ins.CreateParameter(); pr2.ParameterName = "@r"; pr2.Value = role; ins.Parameters.Add(pr2);
                var pk2 = ins.CreateParameter(); pk2.ParameterName = "@k"; pk2.Value = key; ins.Parameters.Add(pk2);
                await ins.ExecuteNonQueryAsync();
            }
        }
    }
}
