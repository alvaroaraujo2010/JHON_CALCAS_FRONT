using ContaNexo.API.Data;
using ContaNexo.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get()
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var salesMonth = await db.Sales.Where(s => s.SaleDate >= start).SumAsync(s => (decimal?)s.Total) ?? 0;
        var purchasesMonth = await db.Purchases.Where(p => p.PurchaseDate >= start).SumAsync(p => (decimal?)p.Total) ?? 0;
        var products = await db.Products.Where(p => p.IsActive).ToListAsync();
        var lowStock = products.Where(p => p.Stock <= p.MinStock).ToList();

        var recentSales = await db.Sales
            .Include(s => s.Customer)
            .OrderByDescending(s => s.SaleDate)
            .Take(5)
            .Select(s => new RecentSaleDto(s.Id, s.DocumentNumber, s.Customer != null ? s.Customer.Name : null, s.Total, s.SaleDate))
            .ToListAsync();

        return Ok(new DashboardDto(
            salesMonth, purchasesMonth, products.Count, lowStock.Count,
            await db.Customers.CountAsync(c => c.IsActive),
            await db.Suppliers.CountAsync(s => s.IsActive),
            recentSales,
            lowStock.Select(p => new LowStockProductDto(p.Id, p.Sku, p.Name, p.Stock, p.MinStock)).ToList()
        ));
    }
}
