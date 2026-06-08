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
public class SalesController(
    AppDbContext db,
    ElectronicInvoiceService electronicInvoice,
    SalesAccountingService salesAccounting,
    InventoryValuationService valuation) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SaleDto>>> GetAll()
    {
        var list = await db.Sales.Include(s => s.Customer).Include(s => s.Details).ThenInclude(d => d.Product)
            .OrderByDescending(s => s.SaleDate).ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SaleDto>> Get(int id)
    {
        var s = await db.Sales.Include(x => x.Customer).Include(x => x.Details).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        return Ok(ToDto(s));
    }

    [HttpPost]
    [Authorize(Policy = "sales.create")]
    public async Task<ActionResult<SaleDto>> Create([FromBody] CreateSaleRequest req)
    {
        if (!req.Details.Any()) return BadRequest(new { message = "Debe incluir al menos un detalle" });

        Customer? customer = null;
        if (req.CustomerId.HasValue)
        {
            customer = await db.Customers.FindAsync(req.CustomerId);
            if (customer == null) return BadRequest(new { message = "Cliente no encontrado" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var count = await db.Sales.CountAsync() + 1;
        var sale = new Sale
        {
            DocumentNumber = $"VTA-{DateTime.UtcNow:yyyyMM}-{count:D4}",
            CustomerId = req.CustomerId,
            SaleDate = req.SaleDate ?? DateTime.UtcNow,
            PaymentMethod = req.PaymentMethod,
            Notes = req.Notes,
            CreatedByUserId = int.Parse(userId)
        };

        decimal subtotal = 0;
        decimal totalCostoVenta = 0;
        foreach (var d in req.Details)
        {
            var product = await db.Products.FindAsync(d.ProductId);
            if (product == null) return BadRequest(new { message = $"Producto {d.ProductId} no encontrado" });
            if (product.Stock < d.Quantity)
                return BadRequest(new { message = $"Stock insuficiente para {product.Name}. Disponible: {product.Stock}" });

            var line = d.Quantity * d.UnitPrice;
            subtotal += line;

            // Fase 5: el servicio de valoración calcula el CMV según el método del producto
            // (PEPS consume lotes antiguos, promedio ponderado usa el UnitCost actual).
            var vRes = await valuation.OnSaleAsync(product, d.Quantity, sale.DocumentNumber);
            totalCostoVenta += vRes.TotalCmv;

            sale.Details.Add(new SaleDetail
            {
                ProductId = d.ProductId, Quantity = d.Quantity, UnitPrice = d.UnitPrice, LineTotal = line
            });

            db.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = product.Id, Type = MovementType.Salida, Quantity = d.Quantity,
                StockBefore = product.Stock + d.Quantity, StockAfter = product.Stock,
                Reference = sale.DocumentNumber, Notes = "Venta registrada"
            });
        }

        sale.Subtotal = Math.Round(subtotal, 2);
        sale.Tax = Math.Round(subtotal * req.TaxRate, 2);
        sale.Total = Math.Round(sale.Subtotal + sale.Tax, 2);
        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        // Genera comprobante contable con desglose de IVA, CxC y ReteFuente.
        await salesAccounting.CreateJournalEntryForSaleAsync(
            sale, customer, Math.Round(totalCostoVenta, 2), userId);

        if (sale.CustomerId.HasValue) await db.Entry(sale).Reference(s => s.Customer).LoadAsync();
        await db.Entry(sale).Collection(s => s.Details).Query().Include(d => d.Product).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = sale.Id }, ToDto(sale));
    }

    [HttpGet("{id}/electronic-invoice")]
    public async Task<ActionResult<ElectronicInvoiceDto>> GetElectronicInvoice(int id)
    {
        var invoice = await electronicInvoice.GetAsync(id);
        if (invoice == null) return NotFound();
        return Ok(invoice);
    }

    [HttpPost("{id}/electronic-invoice/emit")]
    [Authorize(Policy = "sales.emit_invoice")]
    public async Task<ActionResult<ElectronicInvoiceDto>> EmitElectronicInvoice(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (invoice, error) = await electronicInvoice.EmitAsync(id, userId);
        if (error != null) return BadRequest(new { message = error });
        return Ok(invoice);
    }

    private static SaleDto ToDto(Sale s) => new(
        s.Id, s.DocumentNumber, s.CustomerId, s.Customer?.Name, s.SaleDate,
        s.Subtotal, s.Tax, s.Total, s.PaymentMethod, s.Status, s.Notes,
        s.ElectronicInvoiceStatus, s.ElectronicInvoiceNumber, s.Cufe, s.ElectronicInvoiceIssuedAt,
        s.Details.Select(d => new SaleDetailDto(d.ProductId, d.Product?.Name ?? "", d.Quantity, d.UnitPrice, d.LineTotal)).ToList());
}
