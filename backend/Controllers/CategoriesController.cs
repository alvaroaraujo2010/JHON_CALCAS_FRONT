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
public class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll([FromQuery] bool? activeOnly = true)
    {
        var q = db.Categories.AsQueryable();
        if (activeOnly == true) q = q.Where(c => c.IsActive);
        var list = await q.OrderBy(c => c.Name).Select(c => new CategoryDto(
            c.Id, c.Name, c.Description, c.IsActive, c.Products.Count(p => p.IsActive))).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> Get(int id)
    {
        var c = await db.Categories.Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        return Ok(new CategoryDto(c.Id, c.Name, c.Description, c.IsActive, c.Products.Count(p => p.IsActive)));
    }

    [HttpPost]
    [Authorize(Policy = "categories.manage")]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryRequest req)
    {
        var c = new Category { Name = req.Name, Description = req.Description, IsActive = req.IsActive };
        db.Categories.Add(c);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = c.Id }, new CategoryDto(c.Id, c.Name, c.Description, c.IsActive, 0));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "categories.manage")]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] CategoryRequest req)
    {
        var c = await db.Categories.FindAsync(id);
        if (c == null) return NotFound();
        c.Name = req.Name;
        c.Description = req.Description;
        c.IsActive = req.IsActive;
        await db.SaveChangesAsync();
        var count = await db.Products.CountAsync(p => p.CategoryId == id && p.IsActive);
        return Ok(new CategoryDto(c.Id, c.Name, c.Description, c.IsActive, count));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "categories.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Categories.FindAsync(id);
        if (c == null) return NotFound();
        if (await db.Products.AnyAsync(p => p.CategoryId == id))
            return BadRequest(new { message = "No se puede eliminar: tiene productos asociados" });
        db.Categories.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
