using System.Security.Claims;
using ContaNexo.API.Data;
using ContaNexo.API.DTOs;
using ContaNexo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController(AppDbContext db) : ControllerBase
{
    [HttpGet("movements")]
    public async Task<ActionResult<List<InventoryMovementDto>>> GetMovements([FromQuery] int? productId)
    {
        var q = db.InventoryMovements.Include(m => m.Product).AsQueryable();
        if (productId.HasValue) q = q.Where(m => m.ProductId == productId);
        var list = await q.OrderByDescending(m => m.CreatedAt).Take(200).ToListAsync();
        return Ok(list.Select(m => new InventoryMovementDto(
            m.Id, m.ProductId, m.Product?.Name ?? "", m.Type.ToString(), m.Quantity,
            m.StockBefore, m.StockAfter, m.Reference, m.Notes, m.CreatedAt)).ToList());
    }

    [HttpPost("adjust")]
    [Authorize(Roles = "Administrador,Almacen")]
    public async Task<ActionResult<InventoryMovementDto>> Adjust([FromBody] AdjustInventoryRequest req)
    {
        var product = await db.Products.FindAsync(req.ProductId);
        if (product == null) return NotFound();

        if (!Enum.TryParse<MovementType>(req.Type, true, out var type))
            return BadRequest(new { message = "Tipo inválido: Entrada, Salida o Ajuste" });

        var before = product.Stock;
        int after = type switch
        {
            MovementType.Entrada => before + req.Quantity,
            MovementType.Salida => before - req.Quantity,
            MovementType.Ajuste => req.Quantity,
            _ => before
        };
        if (after < 0) return BadRequest(new { message = "El stock no puede ser negativo" });

        product.Stock = after;
        product.UpdatedAt = DateTime.UtcNow;
        var movement = new InventoryMovement
        {
            ProductId = product.Id, Type = type, Quantity = req.Quantity,
            StockBefore = before, StockAfter = after, Reference = "AJUSTE-MANUAL", Notes = req.Notes,
            UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        };
        db.InventoryMovements.Add(movement);
        await db.SaveChangesAsync();
        return Ok(new InventoryMovementDto(movement.Id, product.Id, product.Name, type.ToString(),
            req.Quantity, before, after, movement.Reference, movement.Notes, movement.CreatedAt));
    }

    [HttpGet("kardex/{productId}")]
    public async Task<ActionResult<List<InventoryMovementDto>>> Kardex(int productId)
    {
        var product = await db.Products.FindAsync(productId);
        if (product == null) return NotFound();
        var list = await db.InventoryMovements.Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.CreatedAt).ToListAsync();
        return Ok(list.Select(m => new InventoryMovementDto(
            m.Id, m.ProductId, product.Name, m.Type.ToString(), m.Quantity,
            m.StockBefore, m.StockAfter, m.Reference, m.Notes, m.CreatedAt)).ToList());
    }
}
