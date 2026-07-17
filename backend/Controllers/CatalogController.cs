using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController(AppDbContext db, IConfiguration config) : ControllerBase
{
    private string MediaBaseUrl =>
        config["Catalog:MediaBaseUrl"]?.TrimEnd('/')
        ?? $"{Request.Scheme}://{Request.Host}";

    [HttpGet("products")]
    public async Task<ActionResult<List<CatalogProductDto>>> GetProducts(
        [FromQuery] string? productLine, [FromQuery] string? brand, [FromQuery] string? model)
    {
        var q = db.CatalogProducts.Where(p => p.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(productLine))
            q = q.Where(p => p.ProductLine == productLine);
        if (!string.IsNullOrWhiteSpace(brand))
        {
            var b = brand.Trim();
            q = q.Where(p =>
                p.Brand == b ||
                p.Brand.StartsWith(b) ||
                p.Brand.Contains(b) ||
                b.StartsWith(p.Brand));
        }
        if (!string.IsNullOrWhiteSpace(model))
        {
            var m = model.Trim();
            q = q.Where(p =>
                p.MenuModel == m ||
                p.Model == m ||
                (p.MenuModel != null && p.MenuModel.StartsWith(m)) ||
                (p.Model != null && p.Model.StartsWith(m)) ||
                (p.MenuModel != null && p.MenuModel.Contains(m)) ||
                (p.Model != null && p.Model.Contains(m)) ||
                (p.MenuModel != null && m.StartsWith(p.MenuModel)) ||
                (p.Model != null && m.StartsWith(p.Model)));
        }

        var list = await q.OrderBy(p => p.SortOrder).ThenBy(p => p.Title).ToListAsync();
        if (list.Count == 0 && !string.IsNullOrWhiteSpace(productLine))
        {
            // Si el menu trae filtros mas especificos que los datos cargados, no caemos a placeholders:
            // mostramos lo disponible para la linea solicitada.
            list = await db.CatalogProducts
                .Where(p => p.IsActive && p.ProductLine == productLine)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Title)
                .ToListAsync();
        }
        return Ok(list.Select(ToDto).ToList());
    }

    [HttpGet("products/{slug}")]
    public async Task<ActionResult<CatalogProductDto>> GetBySlug(string slug)
    {
        var p = await db.CatalogProducts.FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive);
        if (p == null) return NotFound();
        return Ok(ToDto(p));
    }

    [HttpGet("brands")]
    public async Task<ActionResult<List<BrandDto>>> GetBrands([FromQuery] string? productLine)
    {
        var q = db.CatalogProducts.Where(p => p.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(productLine))
            q = q.Where(p => p.ProductLine == productLine);

        var brands = await q.GroupBy(p => p.Brand)
            .Select(g => new BrandDto(g.Key, g.Count()))
            .OrderBy(b => b.Brand)
            .ToListAsync();
        return Ok(brands);
    }

    [HttpGet("lines")]
    public ActionResult<List<LineInfoDto>> GetLines()
    {
        var lines = new[]
        {
            new LineInfoDto("protectores-tanque", "Protectores de Tanque", "Protege el tanque de tu moto con nuestros protectores de alta adherencia"),
            new LineInfoDto("calcas-motos", "Calcas Motos", "Personaliza tu moto con nuestras calcas de alta calidad"),
            new LineInfoDto("calcas-rines", "Calcas Rines", "Dale estilo a tus rines con nuestras calcas especializadas"),
            new LineInfoDto("emblemas", "Emblemas", "Complementa tu moto con nuestros emblemas exclusivos"),
            new LineInfoDto("otros", "Otros", "Cascos y accesorios adicionales"),
        };
        return Ok(lines);
    }

    // ─── Admin CRUD ───────────────────────────────────────────────

    [HttpGet("admin")]
    [Authorize(Policy = "products.view")]
    public async Task<ActionResult<List<CatalogProductDto>>> GetAllForAdmin()
    {
        var list = await db.CatalogProducts
            .Include(p => p.InternalProduct)
            .OrderBy(p => p.ProductLine)
            .ThenBy(p => p.SortOrder)
            .ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    [HttpPost]
    [Authorize(Policy = "products.create")]
    public async Task<ActionResult<CatalogProductDto>> Create([FromForm] CatalogProductForm form)
    {
        if (form.File == null || form.File.Length == 0)
            return BadRequest(new { message = "Debe incluir una imagen" });

        var slug = await EnsureUniqueSlugAsync(GenerateSlug(form.Title));
        var fileName = await SaveImageAsync(form.File);
        var product = MapFormToEntity(new CatalogProduct(), form, slug, fileName);
        db.CatalogProducts.Add(product);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBySlug), new { slug = product.Slug }, ToDto(product));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "products.edit")]
    public async Task<ActionResult<CatalogProductDto>> Update(int id, [FromForm] CatalogProductForm form)
    {
        var p = await db.CatalogProducts.FindAsync(id);
        if (p == null) return NotFound();

        MapFormToEntity(p, form, p.Slug, p.ImageFileName);

        if (form.File != null && form.File.Length > 0)
        {
            DeleteImage(p.ImageFileName);
            p.ImageFileName = await SaveImageAsync(form.File);
        }

        p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(p));
    }

    [HttpPut("{id}/toggle-active")]
    [Authorize(Policy = "products.edit")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var p = await db.CatalogProducts.FindAsync(id);
        if (p == null) return NotFound();
        p.IsActive = !p.IsActive;
        p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(p));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "products.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.CatalogProducts.FindAsync(id);
        if (p == null) return NotFound();
        DeleteImage(p.ImageFileName);
        db.CatalogProducts.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ─── Privados ────────────────────────────────────────────────

    private CatalogProductDto ToDto(CatalogProduct p) => new(
        p.Id, p.Slug, p.ProductLine, p.Brand, p.Model, p.MenuModel, p.DesignRef, p.Color,
        p.Title, p.Description, p.Price,
        string.IsNullOrWhiteSpace(p.ImageFileName) ? string.Empty : $"{MediaBaseUrl}/uploads/gallery/{p.ImageFileName}",
        p.SortOrder, p.IsActive, p.InternalProductId, p.InternalProduct?.Sku, p.InternalProduct?.Name);

    private static CatalogProduct MapFormToEntity(CatalogProduct p, CatalogProductForm form, string slug, string fileName)
    {
        p.Slug = slug;
        p.ProductLine = form.ProductLine;
        p.Brand = form.Brand ?? "";
        p.Model = form.Model;
        p.MenuModel = string.IsNullOrWhiteSpace(form.MenuModel) ? form.Model : form.MenuModel;
        p.DesignRef = form.DesignRef;
        p.Color = form.Color;
        p.Title = form.Title;
        p.Description = form.Description;
        p.Price = form.Price;
        p.ImageFileName = fileName;
        p.SortOrder = form.SortOrder;
        p.InternalProductId = form.InternalProductId is > 0 ? form.InternalProductId : null;
        p.UpdatedAt = DateTime.UtcNow;
        if (p.Id == 0) p.CreatedAt = DateTime.UtcNow;
        return p;
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug)
    {
        var slug = baseSlug;
        var i = 1;
        while (await db.CatalogProducts.AnyAsync(p => p.Slug == slug))
            slug = $"{baseSlug}-{++i}";
        return slug;
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("ñ", "n").Replace("á", "a").Replace("é", "e")
            .Replace("í", "i").Replace("ó", "o").Replace("ú", "u");
        return System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "gallery");
        Directory.CreateDirectory(uploadsDir);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".png" => ".png", ".jpg" => ".jpg", ".jpeg" => ".jpeg",
            ".webp" => ".webp", _ => ".png"
        };
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(uploadsDir, fileName);
        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
        return fileName;
    }

    private void DeleteImage(string fileName)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "gallery", fileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }
}

public record CatalogProductDto(
    int Id, string Slug, string ProductLine, string Brand, string? Model, string? MenuModel,
    string? DesignRef, string? Color, string Title, string? Description, decimal Price,
    string ImageUrl, int SortOrder, bool IsActive, int? InternalProductId,
    string? InternalProductSku, string? InternalProductName);

public record BrandDto(string Brand, int Count);

public record LineInfoDto(string Slug, string Title, string Description);

public class CatalogProductForm
{
    public string ProductLine { get; set; } = "";
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? MenuModel { get; set; }
    public string? DesignRef { get; set; }
    public string? Color { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int SortOrder { get; set; }
    public int? InternalProductId { get; set; }
    public IFormFile? File { get; set; }
}
