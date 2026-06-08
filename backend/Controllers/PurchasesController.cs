using System.Security.Claims;
using ContaNexo.API.Data;
using ContaNexo.API.DTOs;
using ContaNexo.API.Models;
using ContaNexo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchasesController(
    AppDbContext db,
    InventoryValuationService valuation,
    PurchasesAccountingService purchasesAccounting) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PurchaseDto>>> GetAll()
    {
        var list = await db.Purchases.Include(p => p.Supplier).Include(p => p.Details).ThenInclude(d => d.Product)
            .OrderByDescending(p => p.PurchaseDate).ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseDto>> Get(int id)
    {
        var p = await db.Purchases.Include(x => x.Supplier).Include(x => x.Details).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        return Ok(ToDto(p));
    }

    [HttpPost]
    [Authorize(Policy = "purchases.create")]
    public async Task<ActionResult<PurchaseDto>> Create([FromBody] CreatePurchaseRequest req)
    {
        if (!req.Details.Any()) return BadRequest(new { message = "Debe incluir al menos un detalle" });
        var supplier = await db.Suppliers.FindAsync(req.SupplierId);
        if (supplier == null) return BadRequest(new { message = "Proveedor no encontrado" });

        var count = await db.Purchases.CountAsync() + 1;
        var purchase = new Purchase
        {
            DocumentNumber = $"COMP-{DateTime.UtcNow:yyyyMM}-{count:D4}",
            SupplierId = req.SupplierId,
            PurchaseDate = req.PurchaseDate ?? DateTime.UtcNow,
            Notes = req.Notes,
            CreatedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        };

        decimal subtotal = 0;
        foreach (var d in req.Details)
        {
            var product = await db.Products.FindAsync(d.ProductId);
            if (product == null) return BadRequest(new { message = $"Producto {d.ProductId} no encontrado" });
            var line = d.Quantity * d.UnitCost;
            subtotal += line;
            purchase.Details.Add(new PurchaseDetail
            {
                ProductId = d.ProductId, Quantity = d.Quantity, UnitCost = d.UnitCost, LineTotal = line
            });

            // El servicio de valoración crea el lote y (si el producto es promedio ponderado)
            // recalcula el UnitCost; si es PEPS mantiene el costo en el lote.
            await valuation.OnPurchaseAsync(product, d.Quantity, d.UnitCost, null, purchase.DocumentNumber);

            db.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = product.Id, Type = MovementType.Entrada, Quantity = d.Quantity,
                StockBefore = product.Stock - d.Quantity, StockAfter = product.Stock,
                Reference = purchase.DocumentNumber, Notes = "Compra registrada"
            });
        }

        purchase.Subtotal = subtotal;
        purchase.Tax = subtotal * req.TaxRate;
        purchase.Total = purchase.Subtotal + purchase.Tax;
        db.Purchases.Add(purchase);
        await db.SaveChangesAsync();
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await purchasesAccounting.CreateJournalEntryForPurchaseAsync(purchase, supplier, userId.ToString());
        await db.Entry(purchase).Reference(p => p.Supplier).LoadAsync();
        await db.Entry(purchase).Collection(p => p.Details).Query().Include(d => d.Product).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = purchase.Id }, ToDto(purchase));
    }

    private static PurchaseDto ToDto(Purchase p) => new(
        p.Id, p.DocumentNumber, p.SupplierId, p.Supplier?.Name ?? "", p.PurchaseDate,
        p.Subtotal, p.Tax, p.Total, p.Status, p.Notes,
        p.Details.Select(d => new PurchaseDetailDto(d.ProductId, d.Product?.Name ?? "", d.Quantity, d.UnitCost, d.LineTotal)).ToList());
}
