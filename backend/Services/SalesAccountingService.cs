using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

/// <summary>
/// Genera el comprobante contable de una venta con desglose según el PUC colombiano:
///   1305*  Clientes nacionales (CxC)
///   135515 Retención en la fuente por compras (clientes agentes retenedores)
///   2408*  IVA generado (débito fiscal 19%)
///   4135*  Comercio al por mayor y menor (ingreso)
///   1435*  Mercancías no fabricadas por la empresa (inventario, salida)
///   6135*  Costo de ventas (CMV)
///   1110*  Caja / Bancos (si es venta de contado, no genera CxC)
///
/// Cuando el cliente es agente retenedor de renta, se le practica
/// Retención en la Fuente del 2.5% sobre el subtotal
/// (Art. 1.2.4.9.1 DUR 1625/2016, concordante con Art. 401 ET).
/// </summary>
public class SalesAccountingService
{
    private readonly AppDbContext _db;

    /// <summary>Tarifa ReteFuente por compras a agentes retenedores (Art. 1.2.4.9.1 DUR 1625/2016).</summary>
    public const decimal ReteFuenteRate = 0.025m;

    public SalesAccountingService(AppDbContext db) => _db = db;

    public async Task<JournalEntry> CreateJournalEntryForSaleAsync(
        Sale sale,
        Customer? customer,
        decimal totalCostoVenta,
        string userId = "system")
    {
        var codes = new[]
        {
            "130505", "135515", "240805", "413595", "143505", "613595", "111005", "111010", "1305", "2408", "4135", "1435", "6135"
        };
        var accounts = await _db.Accounts
            .Where(a => codes.Contains(a.Code))
            .ToDictionaryAsync(a => a.Code);

        await EnsureAccountsAsync(accounts);

        var nextNumber = (await _db.JournalEntries.CountAsync()) + 1;
        var entryNumber = $"VT-{sale.Id:D6}-{nextNumber:D4}";

        var entry = new JournalEntry
        {
            EntryNumber = entryNumber,
            EntryDate = sale.SaleDate,
            Description = $"Venta {sale.DocumentNumber} — {(customer?.Name ?? "Consumidor final")}",
            Reference = sale.DocumentNumber,
            Status = "posted"
        };

        var lines = new List<JournalEntryLine>();
        var reteFuente = customer?.IsRetentionAgent == true ? Math.Round(sale.Subtotal * ReteFuenteRate, 2) : 0m;
        var isCash = string.Equals(sale.PaymentMethod, "Efectivo", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(sale.PaymentMethod, "Contado", StringComparison.OrdinalIgnoreCase);
        var isBank = string.Equals(sale.PaymentMethod, "MercadoPago", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(sale.PaymentMethod, "Transferencia", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(sale.PaymentMethod, "Tarjeta", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(sale.PaymentMethod, "PSE", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(sale.PaymentMethod, "Bancos", StringComparison.OrdinalIgnoreCase);

        // ─── DÉBITO: Caja, Bancos o Clientes (CxC) por el monto BRUTO facturado ───
        if (isCash)
        {
            AddLine(lines, GetAccount(accounts, "111005"), sale.Total, 0,
                $"Cobro en efectivo de venta {sale.DocumentNumber}");
        }
        else if (isBank)
        {
            AddLine(lines, GetAccount(accounts, "111010"), sale.Total, 0,
                $"Cobro electrónico / bancos — venta {sale.DocumentNumber}");
        }
        else
        {
            AddLine(lines, GetAccount(accounts, "130505"), sale.Total, 0,
                $"CxC cliente {(customer?.Name ?? "Consumidor final")} — {sale.DocumentNumber}");
        }

        // ─── CRÉDITO: Ingreso por ventas (NETO de ReteFuente si aplica) ───
        // Si el cliente retiene, el ingreso realmente esperado es subtotal - reteFuente.
        var netIncome = sale.Subtotal - reteFuente;
        AddLine(lines, GetAccount(accounts, "413595"), 0, netIncome,
            $"Ingreso por venta {sale.DocumentNumber}{(reteFuente > 0 ? " (neto de ReteFuente)" : "")}");

        // ─── CRÉDITO: IVA generado ───
        if (sale.Tax > 0)
        {
            AddLine(lines, GetAccount(accounts, "240805"), 0, sale.Tax,
                $"IVA 19% sobre venta {sale.DocumentNumber}");
        }

        // ─── CRÉDITO: ReteFuente practicada (cuenta de pasivo, se paga al gobierno) ───
        if (reteFuente > 0)
        {
            AddLine(lines, GetAccount(accounts, "135515"), 0, reteFuente,
                $"ReteFuente 2.5% practicada (cliente agente retenedor)");
        }

        // ─── Asiento paralelo: CMV e inventario ───
        if (totalCostoVenta > 0)
        {
            AddLine(lines, GetAccount(accounts, "613595"), totalCostoVenta, 0,
                $"CMV venta {sale.DocumentNumber}");

            AddLine(lines, GetAccount(accounts, "143505"), 0, totalCostoVenta,
                $"Salida de inventario por venta {sale.DocumentNumber}");
        }

        entry.Lines = lines;
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    private static void AddLine(List<JournalEntryLine> lines, Account account, decimal debit, decimal credit, string? description)
    {
        lines.Add(new JournalEntryLine
        {
            AccountId = account.Id,
            Debit = Math.Round(debit, 2),
            Credit = Math.Round(credit, 2),
            Description = description
        });
    }

    private static Account GetAccount(Dictionary<string, Account> accounts, string code)
    {
        if (accounts.TryGetValue(code, out var a)) return a;
        throw new InvalidOperationException($"Cuenta PUC {code} no encontrada. Verifique la siembra.");
    }

    private async Task EnsureAccountsAsync(Dictionary<string, Account> accounts)
    {
        // Cuentas padre (clase 4) que pueden faltar en el DbSeeder original.
        var parents = new Dictionary<string, (string Name, AccountType Type, string ParentCode)>
        {
            ["13"]   = ("Deudores",                              AccountType.Activo,  "1"),
            ["1305"] = ("Clientes",                              AccountType.Activo,  "13"),
            ["1355"] = ("Retención en la fuente",                AccountType.Activo,  "13"),
            ["24"]   = ("Impuestos, gravámenes y tasas",         AccountType.Pasivo,  "2"),
            ["2408"] = ("IVA generado",                          AccountType.Pasivo,  "24"),
        };
        var needed = new Dictionary<string, (string Name, AccountType Type, string ParentCode)>
        {
            ["111005"] = ("Caja general",                         AccountType.Activo,  "1110"),
            ["111010"] = ("Bancos",                               AccountType.Activo,  "1110"),
            ["130505"] = ("Clientes nacionales",                  AccountType.Activo,  "1305"),
            ["135515"] = ("Retención en la fuente por compras",   AccountType.Activo,  "1355"),
            ["143505"] = ("Mercancías no fabricadas — producto terminado", AccountType.Activo, "1435"),
            ["240805"] = ("IVA generado en ventas 19%",           AccountType.Pasivo,  "2408"),
            ["413595"] = ("Comercio al por menor — otros",        AccountType.Ingreso, "4135"),
            ["613595"] = ("Costo de ventas — comercio al por menor", AccountType.Gasto, "6135"),
        };
        var all = new Dictionary<string, (string Name, AccountType Type, string ParentCode)>();
        foreach (var kv in parents) all[kv.Key] = kv.Value;
        foreach (var kv in needed) all[kv.Key] = kv.Value;

        // Ordenar por longitud de código ascendente para crear padres antes que hijos.
        foreach (var kv in all.OrderBy(k => k.Key.Length))
        {
            if (accounts.ContainsKey(kv.Key)) continue;
            var existing = await _db.Accounts.FirstOrDefaultAsync(a => a.Code == kv.Key);
            if (existing != null)
            {
                accounts[kv.Key] = existing;
                continue;
            }
            var parent = await _db.Accounts.FirstOrDefaultAsync(a => a.Code == kv.Value.ParentCode);
            var acc = new Account
            {
                Code = kv.Key,
                Name = kv.Value.Name,
                Type = kv.Value.Type,
                ParentId = parent?.Id,
                IsActive = true
            };
            _db.Accounts.Add(acc);
            accounts[kv.Key] = acc;
        }
        await _db.SaveChangesAsync();
    }
}
