using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

/// <summary>
/// Genera el comprobante contable (asiento) a partir de una nómina procesada.
/// Estructura basada en el Plan Único de Cuentas (PUC) colombiano:
///   510506  Sueldos
///   510507  Auxilio de transporte
///   510512  Aportes a salud
///   510513  Aportes a pensión
///   510515  Aportes a ARL
///   510516  Aportes a caja de compensación
///   510517  Aportes a SENA
///   510518  Aportes a ICBF
///   510525  Prima de servicios
///   510527  Intereses sobre cesantías
///   510530  Cesantías
///   510533  Vacaciones
///   2370    Cesantías (por pagar)
///   2380    Intereses sobre cesantías
///   2510    Salarios por pagar
///   1355    Retención en la fuente asalariados
///   2375    Aportes a seguridad social (por pagar)
///   2385    Aportes parafiscales (por pagar)
/// </summary>
public class PayrollAccountingService
{
    private readonly AppDbContext _db;

    public PayrollAccountingService(AppDbContext db) => _db = db;

    public async Task<JournalEntry> CreateJournalEntryForPayrollAsync(Payroll payroll, AppDbContext db, string userId = "system")
    {
        var accountCodes = new[]
        {
            "510506", "510507", "510512", "510513", "510515", "510516",
            "510517", "510518", "510525", "510527", "510530", "510533",
            "2370", "2380", "2510", "1355", "2375", "2385"
        };
        var accounts = await db.Accounts.Where(a => accountCodes.Contains(a.Code)).ToDictionaryAsync(a => a.Code);

        await EnsureAccountsAsync(db, accounts);

        var nextNumber = (await db.JournalEntries.CountAsync()) + 1;
        var entryNumber = $"NI-{payroll.Id:D6}-{nextNumber:D4}";

        var entry = new JournalEntry
        {
            EntryNumber = entryNumber,
            EntryDate = DateTime.TryParse(payroll.PaymentDate ?? payroll.PeriodEnd, out var d) ? d : DateTime.UtcNow,
            Description = $"Nómina período {payroll.PeriodStart} a {payroll.PeriodEnd}",
            Reference = $"NOM-{payroll.Id}",
            Status = "posted"
        };

        var lines = new List<JournalEntryLine>();

        AddLine(lines, GetAccount(accounts, "510506"), payroll.Details.Sum(x => x.BaseSalary), 0, "Sueldos del período");
        if (payroll.TotalTransportAllowance > 0)
        {
            var transpAcc = GetAccount(accounts, "510507");
            AddLine(lines, transpAcc, payroll.TotalTransportAllowance, 0, "Auxilio de transporte");
        }

        if (payroll.TotalPrimaProvision > 0)
        {
            var acc = GetAccount(accounts, "510525");
            AddLine(lines, acc, payroll.TotalPrimaProvision, 0, "Provisión prima de servicios");
        }
        if (payroll.TotalCesantiasProvision > 0)
        {
            var acc = GetAccount(accounts, "510530");
            AddLine(lines, acc, payroll.TotalCesantiasProvision, 0, "Provisión cesantías");
        }
        if (payroll.TotalCesantiasInterestProvision > 0)
        {
            var acc = GetAccount(accounts, "510527");
            AddLine(lines, acc, payroll.TotalCesantiasInterestProvision, 0, "Provisión intereses sobre cesantías");
        }
        if (payroll.TotalVacationProvision > 0)
        {
            var acc = GetAccount(accounts, "510533");
            AddLine(lines, acc, payroll.TotalVacationProvision, 0, "Provisión vacaciones");
        }

        if (payroll.TotalEmployerHealth > 0)
        {
            var acc = GetAccount(accounts, "510512");
            AddLine(lines, acc, payroll.TotalEmployerHealth, 0, "Aportes salud empleador");
        }
        if (payroll.TotalEmployerPension > 0)
        {
            var acc = GetAccount(accounts, "510513");
            AddLine(lines, acc, payroll.TotalEmployerPension, 0, "Aportes pensión empleador");
        }
        if (payroll.TotalArl > 0)
        {
            var acc = GetAccount(accounts, "510515");
            AddLine(lines, acc, payroll.TotalArl, 0, "Aportes ARL");
        }
        if (payroll.TotalCompensationFund > 0)
        {
            var acc = GetAccount(accounts, "510516");
            AddLine(lines, acc, payroll.TotalCompensationFund, 0, "Aportes Caja de Compensación");
        }
        if (payroll.TotalSena > 0)
        {
            var acc = GetAccount(accounts, "510517");
            AddLine(lines, acc, payroll.TotalSena, 0, "Aportes SENA");
        }
        if (payroll.TotalIcbf > 0)
        {
            var acc = GetAccount(accounts, "510518");
            AddLine(lines, acc, payroll.TotalIcbf, 0, "Aportes ICBF");
        }

        AddLine(lines, GetAccount(accounts, "2510"), 0, payroll.TotalNet, "Salarios netos a pagar");

        var totalRet = payroll.Details.Sum(x => x.WithholdingTax);
        if (totalRet > 0)
        {
            var acc = GetAccount(accounts, "1355");
            AddLine(lines, acc, 0, totalRet, "Retención en la fuente asalariados");
        }

        if (payroll.TotalCesantiasProvision > 0)
        {
            var acc = GetAccount(accounts, "2370");
            AddLine(lines, acc, 0, payroll.TotalCesantiasProvision, "Cesantías por pagar");
        }
        if (payroll.TotalCesantiasInterestProvision > 0)
        {
            var acc = GetAccount(accounts, "2380");
            AddLine(lines, acc, 0, payroll.TotalCesantiasInterestProvision, "Intereses sobre cesantías por pagar");
        }

        var totalSS = payroll.Details.Sum(x => x.EmployeeHealthDeduction + x.EmployeePensionDeduction)
            + payroll.TotalEmployerHealth + payroll.TotalEmployerPension;
        if (totalSS > 0)
        {
            var acc = GetAccount(accounts, "2375");
            AddLine(lines, acc, 0, totalSS, "Aportes a seguridad social por pagar (PILA)");
        }

        var totalPara = payroll.TotalSena + payroll.TotalIcbf + payroll.TotalCompensationFund;
        if (totalPara > 0)
        {
            var acc = GetAccount(accounts, "2385");
            AddLine(lines, acc, 0, totalPara, "Aportes parafiscales por pagar");
        }

        entry.Lines = lines;
        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    private static JournalEntryLine AddLine(List<JournalEntryLine> lines, Account account, decimal debit, decimal credit, string? description)
    {
        var line = new JournalEntryLine
        {
            AccountId = account.Id,
            Debit = Math.Round(debit, 2),
            Credit = Math.Round(credit, 2),
            Description = description
        };
        lines.Add(line);
        return line;
    }

    private static Account GetAccount(Dictionary<string, Account> accounts, string code)
    {
        if (accounts.TryGetValue(code, out var a)) return a;
        throw new InvalidOperationException($"Cuenta PUC {code} no encontrada. Ejecute la siembra de cuentas.");
    }

    private static async Task EnsureAccountsAsync(AppDbContext db, Dictionary<string, Account> accounts)
    {
        var needed = new Dictionary<string, (string Name, string Type)>
        {
            ["510506"] = ("Sueldos", "Gasto"),
            ["510507"] = ("Auxilio de transporte", "Gasto"),
            ["510512"] = ("Aportes a salud (empleador)", "Gasto"),
            ["510513"] = ("Aportes a pensión (empleador)", "Gasto"),
            ["510515"] = ("Aportes a administradoras de riesgos laborales (ARL)", "Gasto"),
            ["510516"] = ("Aportes a caja de compensación familiar", "Gasto"),
            ["510517"] = ("Aportes al SENA", "Gasto"),
            ["510518"] = ("Aportes al ICBF", "Gasto"),
            ["510525"] = ("Prima de servicios", "Gasto"),
            ["510527"] = ("Intereses sobre cesantías", "Gasto"),
            ["510530"] = ("Cesantías", "Gasto"),
            ["510533"] = ("Vacaciones", "Gasto"),
            ["2370"]   = ("Cesantías por pagar", "Pasivo"),
            ["2380"]   = ("Intereses sobre cesantías por pagar", "Pasivo"),
            ["2510"]   = ("Salarios por pagar", "Pasivo"),
            ["1355"]   = ("Retención en la fuente asalariados", "Pasivo"),
            ["2375"]   = ("Aportes a seguridad social por pagar", "Pasivo"),
            ["2385"]   = ("Aportes parafiscales por pagar", "Pasivo")
        };
        foreach (var (code, info) in needed)
        {
            if (accounts.ContainsKey(code)) continue;
            var existing = await db.Accounts.FirstOrDefaultAsync(a => a.Code == code);
            if (existing != null)
            {
                accounts[code] = existing;
                continue;
            }
            string parentCode = info.Type switch
            {
                "Gasto" => "51",
                "Pasivo" => "23",
                _ => "5"
            };
            var parent = await db.Accounts.FirstOrDefaultAsync(a => a.Code == parentCode);
            var accType = info.Type switch
            {
                "Gasto" => AccountType.Gasto,
                "Pasivo" => AccountType.Pasivo,
                _ => AccountType.Activo
            };
            var acc = new Account
            {
                Code = code,
                Name = info.Name,
                Type = accType,
                ParentId = parent?.Id,
                IsActive = true
            };
            db.Accounts.Add(acc);
            accounts[code] = acc;
        }
        await db.SaveChangesAsync();
    }
}
