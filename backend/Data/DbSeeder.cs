using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await EnsureElectronicInvoiceColumnsAsync(db);
        await EnsureNewTablesAsync(db);
        await EnsureSocialSecurityColumnsAsync(db);
        await EnsureEmployeesColumnsAsync(db);
        await EnsurePayrollColumnsAsync(db);
        await EnsurePayrollDetailsColumnsAsync(db);
        await EnsurePayrollDeductionLinesColumnsAsync(db);

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

    private static async Task EnsureNewTablesAsync(AppDbContext db)
    {
        var tables = new (string name, string sql)[]
        {
            ("Employees", @"CREATE TABLE IF NOT EXISTS `Employees` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `Name` varchar(200) NOT NULL,
                `Email` varchar(200) NULL,
                `Phone` varchar(50) NULL,
                `Position` varchar(100) NULL,
                `Department` varchar(100) NULL,
                `HireDate` datetime(6) NOT NULL,
                `TaxId` varchar(50) NULL,
                `BankAccount` varchar(50) NULL,
                `BankName` varchar(100) NULL,
                `BankAccountType` varchar(20) NULL,
                `BaseSalary` decimal(18,2) NOT NULL DEFAULT 0,
                `IsActive` tinyint(1) NOT NULL DEFAULT 1,
                `ContractType` varchar(20) NOT NULL DEFAULT 'indefinido',
                `IntegralSalary` tinyint(1) NOT NULL DEFAULT 0,
                `TerminationReason` varchar(50) NULL,
                `TerminationDate` datetime(6) NULL,
                `WithholdingProcedure2` tinyint(1) NOT NULL DEFAULT 0,
                `TransportAllowanceOverride` tinyint(1) NULL,
                `SolidarityFundOverride` decimal(18,2) NULL,
                `CreatedAt` datetime(6) NOT NULL DEFAULT NOW(6),
                `UpdatedAt` datetime(6) NOT NULL DEFAULT NOW(6),
                PRIMARY KEY (`Id`)
            ) CHARACTER SET=utf8mb4"),

            ("PayrollDeductions", @"CREATE TABLE IF NOT EXISTS `PayrollDeductions` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `Name` varchar(100) NOT NULL,
                `Type` varchar(20) NOT NULL DEFAULT 'percentage',
                `Value` decimal(18,2) NOT NULL DEFAULT 0,
                `Description` varchar(300) NULL,
                `IsActive` tinyint(1) NOT NULL DEFAULT 1,
                `CreatedAt` datetime(6) NOT NULL DEFAULT NOW(6),
                PRIMARY KEY (`Id`)
            ) CHARACTER SET=utf8mb4"),

            ("Payrolls", @"CREATE TABLE IF NOT EXISTS `Payrolls` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `PeriodStart` varchar(20) NOT NULL,
                `PeriodEnd` varchar(20) NOT NULL,
                `Status` varchar(20) NOT NULL DEFAULT 'draft',
                `PaymentDate` varchar(20) NULL,
                `TotalGross` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalTransportAllowance` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalDeductions` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalNet` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalEmployerCost` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalPrimaProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalCesantiasProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalCesantiasInterestProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalVacationProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalEmployerHealth` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalEmployerPension` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalArl` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalCompensationFund` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalSena` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalIcbf` decimal(18,2) NOT NULL DEFAULT 0,
                `Notes` varchar(500) NULL,
                `CreatedAt` datetime(6) NOT NULL DEFAULT NOW(6),
                `UpdatedAt` datetime(6) NOT NULL DEFAULT NOW(6),
                PRIMARY KEY (`Id`)
            ) CHARACTER SET=utf8mb4"),

            ("PayrollDetails", @"CREATE TABLE IF NOT EXISTS `PayrollDetails` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `PayrollId` int NOT NULL,
                `EmployeeId` int NOT NULL DEFAULT 0,
                `EmployeeName` varchar(200) NOT NULL,
                `BaseSalary` decimal(18,2) NOT NULL DEFAULT 0,
                `TransportAllowance` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalGross` decimal(18,2) NOT NULL DEFAULT 0,
                `Ibc` decimal(18,2) NOT NULL DEFAULT 0,
                `EmployeeHealthDeduction` decimal(18,2) NOT NULL DEFAULT 0,
                `EmployeePensionDeduction` decimal(18,2) NOT NULL DEFAULT 0,
                `SolidarityFundDeduction` decimal(18,2) NOT NULL DEFAULT 0,
                `WithholdingTax` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalDeductions` decimal(18,2) NOT NULL DEFAULT 0,
                `NetSalary` decimal(18,2) NOT NULL DEFAULT 0,
                `EmployerHealthContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `EmployerPensionContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `ArlContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `CompensationFundContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `SenaContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `IcbfContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalEmployerContributions` decimal(18,2) NOT NULL DEFAULT 0,
                `PrimaProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `CesantiasProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `CesantiasInterestProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `VacationProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalProvisions` decimal(18,2) NOT NULL DEFAULT 0,
                PRIMARY KEY (`Id`),
                KEY `IX_PayrollDetails_PayrollId` (`PayrollId`)
            ) CHARACTER SET=utf8mb4"),

            ("PayrollDeductionLines", @"CREATE TABLE IF NOT EXISTS `PayrollDeductionLines` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `PayrollDetailId` int NOT NULL,
                `DeductionId` int NOT NULL DEFAULT 0,
                `DeductionName` varchar(100) NOT NULL,
                `Category` varchar(20) NOT NULL DEFAULT 'other',
                `Amount` decimal(18,2) NOT NULL DEFAULT 0,
                PRIMARY KEY (`Id`),
                KEY `IX_PayrollDeductionLines_PayrollDetailId` (`PayrollDetailId`)
            ) CHARACTER SET=utf8mb4"),

            ("SocialSecurityPayments", @"CREATE TABLE IF NOT EXISTS `SocialSecurityPayments` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `Period` varchar(20) NOT NULL,
                `EmployeeId` int NOT NULL DEFAULT 0,
                `EmployeeName` varchar(200) NOT NULL,
                `TaxId` varchar(50) NULL,
                `BaseSalary` decimal(18,2) NOT NULL DEFAULT 0,
                `TransportAllowance` decimal(18,2) NOT NULL DEFAULT 0,
                `Ibc` decimal(18,2) NOT NULL DEFAULT 0,
                `CapIbc` decimal(18,2) NOT NULL DEFAULT 0,
                `EmployeeHealthContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `EmployeePensionContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `SolidarityFundContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `EmployerHealthContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `EmployerPensionContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `ArlContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `CompensationFundContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `SenaContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `IcbfContribution` decimal(18,2) NOT NULL DEFAULT 0,
                `EmployeeContributionTotal` decimal(18,2) NOT NULL DEFAULT 0,
                `EmployerContributionTotal` decimal(18,2) NOT NULL DEFAULT 0,
                `ContributionRate` decimal(8,2) NOT NULL DEFAULT 8,
                `ContributionAmount` decimal(18,2) NOT NULL DEFAULT 0,
                `PaymentDate` varchar(20) NULL,
                `Status` varchar(20) NOT NULL DEFAULT 'pending',
                `Reference` varchar(100) NULL,
                `Operator` varchar(100) NULL,
                `CreatedAt` datetime(6) NOT NULL DEFAULT NOW(6),
                PRIMARY KEY (`Id`)
            ) CHARACTER SET=utf8mb4"),

            ("PaymentRecords", @"CREATE TABLE IF NOT EXISTS `PaymentRecords` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `PayrollId` int NOT NULL DEFAULT 0,
                `PeriodStart` varchar(20) NOT NULL,
                `PeriodEnd` varchar(20) NOT NULL,
                `PaymentDate` varchar(20) NOT NULL,
                `TotalAmount` decimal(18,2) NOT NULL DEFAULT 0,
                `PaymentMethod` varchar(30) NOT NULL DEFAULT 'transfer',
                `Reference` varchar(100) NULL,
                `Status` varchar(20) NOT NULL DEFAULT 'completed',
                `Notes` varchar(300) NULL,
                `CreatedAt` datetime(6) NOT NULL DEFAULT NOW(6),
                PRIMARY KEY (`Id`)
            ) CHARACTER SET=utf8mb4"),

            ("LegalParameters", @"CREATE TABLE IF NOT EXISTS `LegalParameters` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `Year` int NOT NULL,
                `Smlmv` decimal(18,2) NOT NULL DEFAULT 0,
                `Uvt` decimal(18,2) NOT NULL DEFAULT 0,
                `TransportAllowance` decimal(18,2) NOT NULL DEFAULT 0,
                `TransportAllowanceTop` decimal(8,2) NOT NULL DEFAULT 2,
                `MinimumWithholdingUvt` decimal(8,2) NOT NULL DEFAULT 95,
                `ExemptIncomeUvt` decimal(8,2) NOT NULL DEFAULT 240,
                `MaxHealthIbcSmlmv` decimal(8,2) NOT NULL DEFAULT 25,
                `ArlRiskOneRate` decimal(8,3) NOT NULL DEFAULT 0.522,
                `EmployerHealthRate` decimal(8,2) NOT NULL DEFAULT 8.5,
                `EmployerPensionRate` decimal(8,2) NOT NULL DEFAULT 12.0,
                `CompensationFundRate` decimal(8,2) NOT NULL DEFAULT 4.0,
                `SenaRate` decimal(8,2) NOT NULL DEFAULT 2.0,
                `IcbfRate` decimal(8,2) NOT NULL DEFAULT 3.0,
                `EmployeeHealthRate` decimal(8,2) NOT NULL DEFAULT 4.0,
                `EmployeePensionRate` decimal(8,2) NOT NULL DEFAULT 4.0,
                `SolidarityFundLowRate` decimal(8,2) NOT NULL DEFAULT 1.0,
                `SolidarityFundHighRate` decimal(8,2) NOT NULL DEFAULT 1.2,
                `PrimaYearFraction` decimal(8,2) NOT NULL DEFAULT 1,
                `CesantiasYearFraction` decimal(8,2) NOT NULL DEFAULT 1,
                `CesantiasInterestRate` decimal(8,2) NOT NULL DEFAULT 12,
                `VacationDaysPerYear` decimal(8,2) NOT NULL DEFAULT 15,
                `EffectiveFrom` datetime(6) NOT NULL,
                `Notes` varchar(500) NULL,
                `CreatedAt` datetime(6) NOT NULL DEFAULT NOW(6),
                `UpdatedAt` datetime(6) NOT NULL DEFAULT NOW(6),
                PRIMARY KEY (`Id`),
                UNIQUE KEY `UQ_LegalParameters_Year` (`Year`)
            ) CHARACTER SET=utf8mb4"),

            ("WithholdingTaxBrackets", @"CREATE TABLE IF NOT EXISTS `WithholdingTaxBrackets` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `Year` int NOT NULL,
                `FromUvt` decimal(18,2) NOT NULL DEFAULT 0,
                `ToUvt` decimal(18,2) NULL,
                `MarginalRate` decimal(8,4) NOT NULL DEFAULT 0,
                `BaseTaxUvt` decimal(18,2) NOT NULL DEFAULT 0,
                `Procedure` varchar(5) NOT NULL DEFAULT '1',
                PRIMARY KEY (`Id`),
                KEY `IX_WithholdingTaxBrackets_Year` (`Year`)
            ) CHARACTER SET=utf8mb4"),

            ("PayrollProvisions", @"CREATE TABLE IF NOT EXISTS `PayrollProvisions` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `EmployeeId` int NOT NULL,
                `EmployeeName` varchar(200) NOT NULL,
                `Year` int NOT NULL,
                `Month` int NOT NULL,
                `PeriodLabel` varchar(20) NOT NULL,
                `BaseSalary` decimal(18,2) NOT NULL DEFAULT 0,
                `TransportAllowance` decimal(18,2) NOT NULL DEFAULT 0,
                `PrimaProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `CesantiasProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `CesantiasInterestProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `VacationProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalProvision` decimal(18,2) NOT NULL DEFAULT 0,
                `AccumulatedPrima` decimal(18,2) NOT NULL DEFAULT 0,
                `AccumulatedCesantias` decimal(18,2) NOT NULL DEFAULT 0,
                `AccumulatedCesantiasInterest` decimal(18,2) NOT NULL DEFAULT 0,
                `AccumulatedVacations` decimal(18,2) NOT NULL DEFAULT 0,
                `AccumulatedTotal` decimal(18,2) NOT NULL DEFAULT 0,
                `PayrollId` int NULL,
                `CreatedAt` datetime(6) NOT NULL DEFAULT NOW(6),
                PRIMARY KEY (`Id`),
                UNIQUE KEY `UQ_PayrollProvisions_Employee_Month` (`EmployeeId`, `Year`, `Month`)
            ) CHARACTER SET=utf8mb4"),

            ("PayrollSettlements", @"CREATE TABLE IF NOT EXISTS `PayrollSettlements` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `EmployeeId` int NOT NULL,
                `EmployeeName` varchar(200) NOT NULL,
                `TaxId` varchar(50) NULL,
                `SettlementDate` datetime(6) NOT NULL,
                `HireDate` datetime(6) NOT NULL,
                `LastContractDate` datetime(6) NULL,
                `TerminationReason` varchar(50) NULL,
                `BaseSalary` decimal(18,2) NOT NULL DEFAULT 0,
                `TransportAllowance` decimal(18,2) NOT NULL DEFAULT 0,
                `AverageVariableIncome` decimal(18,2) NOT NULL DEFAULT 0,
                `WorkedDays` int NOT NULL DEFAULT 0,
                `WorkedDaysCurrentSemester` int NOT NULL DEFAULT 0,
                `CesantiasAmount` decimal(18,2) NOT NULL DEFAULT 0,
                `CesantiasInterestAmount` decimal(18,2) NOT NULL DEFAULT 0,
                `PrimaAmount` decimal(18,2) NOT NULL DEFAULT 0,
                `VacationAmount` decimal(18,2) NOT NULL DEFAULT 0,
                `SeveranceAmount` decimal(18,2) NOT NULL DEFAULT 0,
                `OtherAmounts` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalGross` decimal(18,2) NOT NULL DEFAULT 0,
                `RetencionFuente` decimal(18,2) NOT NULL DEFAULT 0,
                `TotalDeductions` decimal(18,2) NOT NULL DEFAULT 0,
                `NetToPay` decimal(18,2) NOT NULL DEFAULT 0,
                `Status` varchar(20) NOT NULL DEFAULT 'draft',
                `PaymentDate` varchar(20) NULL,
                `PaymentMethod` varchar(30) NULL,
                `Reference` varchar(100) NULL,
                `Notes` varchar(500) NULL,
                `CreatedAt` datetime(6) NOT NULL DEFAULT NOW(6),
                `UpdatedAt` datetime(6) NOT NULL DEFAULT NOW(6),
                PRIMARY KEY (`Id`),
                KEY `IX_PayrollSettlements_EmployeeId` (`EmployeeId`)
            ) CHARACTER SET=utf8mb4")
        };

        foreach (var (name, sql) in tables)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(sql);
                Console.WriteLine($"[DbSeeder] Tabla `{name}` verificada/creada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbSeeder] Error en tabla `{name}`: {ex.Message}");
            }
        }
    }

    private static async Task EnsureElectronicInvoiceColumnsAsync(AppDbContext db)
    {
        var columns = new (string name, string alterSql)[]
        {
            ("ElectronicInvoiceStatus", "ALTER TABLE Sales ADD COLUMN `ElectronicInvoiceStatus` VARCHAR(20) NOT NULL DEFAULT 'Borrador'"),
            ("ElectronicInvoiceNumber", "ALTER TABLE Sales ADD COLUMN `ElectronicInvoiceNumber` VARCHAR(50) NULL"),
            ("Cufe", "ALTER TABLE Sales ADD COLUMN `Cufe` VARCHAR(128) NULL"),
            ("ElectronicInvoiceIssuedAt", "ALTER TABLE Sales ADD COLUMN `ElectronicInvoiceIssuedAt` DATETIME(6) NULL")
        };

        await EnsureColumnsAsync(db, "Sales", columns);
    }

    private static async Task EnsureEmployeesColumnsAsync(AppDbContext db)
    {
        var columns = new (string name, string alterSql)[]
        {
            ("BankAccountType", "ALTER TABLE Employees ADD COLUMN `BankAccountType` varchar(20) NULL"),
            ("ContractType", "ALTER TABLE Employees ADD COLUMN `ContractType` varchar(20) NOT NULL DEFAULT 'indefinido'"),
            ("IntegralSalary", "ALTER TABLE Employees ADD COLUMN `IntegralSalary` tinyint(1) NOT NULL DEFAULT 0"),
            ("TerminationReason", "ALTER TABLE Employees ADD COLUMN `TerminationReason` varchar(50) NULL"),
            ("TerminationDate", "ALTER TABLE Employees ADD COLUMN `TerminationDate` datetime(6) NULL"),
            ("WithholdingProcedure2", "ALTER TABLE Employees ADD COLUMN `WithholdingProcedure2` tinyint(1) NOT NULL DEFAULT 0"),
            ("TransportAllowanceOverride", "ALTER TABLE Employees ADD COLUMN `TransportAllowanceOverride` tinyint(1) NULL"),
            ("SolidarityFundOverride", "ALTER TABLE Employees ADD COLUMN `SolidarityFundOverride` decimal(18,2) NULL"),
            ("UpdatedAt", "ALTER TABLE Employees ADD COLUMN `UpdatedAt` datetime(6) NOT NULL DEFAULT NOW(6)")
        };
        await EnsureColumnsAsync(db, "Employees", columns);
    }

    private static async Task EnsurePayrollColumnsAsync(AppDbContext db)
    {
        var columns = new (string name, string alterSql)[]
        {
            ("TotalTransportAllowance", "ALTER TABLE Payrolls ADD COLUMN `TotalTransportAllowance` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalEmployerCost", "ALTER TABLE Payrolls ADD COLUMN `TotalEmployerCost` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalPrimaProvision", "ALTER TABLE Payrolls ADD COLUMN `TotalPrimaProvision` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalCesantiasProvision", "ALTER TABLE Payrolls ADD COLUMN `TotalCesantiasProvision` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalCesantiasInterestProvision", "ALTER TABLE Payrolls ADD COLUMN `TotalCesantiasInterestProvision` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalVacationProvision", "ALTER TABLE Payrolls ADD COLUMN `TotalVacationProvision` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalEmployerHealth", "ALTER TABLE Payrolls ADD COLUMN `TotalEmployerHealth` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalEmployerPension", "ALTER TABLE Payrolls ADD COLUMN `TotalEmployerPension` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalArl", "ALTER TABLE Payrolls ADD COLUMN `TotalArl` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalCompensationFund", "ALTER TABLE Payrolls ADD COLUMN `TotalCompensationFund` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalSena", "ALTER TABLE Payrolls ADD COLUMN `TotalSena` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalIcbf", "ALTER TABLE Payrolls ADD COLUMN `TotalIcbf` decimal(18,2) NOT NULL DEFAULT 0")
        };
        await EnsureColumnsAsync(db, "Payrolls", columns);
    }

    private static async Task EnsurePayrollDetailsColumnsAsync(AppDbContext db)
    {
        var columns = new (string name, string alterSql)[]
        {
            ("TransportAllowance", "ALTER TABLE PayrollDetails ADD COLUMN `TransportAllowance` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalGross", "ALTER TABLE PayrollDetails ADD COLUMN `TotalGross` decimal(18,2) NOT NULL DEFAULT 0"),
            ("Ibc", "ALTER TABLE PayrollDetails ADD COLUMN `Ibc` decimal(18,2) NOT NULL DEFAULT 0"),
            ("EmployeeHealthDeduction", "ALTER TABLE PayrollDetails ADD COLUMN `EmployeeHealthDeduction` decimal(18,2) NOT NULL DEFAULT 0"),
            ("EmployeePensionDeduction", "ALTER TABLE PayrollDetails ADD COLUMN `EmployeePensionDeduction` decimal(18,2) NOT NULL DEFAULT 0"),
            ("SolidarityFundDeduction", "ALTER TABLE PayrollDetails ADD COLUMN `SolidarityFundDeduction` decimal(18,2) NOT NULL DEFAULT 0"),
            ("WithholdingTax", "ALTER TABLE PayrollDetails ADD COLUMN `WithholdingTax` decimal(18,2) NOT NULL DEFAULT 0"),
            ("EmployerHealthContribution", "ALTER TABLE PayrollDetails ADD COLUMN `EmployerHealthContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("EmployerPensionContribution", "ALTER TABLE PayrollDetails ADD COLUMN `EmployerPensionContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("ArlContribution", "ALTER TABLE PayrollDetails ADD COLUMN `ArlContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("CompensationFundContribution", "ALTER TABLE PayrollDetails ADD COLUMN `CompensationFundContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("SenaContribution", "ALTER TABLE PayrollDetails ADD COLUMN `SenaContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("IcbfContribution", "ALTER TABLE PayrollDetails ADD COLUMN `IcbfContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalEmployerContributions", "ALTER TABLE PayrollDetails ADD COLUMN `TotalEmployerContributions` decimal(18,2) NOT NULL DEFAULT 0"),
            ("PrimaProvision", "ALTER TABLE PayrollDetails ADD COLUMN `PrimaProvision` decimal(18,2) NOT NULL DEFAULT 0"),
            ("CesantiasProvision", "ALTER TABLE PayrollDetails ADD COLUMN `CesantiasProvision` decimal(18,2) NOT NULL DEFAULT 0"),
            ("CesantiasInterestProvision", "ALTER TABLE PayrollDetails ADD COLUMN `CesantiasInterestProvision` decimal(18,2) NOT NULL DEFAULT 0"),
            ("VacationProvision", "ALTER TABLE PayrollDetails ADD COLUMN `VacationProvision` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TotalProvisions", "ALTER TABLE PayrollDetails ADD COLUMN `TotalProvisions` decimal(18,2) NOT NULL DEFAULT 0")
        };
        await EnsureColumnsAsync(db, "PayrollDetails", columns);
    }

    private static async Task EnsurePayrollDeductionLinesColumnsAsync(AppDbContext db)
    {
        var columns = new (string name, string alterSql)[]
        {
            ("Category", "ALTER TABLE PayrollDeductionLines ADD COLUMN `Category` varchar(20) NOT NULL DEFAULT 'other'")
        };
        await EnsureColumnsAsync(db, "PayrollDeductionLines", columns);
    }

    private static async Task EnsureColumnsAsync(AppDbContext db, string table, (string name, string alterSql)[] columns)
    {
        foreach (var (name, alterSql) in columns)
        {
            var exists = await db.Database
                .SqlQueryRaw<int>(
                    @"SELECT COUNT(*) AS `Value`
                      FROM information_schema.columns
                      WHERE table_schema = DATABASE()
                        AND table_name = {0}
                        AND column_name = {1}",
                    table, name)
                .SingleAsync();

            if (exists == 0)
                await db.Database.ExecuteSqlRawAsync(alterSql);
        }
    }

    private static async Task EnsureSocialSecurityColumnsAsync(AppDbContext db)
    {
        var columns = new (string name, string alterSql)[]
        {
            ("EmployeeHealthContribution", "ALTER TABLE SocialSecurityPayments ADD COLUMN `EmployeeHealthContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("EmployeePensionContribution", "ALTER TABLE SocialSecurityPayments ADD COLUMN `EmployeePensionContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("EmployerHealthContribution", "ALTER TABLE SocialSecurityPayments ADD COLUMN `EmployerHealthContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("EmployerPensionContribution", "ALTER TABLE SocialSecurityPayments ADD COLUMN `EmployerPensionContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("ArlContribution", "ALTER TABLE SocialSecurityPayments ADD COLUMN `ArlContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("CompensationFundContribution", "ALTER TABLE SocialSecurityPayments ADD COLUMN `CompensationFundContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("SenaContribution", "ALTER TABLE SocialSecurityPayments ADD COLUMN `SenaContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("IcbfContribution", "ALTER TABLE SocialSecurityPayments ADD COLUMN `IcbfContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("EmployeeContributionTotal", "ALTER TABLE SocialSecurityPayments ADD COLUMN `EmployeeContributionTotal` decimal(18,2) NOT NULL DEFAULT 0"),
            ("EmployerContributionTotal", "ALTER TABLE SocialSecurityPayments ADD COLUMN `EmployerContributionTotal` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TransportAllowance", "ALTER TABLE SocialSecurityPayments ADD COLUMN `TransportAllowance` decimal(18,2) NOT NULL DEFAULT 0"),
            ("Ibc", "ALTER TABLE SocialSecurityPayments ADD COLUMN `Ibc` decimal(18,2) NOT NULL DEFAULT 0"),
            ("CapIbc", "ALTER TABLE SocialSecurityPayments ADD COLUMN `CapIbc` decimal(18,2) NOT NULL DEFAULT 0"),
            ("SolidarityFundContribution", "ALTER TABLE SocialSecurityPayments ADD COLUMN `SolidarityFundContribution` decimal(18,2) NOT NULL DEFAULT 0"),
            ("TaxId", "ALTER TABLE SocialSecurityPayments ADD COLUMN `TaxId` varchar(50) NULL"),
            ("Operator", "ALTER TABLE SocialSecurityPayments ADD COLUMN `Operator` varchar(100) NULL")
        };

        foreach (var (name, alterSql) in columns)
        {
            var exists = await db.Database
                .SqlQueryRaw<int>(
                    @"SELECT COUNT(*) AS `Value`
                      FROM information_schema.columns
                      WHERE table_schema = DATABASE()
                        AND table_name = 'SocialSecurityPayments'
                        AND column_name = {0}",
                    name)
                .SingleAsync();

            if (exists == 0)
                await db.Database.ExecuteSqlRawAsync(alterSql);
        }
    }
}
