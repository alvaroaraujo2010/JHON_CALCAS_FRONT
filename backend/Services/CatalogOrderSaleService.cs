using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

/// <summary>
/// Registra en el módulo de Ventas (ERP) un pedido web pagado,
/// para que aparezca en dashboard, listado de ventas y contabilidad.
/// Idempotente vía <see cref="Order.SaleId"/>.
/// </summary>
public class CatalogOrderSaleService(
    AppDbContext db,
    SalesAccountingService salesAccounting)
{
    public record SaleRegistrationResult(bool Created, int? SaleId, string? DocumentNumber, List<string> Warnings);

    public async Task<SaleRegistrationResult> TryRegisterSaleForPaidOrderAsync(
        Order order,
        decimal costOfGoodsSold = 0m)
    {
        if (order.SaleId.HasValue)
            return new SaleRegistrationResult(false, order.SaleId, null, []);

        await db.Entry(order).Collection(o => o.Items).LoadAsync();

        var warnings = new List<string>();
        var customer = await FindOrCreateCustomerAsync(order);

        var catalogIds = order.Items.Select(i => i.CatalogProductId).Distinct().ToList();
        var catalogById = catalogIds.Count == 0
            ? new Dictionary<int, CatalogProduct>()
            : await db.CatalogProducts.Where(c => catalogIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id);

        var details = new List<SaleDetail>();
        foreach (var item in order.Items)
        {
            if (!catalogById.TryGetValue(item.CatalogProductId, out var catalog) ||
                !catalog.InternalProductId.HasValue)
            {
                warnings.Add($"{item.ProductTitle}: sin vínculo a inventario (solo suma al total de la venta)");
                continue;
            }

            details.Add(new SaleDetail
            {
                ProductId = catalog.InternalProductId.Value,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            });
        }

        // Evitar choque con numeración VTA- del módulo de ventas POS
        var docNumber = order.OrderNumber;
        if (await db.Sales.AnyAsync(s => s.DocumentNumber == docNumber))
            docNumber = $"WEB-{order.OrderNumber}";

        var paymentMethod = NormalizePaymentMethod(order.PaymentMethod);
        var sale = new Sale
        {
            DocumentNumber = docNumber,
            CustomerId = customer?.Id,
            SaleDate = order.UpdatedAt != default ? order.UpdatedAt : DateTime.UtcNow,
            PaymentMethod = paymentMethod,
            Status = "Completada",
            Notes = BuildNotes(order),
            Subtotal = order.Total,
            Tax = 0,
            Total = order.Total,
            CreatedByUserId = null
        };

        foreach (var d in details)
            sale.Details.Add(d);

        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        order.SaleId = sale.Id;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        try
        {
            await salesAccounting.CreateJournalEntryForSaleAsync(
                sale, customer, Math.Round(costOfGoodsSold, 2), "web");
        }
        catch
        {
            warnings.Add("Venta creada pero el asiento contable no se pudo generar (revise el plan de cuentas)");
        }

        return new SaleRegistrationResult(true, sale.Id, sale.DocumentNumber, warnings);
    }

    private async Task<Customer?> FindOrCreateCustomerAsync(Order order)
    {
        var email = (order.CustomerEmail ?? "").Trim();
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(order.CustomerName))
            return null;

        Customer? customer = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            customer = await db.Customers
                .FirstOrDefaultAsync(c => c.Email != null && c.Email.ToLower() == email.ToLower());
        }

        if (customer != null)
        {
            var dirty = false;
            if (string.IsNullOrWhiteSpace(customer.Phone) && !string.IsNullOrWhiteSpace(order.CustomerPhone))
            {
                customer.Phone = order.CustomerPhone;
                dirty = true;
            }
            if (string.IsNullOrWhiteSpace(customer.Address) && !string.IsNullOrWhiteSpace(order.CustomerAddress))
            {
                customer.Address = order.CustomerAddress;
                dirty = true;
            }
            if (dirty) await db.SaveChangesAsync();
            return customer;
        }

        customer = new Customer
        {
            Name = string.IsNullOrWhiteSpace(order.CustomerName) ? email : order.CustomerName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            Phone = order.CustomerPhone,
            Address = string.IsNullOrWhiteSpace(order.CustomerAddress)
                ? null
                : $"{order.CustomerAddress}{(string.IsNullOrWhiteSpace(order.City) ? "" : $", {order.City}")}",
            IsActive = true
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private static string NormalizePaymentMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return "MercadoPago";
        var m = method.Trim().ToLowerInvariant();
        if (m is "account_money" or "credit_card" or "debit_card" or "pse" or "ticket" or "bank_transfer")
            return "MercadoPago";
        return method.Trim();
    }

    private static string BuildNotes(Order order)
    {
        var parts = new List<string> { $"Pedido web {order.OrderNumber}" };
        if (!string.IsNullOrWhiteSpace(order.CustomerAddress))
            parts.Add($"Envío: {order.CustomerAddress}");
        if (!string.IsNullOrWhiteSpace(order.City))
            parts.Add(order.City!);
        if (!string.IsNullOrWhiteSpace(order.Notes))
            parts.Add(order.Notes!);
        return string.Join(" · ", parts);
    }
}
