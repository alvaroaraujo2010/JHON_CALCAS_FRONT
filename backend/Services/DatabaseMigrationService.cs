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
        new("005_inventory_valuation",
            "Inventario: tabla inventorylots + ValuationMethod en products (PEPS/Promedio Ponderado)",
            Apply005),
        new("006_rbac_permissions",
            "RBAC: tablas permissions + role_permissions, catálogo y matriz por defecto sembrada",
            Apply006),
        new("007_ecommerce",
            "E-commerce: tablas catalogproducts, orders, orderitems + campos para Mercado Pago",
            Apply007),
        new("008_catalogproduct_model",
            "Agrega columna Model a catalogproducts si no existe (para BD existentes que aplicaron 007 sin el ALTER)",
            Apply008),
        new("009_subscription",
            "Crea tabla subscription con IsActive, MonthlyFee, DueDate y siembra registro activo por defecto",
            Apply009),
        new("010_catalogproduct_metadata",
            "Catálogo: MenuModel, DesignRef, Color, InternalProductId para filtros del menú e inventario",
            Apply010),
        new("011_rbac_sync_ecommerce",
            "Sincroniza permisos de e-commerce (catalog.manage, orders.*) en catálogo y roles por defecto",
            Apply011),
        new("012_order_stock_deducted",
            "Pedidos web: columna StockDeductedAt para descuento idempotente de inventario",
            Apply012),
        new("013_payment_settings",
            "Pasarelas: tabla paymentsettings y permisos para configurar Mercado Pago",
            Apply013),
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

    /// <summary>
    /// 008 — Agrega columna Model a catalogproducts si no existe.
    /// </summary>
    private static async Task Apply008(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        await using var tblCheck = conn.CreateCommand();
        tblCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                 WHERE table_schema = DATABASE() AND table_name = 'catalogproducts'";
        if (Convert.ToInt32(await tblCheck.ExecuteScalarAsync()) == 0) return;

        await using var colCheck = conn.CreateCommand();
        colCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.columns
            WHERE table_schema = DATABASE() AND table_name = 'catalogproducts' AND column_name = 'Model'";
        if (Convert.ToInt32(await colCheck.ExecuteScalarAsync()) == 0)
        {
            await using var alter = conn.CreateCommand();
            alter.CommandText = "ALTER TABLE catalogproducts ADD COLUMN Model VARCHAR(100) NULL AFTER Brand";
            await alter.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 009 — Crea tabla subscription y siembra registro activo por defecto.
    /// </summary>
    private static async Task Apply009(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        await using (var tblCheck = conn.CreateCommand())
        {
            tblCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                     WHERE table_schema = DATABASE() AND table_name = 'subscription'";
            if (Convert.ToInt32(await tblCheck.ExecuteScalarAsync()) == 0)
            {
                await using var create = conn.CreateCommand();
                create.CommandText = @"
CREATE TABLE subscription (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    MonthlyFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    DueDate DATETIME(6) NULL,
    UpdatedAt DATETIME(6) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await create.ExecuteNonQueryAsync();

                // Seed: registro activo por defecto
                await using var seed = conn.CreateCommand();
                seed.CommandText = "INSERT INTO subscription (IsActive, MonthlyFee, UpdatedAt) VALUES (1, 0, NOW())";
                await seed.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>
    /// 007 — E-commerce: tablas catalogproducts, orders, orderitems.
    /// </summary>
    private static async Task Apply007(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        // 1) Tabla catalogproducts
        await using (var tblCheck = conn.CreateCommand())
        {
            tblCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                     WHERE table_schema = DATABASE() AND table_name = 'catalogproducts'";
            if (Convert.ToInt32(await tblCheck.ExecuteScalarAsync()) == 0)
            {
                await using var create = conn.CreateCommand();
                create.CommandText = @"
CREATE TABLE catalogproducts (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Slug VARCHAR(100) NOT NULL,
    ProductLine VARCHAR(50) NOT NULL,
    Brand VARCHAR(50) NOT NULL,
    Model VARCHAR(100) NULL,
    Title VARCHAR(200) NOT NULL,
    Description TEXT NULL,
    Price DECIMAL(18,2) NOT NULL DEFAULT 0,
    ImageFileName VARCHAR(200) NOT NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NOT NULL,
    INDEX IX_catalogproducts_Slug (Slug),
    INDEX IX_catalogproducts_ProductLine (ProductLine),
    INDEX IX_catalogproducts_Brand (Brand)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await create.ExecuteNonQueryAsync();
            }
            else
            {
                // Agrega columna Model si no existe (para BD existentes)
                await using var colCheck = conn.CreateCommand();
                colCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.columns
                    WHERE table_schema = DATABASE() AND table_name = 'catalogproducts' AND column_name = 'Model'";
                if (Convert.ToInt32(await colCheck.ExecuteScalarAsync()) == 0)
                {
                    await using var alter = conn.CreateCommand();
                    alter.CommandText = "ALTER TABLE catalogproducts ADD COLUMN Model VARCHAR(100) NULL AFTER Brand";
                    await alter.ExecuteNonQueryAsync();
                }
            }
        }

        // 2) Tabla orders
        await using (var tblCheck2 = conn.CreateCommand())
        {
            tblCheck2.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                      WHERE table_schema = DATABASE() AND table_name = 'orders'";
            if (Convert.ToInt32(await tblCheck2.ExecuteScalarAsync()) == 0)
            {
                await using var create = conn.CreateCommand();
                create.CommandText = @"
CREATE TABLE orders (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    OrderNumber VARCHAR(20) NOT NULL,
    CustomerName VARCHAR(200) NOT NULL,
    CustomerEmail VARCHAR(200) NOT NULL,
    CustomerPhone VARCHAR(50) NOT NULL,
    CustomerAddress VARCHAR(500) NULL,
    City VARCHAR(100) NULL,
    Department VARCHAR(100) NULL,
    Notes TEXT NULL,
    Subtotal DECIMAL(18,2) NOT NULL,
    ShippingCost DECIMAL(18,2) NOT NULL DEFAULT 0,
    Total DECIMAL(18,2) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'pending',
    MpPaymentId VARCHAR(100) NULL,
    MpPaymentStatus VARCHAR(30) NULL,
    PaymentMethod VARCHAR(30) NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NOT NULL,
    INDEX IX_orders_OrderNumber (OrderNumber),
    INDEX IX_orders_Status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await create.ExecuteNonQueryAsync();
            }
        }

        // 3) Tabla orderitems
        await using (var tblCheck3 = conn.CreateCommand())
        {
            tblCheck3.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                      WHERE table_schema = DATABASE() AND table_name = 'orderitems'";
            if (Convert.ToInt32(await tblCheck3.ExecuteScalarAsync()) == 0)
            {
                await using var create = conn.CreateCommand();
                create.CommandText = @"
CREATE TABLE orderitems (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    OrderId INT NOT NULL,
    CatalogProductId INT NOT NULL,
    ProductTitle VARCHAR(200) NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_orderitems_orders FOREIGN KEY (OrderId) REFERENCES orders(Id) ON DELETE CASCADE,
    INDEX IX_orderitems_OrderId (OrderId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await create.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>
    /// 010 — Metadatos de catálogo: MenuModel, DesignRef, Color, InternalProductId.
    /// </summary>
    private static async Task Apply010(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        await using var tblCheck = conn.CreateCommand();
        tblCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                 WHERE table_schema = DATABASE() AND table_name = 'catalogproducts'";
        if (Convert.ToInt32(await tblCheck.ExecuteScalarAsync()) == 0) return;

        var columns = new (string Name, string Sql)[]
        {
            ("MenuModel", "ALTER TABLE catalogproducts ADD COLUMN MenuModel VARCHAR(100) NULL AFTER Model"),
            ("DesignRef", "ALTER TABLE catalogproducts ADD COLUMN DesignRef VARCHAR(100) NULL AFTER MenuModel"),
            ("Color", "ALTER TABLE catalogproducts ADD COLUMN Color VARCHAR(50) NULL AFTER DesignRef"),
            ("InternalProductId", "ALTER TABLE catalogproducts ADD COLUMN InternalProductId INT NULL AFTER Color"),
        };

        foreach (var (name, sql) in columns)
        {
            await using var colCheck = conn.CreateCommand();
            colCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.columns
                WHERE table_schema = DATABASE() AND table_name = 'catalogproducts' AND column_name = @col";
            var p = colCheck.CreateParameter();
            p.ParameterName = "@col";
            p.Value = name;
            colCheck.Parameters.Add(p);
            if (Convert.ToInt32(await colCheck.ExecuteScalarAsync()) == 0)
            {
                await using var alter = conn.CreateCommand();
                alter.CommandText = sql;
                await alter.ExecuteNonQueryAsync();
            }
        }

        await using var idxCheck = conn.CreateCommand();
        idxCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.statistics
            WHERE table_schema = DATABASE() AND table_name = 'catalogproducts' AND index_name = 'IX_catalogproducts_MenuModel'";
        if (Convert.ToInt32(await idxCheck.ExecuteScalarAsync()) == 0)
        {
            await using var idx = conn.CreateCommand();
            idx.CommandText = "CREATE INDEX IX_catalogproducts_MenuModel ON catalogproducts (ProductLine, Brand, MenuModel)";
            await idx.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 011 — Añade permisos nuevos al catálogo y a la matriz por defecto de cada rol (INSERT IGNORE).
    /// Cubre permisos de e-commerce agregados después de la migración 006 inicial.
    /// </summary>
    private static async Task Apply011(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

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

        foreach (var (role, keys) in PermissionService.DefaultMatrix)
        {
            foreach (var key in keys)
            {
                await using var ins = conn.CreateCommand();
                ins.CommandText = @"INSERT IGNORE INTO rolepermissions (Role, PermissionKey)
                                    VALUES (@r, @k)";
                var pr = ins.CreateParameter(); pr.ParameterName = "@r"; pr.Value = role; ins.Parameters.Add(pr);
                var pk = ins.CreateParameter(); pk.ParameterName = "@k"; pk.Value = key; ins.Parameters.Add(pk);
                await ins.ExecuteNonQueryAsync();
            }
        }
    }

    private static async Task Apply012(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        await using var tblCheck = conn.CreateCommand();
        tblCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.tables
                                 WHERE table_schema = DATABASE() AND table_name = 'orders'";
        if (Convert.ToInt32(await tblCheck.ExecuteScalarAsync()) == 0) return;

        await using var colCheck = conn.CreateCommand();
        colCheck.CommandText = @"SELECT COUNT(*) FROM information_schema.columns
            WHERE table_schema = DATABASE() AND table_name = 'orders' AND column_name = 'StockDeductedAt'";
        if (Convert.ToInt32(await colCheck.ExecuteScalarAsync()) == 0)
        {
            await using var alter = conn.CreateCommand();
            alter.CommandText = "ALTER TABLE orders ADD COLUMN StockDeductedAt DATETIME(6) NULL AFTER PaymentMethod";
            await alter.ExecuteNonQueryAsync();
        }
    }

    private static async Task Apply013(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        await using var create = conn.CreateCommand();
        create.CommandText = @"
CREATE TABLE IF NOT EXISTS paymentsettings (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Provider VARCHAR(50) NOT NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    UseSandbox TINYINT(1) NOT NULL DEFAULT 0,
    PublicKey VARCHAR(255) NULL,
    AccessToken VARCHAR(255) NULL,
    BaseUrl VARCHAR(255) NULL,
    WebhookUrl VARCHAR(255) NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UNIQUE INDEX IX_paymentsettings_Provider (Provider)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
        await create.ExecuteNonQueryAsync();

        await using var seed = conn.CreateCommand();
        seed.CommandText = @"
INSERT INTO paymentsettings (Provider, IsActive, UseSandbox, PublicKey, AccessToken, BaseUrl, WebhookUrl, CreatedAt, UpdatedAt)
SELECT 'mercadopago', 1, 0, '', '', '', '', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM paymentsettings WHERE Provider = 'mercadopago');";
        await seed.ExecuteNonQueryAsync();

        await db.Database.ExecuteSqlRawAsync(@"
INSERT IGNORE INTO permissions (`Key`, Module, Action, Description)
VALUES ('payments.manage', 'payments', 'manage', 'Configurar pasarelas de pago');");

        await db.Database.ExecuteSqlRawAsync(@"
INSERT IGNORE INTO rolepermissions (Role, PermissionKey)
VALUES ('Administrador', 'payments.manage');");
    }
}
