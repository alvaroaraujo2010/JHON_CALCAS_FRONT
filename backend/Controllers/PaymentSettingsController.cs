using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/payment-settings")]
[Authorize(Policy = "payments.manage")]
public class PaymentSettingsController(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpGet("mercadopago")]
    public async Task<ActionResult<PaymentSettingsDto>> GetMercadoPago()
    {
        var settings = await GetOrCreateMercadoPagoAsync();
        return Ok(ToDto(settings, maskToken: true));
    }

    [HttpPut("mercadopago")]
    public async Task<ActionResult<PaymentSettingsDto>> UpdateMercadoPago([FromBody] UpdatePaymentSettingsRequest req)
    {
        var settings = await GetOrCreateMercadoPagoAsync();
        settings.IsActive = req.IsActive;
        settings.UseSandbox = req.UseSandbox;
        settings.PublicKey = req.PublicKey?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(req.AccessToken) && !req.AccessToken.Contains('*'))
            settings.AccessToken = req.AccessToken.Trim();
        settings.BaseUrl = NormalizeBaseUrl(req.BaseUrl);
        settings.WebhookUrl = req.WebhookUrl?.Trim() ?? string.Empty;
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(settings, maskToken: true));
    }

    private async Task<PaymentSettings> GetOrCreateMercadoPagoAsync()
    {
        var settings = await db.PaymentSettings.FirstOrDefaultAsync(x => x.Provider == "mercadopago");
        if (settings != null) return settings;

        settings = new PaymentSettings
        {
            Provider = "mercadopago",
            IsActive = true,
            PublicKey = config["MercadoPago:PublicKey"] ?? string.Empty,
            AccessToken = config["MercadoPago:AccessToken"] ?? string.Empty,
            BaseUrl = NormalizeBaseUrl(config["MercadoPago:BaseUrl"]),
            WebhookUrl = string.Empty
        };
        db.PaymentSettings.Add(settings);
        await db.SaveChangesAsync();
        return settings;
    }

    private static PaymentSettingsDto ToDto(PaymentSettings settings, bool maskToken) => new(
        settings.Provider,
        settings.IsActive,
        settings.UseSandbox,
        settings.PublicKey,
        maskToken ? Mask(settings.AccessToken) : settings.AccessToken,
        settings.BaseUrl,
        settings.WebhookUrl,
        settings.UpdatedAt
    );

    private static string Mask(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (value.Length <= 10) return "********";
        return value[..6] + new string('*', Math.Min(24, value.Length - 10)) + value[^4..];
    }

    private static string NormalizeBaseUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().TrimEnd('/');
}

public record PaymentSettingsDto(
    string Provider,
    bool IsActive,
    bool UseSandbox,
    string PublicKey,
    string AccessToken,
    string BaseUrl,
    string WebhookUrl,
    DateTime UpdatedAt);

public record UpdatePaymentSettingsRequest(
    bool IsActive,
    bool UseSandbox,
    string? PublicKey,
    string? AccessToken,
    string? BaseUrl,
    string? WebhookUrl);
