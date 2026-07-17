using ContaNexo.API.Data;
using ContaNexo.API.Models;
using ContaNexo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderController(
    AppDbContext db,
    MercadoPagoService mp,
    CatalogOrderInventoryService catalogInventory) : ControllerBase
{
    private static readonly HashSet<string> PaidStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "paid", "approved" };
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest req)
    {
        if (!req.Items.Any())
            return BadRequest(new { message = "Debe incluir al menos un producto" });

        var count = await db.Orders.CountAsync() + 1;
        var orderNumber = $"JHC-{DateTime.UtcNow:yyyyMM}-{count:D4}";

        decimal subtotal = 0;
        var items = new List<OrderItem>();
        foreach (var i in req.Items)
        {
            var product = await db.CatalogProducts.FindAsync(i.CatalogProductId);
            if (product == null)
                return BadRequest(new { message = $"Producto {i.CatalogProductId} no encontrado" });
            if (!product.IsActive)
                return BadRequest(new { message = $"Producto {product.Title} no está disponible" });

            var lineTotal = Math.Round(product.Price * i.Quantity, 2);
            subtotal += lineTotal;
            items.Add(new OrderItem
            {
                CatalogProductId = product.Id,
                ProductTitle = product.Title,
                Quantity = i.Quantity,
                UnitPrice = product.Price,
                LineTotal = lineTotal
            });
        }

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerName = req.CustomerName,
            CustomerEmail = req.CustomerEmail,
            CustomerPhone = req.CustomerPhone,
            CustomerAddress = req.CustomerAddress,
            City = req.City,
            Department = req.Department,
            Notes = req.Notes,
            Subtotal = Math.Round(subtotal, 2),
            ShippingCost = 0,
            Total = Math.Round(subtotal, 2),
            Status = "pending"
        };
        order.Items = items;

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Crear preferencia Mercado Pago
        var baseUrl = await mp.GetBaseUrlAsync();
        var mpItems = items.Select(i => new MercadoPagoService.MpItem(
            i.ProductTitle, i.Quantity, i.UnitPrice,
            $"/api/catalog/products/{i.CatalogProductId}"
        )).ToList();

        try
        {
            var pref = await mp.CreatePreferenceAsync(orderNumber, mpItems, baseUrl, order.Id);
            order.MpPaymentId = pref.Id;
            await db.SaveChangesAsync();

            return Ok(new OrderDto(
                order.Id, order.OrderNumber, order.CustomerName, order.CustomerEmail,
                order.CustomerPhone, order.CustomerAddress, order.City, order.Department,
                order.Subtotal, order.ShippingCost, order.Total, order.Status,
                pref.Id, pref.InitPoint, order.CreatedAt, null,
                items.Select(i => new OrderItemDto(
                    i.CatalogProductId, i.ProductTitle, i.Quantity, i.UnitPrice, i.LineTotal)).ToList()
            ));
        }
        catch (Exception)
        {
            // Si falla MP, la orden queda como pending sin preferencia
            return Ok(new OrderDto(
                order.Id, order.OrderNumber, order.CustomerName, order.CustomerEmail,
                order.CustomerPhone, order.CustomerAddress, order.City, order.Department,
                order.Subtotal, order.ShippingCost, order.Total, "pending_error",
                null, null, order.CreatedAt, null,
                items.Select(i => new OrderItemDto(
                    i.CatalogProductId, i.ProductTitle, i.Quantity, i.UnitPrice, i.LineTotal)).ToList()
            ));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> Get(int id)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        return Ok(ToDto(order));
    }

    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmPayment(int id, [FromQuery] string? mpPaymentId, [FromQuery] string? status)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();


        if (!string.IsNullOrWhiteSpace(mpPaymentId))
        {
            var info = await mp.GetPaymentInfoAsync(mpPaymentId);
            if (info != null)
            {
                order.MpPaymentId = mpPaymentId;
                order.MpPaymentStatus = info.Status;
                order.PaymentMethod = info.PaymentMethodId;
                order.Status = info.Status switch
                {
                    "approved" => "paid",
                    "pending" => "pending",
                    "in_process" => "processing",
                    "rejected" => "cancelled",
                    _ => "pending"
                };
            }
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            order.Status = status switch
            {
                "approved" => "paid",
                "pending" => "pending",
                "failure" => "cancelled",
                _ => order.Status
            };
        }

        order.UpdatedAt = DateTime.UtcNow;

        CatalogOrderInventoryService.StockDeductionResult? stockResult = null;
        if (IsPaidStatus(order.Status) && !order.StockDeductedAt.HasValue)
            stockResult = await catalogInventory.TryDeductStockForPaidOrderAsync(order);
        else
            await db.SaveChangesAsync();

        return Ok(new
        {
            order.Id,
            order.Status,
            order.MpPaymentStatus,
            order.StockDeductedAt,
            stockDeducted = stockResult?.Deducted ?? [],
            stockWarnings = stockResult?.Warnings ?? []
        });
    }

    [HttpGet]
    [Authorize(Policy = "orders.view")]
    public async Task<ActionResult<List<OrderDto>>> GetAll()
    {
        var orders = await db.Orders.Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders.Select(ToDto).ToList());
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "orders.manage")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest req)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        order.Status = req.Status;
        order.UpdatedAt = DateTime.UtcNow;

        CatalogOrderInventoryService.StockDeductionResult? stockResult = null;
        if (IsPaidStatus(order.Status) && !order.StockDeductedAt.HasValue)
            stockResult = await catalogInventory.TryDeductStockForPaidOrderAsync(order);
        else
            await db.SaveChangesAsync();

        var dto = ToDto(order);
        if (stockResult != null && (stockResult.Warnings.Count > 0 || !stockResult.Processed))
            return Ok(new { order = dto, stockWarnings = stockResult.Warnings, stockDeducted = stockResult.Deducted, stockProcessed = stockResult.Processed });

        if (stockResult?.Deducted.Count > 0)
            return Ok(new { order = dto, stockDeducted = stockResult.Deducted, stockProcessed = true });

        return Ok(dto);
    }

    private static bool IsPaidStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && PaidStatuses.Contains(status);

    private static OrderDto ToDto(Order o) => new(
        o.Id, o.OrderNumber, o.CustomerName, o.CustomerEmail,
        o.CustomerPhone, o.CustomerAddress, o.City, o.Department,
        o.Subtotal, o.ShippingCost, o.Total, o.Status,
        o.MpPaymentId, null, o.CreatedAt, o.StockDeductedAt,
        o.Items.Select(i => new OrderItemDto(
            i.CatalogProductId, i.ProductTitle, i.Quantity, i.UnitPrice, i.LineTotal)).ToList()
    );
}

public record CreateOrderRequest(
    string CustomerName, string CustomerEmail, string CustomerPhone,
    string? CustomerAddress, string? City, string? Department, string? Notes,
    List<OrderItemRequest> Items);

public record OrderItemRequest(int CatalogProductId, int Quantity);

public record OrderDto(
    int Id, string OrderNumber, string CustomerName, string CustomerEmail,
    string CustomerPhone, string? CustomerAddress, string? City, string? Department,
    decimal Subtotal, decimal ShippingCost, decimal Total, string Status,
    string? MpPreferenceId, string? MpInitPoint, DateTime CreatedAt, DateTime? StockDeductedAt,
    List<OrderItemDto> Items);

public record OrderItemDto(int ProductId, string ProductTitle, int Quantity, decimal UnitPrice, decimal LineTotal);

public record UpdateOrderStatusRequest(string Status);
