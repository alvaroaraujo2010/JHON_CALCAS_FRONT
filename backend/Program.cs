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

builder.Services.AddAuthorization();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<ElectronicInvoiceService>();
builder.Services.AddScoped<LegalParameterService>();
builder.Services.AddScoped<WithholdingTaxService>();
builder.Services.AddScoped<PayrollCalculator>();
builder.Services.AddScoped<PayrollAccountingService>();
builder.Services.AddScoped<PayrollSettlementService>();
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
    await DbSeeder.SeedAsync(db);
    // Sembrar tablas de retención 2025 y 2026 si no existen
    await withholdingSvc.SeedTableAsync(2025);
    await withholdingSvc.SeedTableAsync(2026);
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors("ContaNexo");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
