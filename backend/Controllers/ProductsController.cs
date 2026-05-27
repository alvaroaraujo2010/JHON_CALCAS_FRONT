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
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll([FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] bool? lowStock)
    {
        var q = db.Products.Include(p => p.Category).Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId);
        if (lowStock == true) q = q.Where(p => p.Stock <= p.MinStock);
        var list = await q.OrderBy(p => p.Name).ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> Get(int id)
    {
        var p = await db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        return Ok(ToDto(p));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,Almacen")]
    public async Task<ActionResult<ProductDto>> Create([FromBody] ProductRequest req)
    {
        if (await db.Products.AnyAsync(p => p.Sku == req.Sku))
            return BadRequest(new { message = "SKU ya existe" });
        var p = new Product
        {
            Sku = req.Sku, Name = req.Name, Description = req.Description, CategoryId = req.CategoryId,
            UnitCost = req.UnitCost, UnitPrice = req.UnitPrice, MinStock = req.MinStock, Unit = req.Unit, IsActive = req.IsActive
        };
        db.Products.Add(p);
        await db.SaveChangesAsync();
        await db.Entry(p).Reference(x => x.Category).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = p.Id }, ToDto(p));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador,Almacen")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] ProductRequest req)
    {
        var p = await db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (await db.Products.AnyAsync(x => x.Sku == req.Sku && x.Id != id))
            return BadRequest(new { message = "SKU ya existe" });
        p.Sku = req.Sku; p.Name = req.Name; p.Description = req.Description; p.CategoryId = req.CategoryId;
        p.UnitCost = req.UnitCost; p.UnitPrice = req.UnitPrice; p.MinStock = req.MinStock;
        p.Unit = req.Unit; p.IsActive = req.IsActive; p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(p));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.Products.FindAsync(id);
        if (p == null) return NotFound();
        p.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Sku, p.Name, p.Description, p.CategoryId, p.Category?.Name ?? "",
        p.UnitCost, p.UnitPrice, p.Stock, p.MinStock, p.Unit, p.IsActive, p.Stock <= p.MinStock);
}
