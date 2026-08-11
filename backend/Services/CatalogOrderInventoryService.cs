using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

public class CatalogOrderInventoryService(AppDbContext db, InventoryValuationService valuation)
{
    public record StockDeductionResult(
        bool Processed,
        List<string> Deducted,
        List<string> Warnings,
        decimal CostOfGoodsSold = 0m);

    /// <summary>
    /// Descuenta inventario cuando un pedido web pasa a pagado.
    /// Idempotente: no vuelve a descontar si ya se procesó.
    /// </summary>
    public async Task<StockDeductionResult> TryDeductStockForPaidOrderAsync(Order order)
    {
        if (order.StockDeductedAt.HasValue)
            return new StockDeductionResult(true, [], [], 0m);

        await db.Entry(order).Collection(o => o.Items).LoadAsync();
        if (order.Items.Count == 0)
        {
            order.StockDeductedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return new StockDeductionResult(true, [], [], 0m);
        }

        var catalogIds = order.Items.Select(i => i.CatalogProductId).Distinct().ToList();
        var catalogById = await db.CatalogProducts
            .Where(c => catalogIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var warnings = new List<string>();
        var linesToDeduct = new List<(OrderItem Item, Product Product)>();

        foreach (var item in order.Items)
        {
            if (!catalogById.TryGetValue(item.CatalogProductId, out var catalog))
            {
                warnings.Add($"{item.ProductTitle}: ítem de catálogo no encontrado");
                continue;
            }

            if (!catalog.InternalProductId.HasValue)
            {
                warnings.Add($"{item.ProductTitle}: sin producto de inventario vinculado (no descuenta stock)");
                continue;
            }

            var product = await db.Products.FindAsync(catalog.InternalProductId.Value);
            if (product == null)
            {
                warnings.Add($"{item.ProductTitle}: producto inventario #{catalog.InternalProductId} no existe");
                return new StockDeductionResult(false, [], warnings);
            }

            if (product.Stock < item.Quantity)
            {
                warnings.Add(
                    $"{item.ProductTitle}: stock insuficiente en {product.Sku} (disponible {product.Stock}, pedido {item.Quantity})");
                return new StockDeductionResult(false, [], warnings);
            }

            linesToDeduct.Add((item, product));
        }

        var deducted = new List<string>();
        decimal totalCmv = 0m;
        foreach (var (item, product) in linesToDeduct)
        {
            var stockBefore = product.Stock;
            var vRes = await valuation.OnSaleAsync(product, item.Quantity, order.OrderNumber);
            totalCmv += vRes.TotalCmv;
            await db.Entry(product).ReloadAsync();

            db.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = product.Id,
                Type = MovementType.Salida,
                Quantity = item.Quantity,
                StockBefore = stockBefore,
                StockAfter = product.Stock,
                Reference = order.OrderNumber,
                Notes = $"Pedido web: {item.ProductTitle}"
            });

            deducted.Add($"{product.Sku} ×{item.Quantity}");
        }

        order.StockDeductedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return new StockDeductionResult(true, deducted, warnings, Math.Round(totalCmv, 2));
    }
}
