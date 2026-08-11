using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

/// <summary>
/// Al pagar un pedido web: descuenta inventario y registra la venta en el ERP.
/// </summary>
public class CatalogOrderFulfillmentService(
    AppDbContext db,
    CatalogOrderInventoryService inventory,
    CatalogOrderSaleService sales)
{
    public record FulfillmentResult(
        CatalogOrderInventoryService.StockDeductionResult Stock,
        CatalogOrderSaleService.SaleRegistrationResult Sale);

    public async Task<FulfillmentResult> FulfillPaidOrderAsync(Order order)
    {
        var stock = await inventory.TryDeductStockForPaidOrderAsync(order);

        // Stock insuficiente / error bloqueante: no crear venta todavía
        if (!stock.Processed && !order.StockDeductedAt.HasValue)
        {
            return new FulfillmentResult(
                stock,
                new CatalogOrderSaleService.SaleRegistrationResult(false, order.SaleId, null, stock.Warnings));
        }

        var cmv = stock.CostOfGoodsSold;
        if (cmv == 0m && order.SaleId == null)
            cmv = await EstimateCmvAsync(order);

        var sale = await sales.TryRegisterSaleForPaidOrderAsync(order, cmv);
        return new FulfillmentResult(stock, sale);
    }

    private async Task<decimal> EstimateCmvAsync(Order order)
    {
        await db.Entry(order).Collection(o => o.Items).LoadAsync();
        var catalogIds = order.Items.Select(i => i.CatalogProductId).Distinct().ToList();
        if (catalogIds.Count == 0) return 0m;

        var catalog = await db.CatalogProducts
            .Where(c => catalogIds.Contains(c.Id) && c.InternalProductId != null)
            .ToDictionaryAsync(c => c.Id);

        decimal total = 0m;
        foreach (var item in order.Items)
        {
            if (!catalog.TryGetValue(item.CatalogProductId, out var c) || !c.InternalProductId.HasValue)
                continue;
            var product = await db.Products.FindAsync(c.InternalProductId.Value);
            if (product == null) continue;
            total += product.UnitCost * item.Quantity;
        }
        return Math.Round(total, 2);
    }
}
