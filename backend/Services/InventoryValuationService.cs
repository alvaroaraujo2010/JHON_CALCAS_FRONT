using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

/// <summary>
/// Servicio de valoración de inventarios. Implementa:
///   - Promedio Ponderado Móvil (recomendado fiscalmente, Art. 65 ET).
///   - PEPS / FIFO (también aceptado).
///
/// Uso:
///   1) <see cref="OnPurchaseAsync"/> al registrar una compra.
///   2) <see cref="OnSaleAsync"/> al registrar una venta (devuelve el CMV).
///   3) <see cref="OnAdjustmentAsync"/> para ajustes manuales (daño, vencimiento, etc.).
/// </summary>
public class InventoryValuationService
{
    private readonly AppDbContext _db;

    public InventoryValuationService(AppDbContext db) => _db = db;

    public record ValuationResult(decimal TotalCmv, List<LotConsumption> Consumptions);
    public record LotConsumption(int LotId, int Quantity, decimal UnitCost, decimal Subtotal);

    /// <summary>
    /// Procesa el ingreso de mercadería por compra: crea un nuevo lote y, si el
    /// producto está configurado como promedio ponderado, recalcula el UnitCost
    /// del producto con la nueva mezcla.
    /// </summary>
    public async Task<InventoryLot> OnPurchaseAsync(
        Product product, int quantity, decimal unitCost, int? purchaseId, string? reference)
    {
        var lot = new InventoryLot
        {
            ProductId = product.Id,
            PurchaseId = purchaseId,
            OriginalQuantity = quantity,
            RemainingQuantity = quantity,
            UnitCost = unitCost,
            EntryDate = DateTime.UtcNow,
            Reference = reference
        };
        _db.InventoryLots.Add(lot);

        if (product.ValuationMethod == InventoryValuationMethod.WeightedAverage)
        {
            // Promedio ponderado móvil: new = (old_stock * old_cost + new_qty * new_cost) / (old + new)
            var currentValue = product.Stock * product.UnitCost;
            var newValue = quantity * unitCost;
            var newStock = product.Stock + quantity;
            product.UnitCost = newStock > 0 ? Math.Round((currentValue + newValue) / newStock, 2) : unitCost;
        }
        product.Stock += quantity;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return lot;
    }

    /// <summary>
    /// Procesa la salida por venta. Devuelve el CMV total y el detalle por lote.
    /// Aplica el método configurado en el producto.
    /// </summary>
    public async Task<ValuationResult> OnSaleAsync(Product product, int quantity, string? reference)
    {
        if (quantity <= 0) return new ValuationResult(0, new());
        if (product.Stock < quantity)
            throw new InvalidOperationException(
                $"Stock insuficiente para {product.Name}. Disponible: {product.Stock}, solicitado: {quantity}");

        var consumptions = new List<LotConsumption>();
        decimal totalCmv = 0;
        int remaining = quantity;

        if (product.ValuationMethod == InventoryValuationMethod.FIFO)
        {
            // PEPS: consumir primero los lotes más antiguos.
            var lots = await _db.InventoryLots
                .Where(l => l.ProductId == product.Id && l.RemainingQuantity > 0)
                .OrderBy(l => l.EntryDate).ThenBy(l => l.Id)
                .ToListAsync();

            foreach (var lot in lots)
            {
                if (remaining == 0) break;
                var take = Math.Min(lot.RemainingQuantity, remaining);
                var subtotal = Math.Round(take * lot.UnitCost, 2);
                lot.RemainingQuantity -= take;
                consumptions.Add(new LotConsumption(lot.Id, take, lot.UnitCost, subtotal));
                totalCmv += subtotal;
                remaining -= take;
            }
        }
        else
        {
            // Promedio Ponderado: el CMV se calcula contra el UnitCost actual del producto.
            // Se consume como un solo bloque conceptual.
            var take = quantity;
            var subtotal = Math.Round(take * product.UnitCost, 2);
            consumptions.Add(new LotConsumption(0, take, product.UnitCost, subtotal));
            totalCmv += subtotal;
            remaining = 0;

            // En promedio ponderado, el UnitCost NO cambia con la venta (solo con la compra).
            // Se reduce el stock y se mantienen los lotes intactos; el siguiente cálculo
            // de promedio se hará con las compras futuras.
        }

        product.Stock -= quantity;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new ValuationResult(Math.Round(totalCmv, 2), consumptions);
    }

    /// <summary>
    /// Procesa un ajuste manual de inventario (entrada o salida sin documento).
    /// Para promedio ponderado, una entrada ajusta el costo; una salida usa el costo actual.
    /// </summary>
    public async Task<decimal> OnAdjustmentAsync(Product product, int deltaQuantity, decimal? unitCost, string? notes)
    {
        if (deltaQuantity == 0) return 0;
        if (deltaQuantity > 0)
        {
            // Entrada: crea un lote con el costo provisto (o 0 si no se da).
            await OnPurchaseAsync(product, deltaQuantity, unitCost ?? product.UnitCost, null,
                notes ?? "Ajuste manual de inventario");
            return Math.Round(deltaQuantity * (unitCost ?? product.UnitCost), 2);
        }
        else
        {
            // Salida: usa el método configurado.
            var res = await OnSaleAsync(product, -deltaQuantity, notes ?? "Ajuste manual de inventario");
            return res.TotalCmv;
        }
    }

    /// <summary>Genera un reporte kardex para un producto en un rango de fechas.</summary>
    public async Task<List<KardexEntry>> GetKardexAsync(int productId, DateTime? from = null, DateTime? to = null)
    {
        var product = await _db.Products.FindAsync(productId)
            ?? throw new ArgumentException($"Producto {productId} no existe");
        var lots = await _db.InventoryLots
            .Where(l => l.ProductId == productId)
            .OrderBy(l => l.EntryDate).ThenBy(l => l.Id)
            .ToListAsync();

        // Kardex simplificado: una línea por lote con su estado actual.
        return lots.Select(l => new KardexEntry(
            l.Id, l.EntryDate, l.OriginalQuantity, l.RemainingQuantity,
            l.OriginalQuantity - l.RemainingQuantity, l.UnitCost,
            l.RemainingQuantity * l.UnitCost,
            l.Reference ?? "")).ToList();
    }

    public record KardexEntry(
        int LotId, DateTime EntryDate, int OriginalQuantity, int RemainingQuantity,
        int ConsumedQuantity, decimal UnitCost, decimal BalanceValue, string Reference);
}
