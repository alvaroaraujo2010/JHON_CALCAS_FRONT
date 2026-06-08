using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

/// <summary>
/// Genera los comprobantes contables de compras. Plan de cuentas PUC:
///   - 143505   Mercancía en bodega (Débito, subtotal sin IVA)
///   - 24080501 IVA descontable 19%  (Débito, si hay IVA)
///   - 2205      Proveedores nacionales (Crédito, total = subtotal + IVA)
///
/// Cuando aplica: 135515 ReteFuente / 2367 ReteIVA (Crédito, pendiente de extender
/// si el proveedor es retenido en la fuente por practicar retención a la empresa).
/// </summary>
public class PurchasesAccountingService
{
    private readonly AppDbContext _db;

    public PurchasesAccountingService(AppDbContext db) => _db = db;

    public async Task<JournalEntry> CreateJournalEntryForPurchaseAsync(
        Purchase purchase, Supplier supplier, string userId)
    {
        var codes = new[] { "143505", "240805", "2205", "1435", "24", "22" };
        var accounts = await _db.Accounts
            .Where(a => codes.Contains(a.Code))
            .ToDictionaryAsync(a => a.Code);

        await EnsureAccountsAsync(accounts);

        var nextNumber = (await _db.JournalEntries.CountAsync()) + 1;
        var entryNumber = $"CP-{purchase.Id:D6}-{nextNumber:D4}";

        var entry = new JournalEntry
        {
            EntryNumber = entryNumber,
            EntryDate = purchase.PurchaseDate,
            Description = $"Compra {purchase.DocumentNumber} — {supplier.Name}",
            Reference = purchase.DocumentNumber,
            Status = "posted"
        };

        var lines = new List<JournalEntryLine>();

        // DÉBITO: Mercancía (subtotal sin IVA)
        AddLine(lines, accounts["143505"], purchase.Subtotal, 0,
            $"Mercancía {purchase.DocumentNumber}");

        // DÉBITO: IVA descontable (si hay)
        if (purchase.Tax > 0)
        {
            AddLine(lines, accounts["240805"], purchase.Tax, 0,
                $"IVA descontable 19% {purchase.DocumentNumber}");
        }

        // CRÉDITO: Proveedores (CxP)
        AddLine(lines, accounts["2205"], 0, purchase.Total,
            $"CxP proveedor {supplier.Name} — {purchase.DocumentNumber}");

        // Validar partida doble
        var debits = lines.Sum(l => l.Debit);
        var credits = lines.Sum(l => l.Credit);
        if (Math.Abs(debits - credits) > 0.01m)
            throw new InvalidOperationException(
                $"Asiento desbalanceado. D={debits}, C={credits}, total={purchase.Total}");

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

    private async Task EnsureAccountsAsync(Dictionary<string, Account> accounts)
    {
        // Padres
        var parents = new Dictionary<string, (string Name, AccountType Type, string ParentCode)>
        {
            ["14"]  = ("Inventarios", AccountType.Activo, "1"),
            ["1435"] = ("Mercancías no fabricadas por la empresa", AccountType.Activo, "14"),
            ["22"]  = ("Proveedores", AccountType.Pasivo, "2"),
            ["2205"] = ("Proveedores nacionales", AccountType.Pasivo, "22"),
        };
        // Hijas
        var needed = new Dictionary<string, (string Name, AccountType Type, string ParentCode)>
        {
            ["143505"] = ("Mercancía en bodega", AccountType.Activo, "1435"),
            ["240805"] = ("IVA descontable en compras 19%", AccountType.Activo, "24"),
        };
        var all = new Dictionary<string, (string Name, AccountType Type, string ParentCode)>();
        foreach (var kv in parents) all[kv.Key] = kv.Value;
        foreach (var kv in needed) all[kv.Key] = kv.Value;

        // Ordenar por longitud de código ascendente: padres primero.
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
