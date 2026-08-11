using System.Security.Claims;
using System.Text;
using ContaNexo.API.Data;
using ContaNexo.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), serverVersion));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Una policy por permiso del catálogo. Acepta el claim "permission" con el key.
    foreach (var p in PermissionService.Catalog)
    {
        options.AddPolicy(p.Key, policy =>
            policy.RequireClaim(JwtService.PermissionClaimType, p.Key));
    }
});
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ElectronicInvoiceService>();
builder.Services.AddScoped<LegalParameterService>();
builder.Services.AddScoped<WithholdingTaxService>();
builder.Services.AddScoped<PayrollCalculator>();
builder.Services.AddScoped<PayrollAccountingService>();
builder.Services.AddScoped<PayrollSettlementService>();
builder.Services.AddScoped<SalesAccountingService>();
builder.Services.AddScoped<PurchasesAccountingService>();
builder.Services.AddScoped<InventoryValuationService>();
builder.Services.AddScoped<CatalogOrderInventoryService>();
builder.Services.AddScoped<CatalogOrderSaleService>();
builder.Services.AddScoped<CatalogOrderFulfillmentService>();
builder.Services.AddScoped<DatabaseMigrationService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddHttpClient<MercadoPagoService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(o => o.AddPolicy("ContaNexo", p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var withholdingSvc = scope.ServiceProvider.GetRequiredService<WithholdingTaxService>();
    var migrationSvc = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();
    // Aplica migraciones SQL pendientes (idempotente) ANTES del seed.
    await migrationSvc.ApplyPendingAsync();
    await DbSeeder.SeedAsync(db);
    // Sembrar tablas de retención 2025 y 2026 si no existen
    await withholdingSvc.SeedTableAsync(2025);
    await withholdingSvc.SeedTableAsync(2026);
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

var logFile = Path.Combine(Directory.GetCurrentDirectory(), "startup-log.txt");
try { File.AppendAllText(logFile, $"[{DateTime.UtcNow:O}] App iniciando...\n"); } catch { }

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    try { File.AppendAllText(logFile, $"[FATAL] {e.ExceptionObject}\n"); } catch { }
};

app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        try { File.AppendAllText(logFile, $"[{DateTime.UtcNow:O}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n"); } catch { }
        throw;
    }
});

app.UseCors("ContaNexo");

app.UseMiddleware<SubscriptionFilter>();

// Endpoint para servir imágenes de galería (evita PhysicalFileProvider / FileSystemWatcher)
var galleryDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "gallery");
Directory.CreateDirectory(galleryDir);
app.MapGet("uploads/gallery/{fileName}", async (string fileName, HttpContext ctx) =>
{
    var path = Path.Combine(galleryDir, fileName);
    if (!File.Exists(path)) return Results.NotFound();
    var ext = Path.GetExtension(fileName).ToLowerInvariant();
    var contentType = ext switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
    ctx.Response.ContentType = contentType;
    await ctx.Response.SendFileAsync(path);
    return Results.Empty;
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
