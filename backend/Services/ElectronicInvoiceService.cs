using System.Security.Cryptography;
using System.Text;
using ContaNexo.API.Data;
using ContaNexo.API.DTOs;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

public class ElectronicInvoiceService(AppDbContext db)
{
    public async Task<ElectronicInvoiceDto?> GetAsync(int saleId)
    {
        var sale = await LoadSaleAsync(saleId);
        if (sale == null) return null;
        var company = await db.CompanySettings.FirstAsync();
        return ToDto(sale, company);
    }

    public async Task<(ElectronicInvoiceDto? Invoice, string? Error)> EmitAsync(int saleId, int userId)
    {
        var sale = await LoadSaleAsync(saleId);
        if (sale == null) return (null, "Venta no encontrada");
        if (sale.ElectronicInvoiceStatus == "Emitida")
            return (null, "La factura electronica ya fue emitida");

        var company = await db.CompanySettings.FirstAsync();
        var feCount = await db.Sales.CountAsync(s => s.ElectronicInvoiceStatus == "Emitida") + 1;
        sale.ElectronicInvoiceNumber = $"FE-{DateTime.UtcNow:yyyy}-{(feCount):D5}";
        sale.ElectronicInvoiceIssuedAt = DateTime.UtcNow;
        sale.ElectronicInvoiceStatus = "Emitida";
        sale.Cufe = BuildCufe(sale, company);

        await CreateAccountingEntryForSaleAsync(sale, userId);
        await db.SaveChangesAsync();

        return (ToDto(sale, company), null);
    }

    private async Task<Sale?> LoadSaleAsync(int saleId) =>
        await db.Sales
            .Include(s => s.Customer)
            .Include(s => s.Details)
            .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(s => s.Id == saleId);

    private static string BuildCufe(Sale sale, CompanySettings company)
    {
        var raw = string.Join("|",
            company.TaxId ?? "900000000",
            sale.ElectronicInvoiceNumber,
            sale.SaleDate.ToString("yyyy-MM-dd"),
            sale.Total.ToString("F2"),
            sale.Subtotal.ToString("F2"),
            sale.Tax.ToString("F2"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task CreateAccountingEntryForSaleAsync(Sale sale, int userId)
    {
        if (await db.JournalEntries.AnyAsync(j => j.Reference == sale.ElectronicInvoiceNumber))
            return;

        var caja = await db.Accounts.FirstOrDefaultAsync(a => a.Code == "1105");
        var ingresos = await db.Accounts.FirstOrDefaultAsync(a => a.Code == "4135");
        if (caja == null || ingresos == null) return;

        var count = await db.JournalEntries.CountAsync() + 1;
        var entry = new JournalEntry
        {
            EntryNumber = $"AST-{DateTime.UtcNow:yyyyMM}-{count:D4}",
            EntryDate = sale.ElectronicInvoiceIssuedAt ?? DateTime.UtcNow,
            Description = $"Venta facturada {sale.ElectronicInvoiceNumber}",
            Reference = sale.ElectronicInvoiceNumber,
            Status = "Registrado",
            CreatedByUserId = userId,
            Lines =
            [
                new JournalEntryLine
                {
                    AccountId = caja.Id,
                    Debit = sale.Total,
                    Credit = 0,
                    Description = "Ingreso por venta"
                },
                new JournalEntryLine
                {
                    AccountId = ingresos.Id,
                    Debit = 0,
                    Credit = sale.Total,
                    Description = "Ingreso operacional"
                }
            ]
        };
        db.JournalEntries.Add(entry);
    }

    public static ElectronicInvoiceDto ToDto(Sale sale, CompanySettings company)
    {
        var taxRate = sale.Subtotal > 0 ? sale.Tax / sale.Subtotal : 0m;
        return new ElectronicInvoiceDto(
            sale.Id,
            sale.DocumentNumber,
            sale.ElectronicInvoiceNumber,
            sale.ElectronicInvoiceStatus,
            sale.Cufe,
            sale.ElectronicInvoiceIssuedAt,
            company.BusinessName,
            company.TaxId,
            company.Address,
            company.Phone,
            company.Email,
            sale.Customer?.Name ?? "Consumidor final",
            sale.Customer?.TaxId,
            sale.SaleDate,
            sale.PaymentMethod,
            sale.Subtotal,
            sale.Tax,
            taxRate,
            sale.Total,
            sale.Details.Select(d => new ElectronicInvoiceLineDto(
                d.Product?.Name ?? "",
                d.Quantity,
                d.UnitPrice,
                d.LineTotal)).ToList());
    }
}
