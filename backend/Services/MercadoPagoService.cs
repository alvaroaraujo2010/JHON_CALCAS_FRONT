using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

/// <summary>
/// Integración con Mercado Pago Checkout Pro.
/// Crea preferencias de pago y procesa IPN (Instant Payment Notification).
/// </summary>
public class MercadoPagoService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly ILogger<MercadoPagoService> _log;

    public MercadoPagoService(HttpClient http, IConfiguration config, AppDbContext db, ILogger<MercadoPagoService> log)
    {
        _http = http;
        _config = config;
        _db = db;
        _log = log;
    }

    public async Task<string> GetBaseUrlAsync()
    {
        var settings = await GetSettingsAsync();
        return NormalizeBaseUrl(settings?.BaseUrl) ?? NormalizeBaseUrl(_config["MercadoPago:BaseUrl"]) ?? "http://localhost:4200";
    }

    public record MpItem(string Title, int Quantity, decimal UnitPrice, string? PictureUrl = null);
    public record MpPreferenceResult(string Id, string InitPoint, string? SandboxInitPoint);

    public record MpCreatePreferenceRequest(
        [property: JsonPropertyName("items")] List<MpPreferenceItem> Items,
        [property: JsonPropertyName("back_urls")] MpBackUrls BackUrls,
        [property: JsonPropertyName("notification_url")] string NotificationUrl,
        [property: JsonPropertyName("external_reference")] string ExternalReference,
        [property: JsonPropertyName("auto_return")] string AutoReturn = "approved"
    );

    public record MpPreferenceItem(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("unit_price")] decimal UnitPrice,
        [property: JsonPropertyName("currency_id")] string CurrencyId = "COP",
        [property: JsonPropertyName("picture_url")] string? PictureUrl = null
    );

    public record MpBackUrls(
        [property: JsonPropertyName("success")] string Success,
        [property: JsonPropertyName("failure")] string Failure,
        [property: JsonPropertyName("pending")] string Pending
    );

    public record MpPreferenceResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("init_point")] string? InitPoint,
        [property: JsonPropertyName("sandbox_init_point")] string? SandboxInitPoint
    );

    public record MpPaymentNotification(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("topic")] string? Topic,
        [property: JsonPropertyName("type")] string? Type
    );

    public record MpPaymentInfo(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("status_detail")] string? StatusDetail,
        [property: JsonPropertyName("external_reference")] string? ExternalReference,
        [property: JsonPropertyName("payment_method_id")] string? PaymentMethodId,
        [property: JsonPropertyName("installments")] int? Installments,
        [property: JsonPropertyName("transaction_amount")] decimal? TransactionAmount,
        [property: JsonPropertyName("payer")] MpPayer? Payer
    );

    public record MpPayer(
        [property: JsonPropertyName("email")] string? Email
    );

    /// <summary>
    /// Crea una preferencia de pago en Mercado Pago.
    /// Retorna el ID de la preferencia y la URL de inicio.
    /// </summary>
    public async Task<MpPreferenceResult> CreatePreferenceAsync(
        string orderNumber, List<MpItem> items, string baseUrl, int? orderId)
    {
        var settings = await GetSettingsAsync();
        var accessToken = GetAccessToken(settings);
        if (settings is { IsActive: false })
            throw new InvalidOperationException("Mercado Pago está inactivo.");
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException("Mercado Pago no tiene Access Token configurado.");

        baseUrl = NormalizeBaseUrl(settings?.BaseUrl) ?? NormalizeBaseUrl(baseUrl) ?? "http://localhost:4200";
        var webhookUrl = !string.IsNullOrWhiteSpace(settings?.WebhookUrl)
            ? settings.WebhookUrl.Trim()
            : $"{baseUrl}/api/payment/webhook";

        var mpItems = items.Select(i => new MpPreferenceItem(
            i.Title, i.Quantity, i.UnitPrice, "COP",
            BuildPictureUrl(baseUrl, i.PictureUrl)
        )).ToList();

        var request = new MpCreatePreferenceRequest(
            mpItems,
            new MpBackUrls(
                $"{baseUrl}/pedido/{orderId}",
                $"{baseUrl}/pedido/{orderId}",
                $"{baseUrl}/pedido/{orderId}"
            ),
            webhookUrl,
            orderNumber,
            "approved"
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var msg = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/checkout/preferences");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        msg.Content = content;

        var response = await _http.SendAsync(msg);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _log.LogError("MP create preference failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Error al crear preferencia de pago: {response.StatusCode}");
        }

        var pref = JsonSerializer.Deserialize<MpPreferenceResponse>(body);
        var initPoint = settings?.UseSandbox == true
            ? pref!.SandboxInitPoint ?? pref.InitPoint ?? ""
            : pref!.InitPoint ?? pref.SandboxInitPoint ?? "";
        return new MpPreferenceResult(pref.Id, initPoint, pref.SandboxInitPoint ?? "");
    }

    /// <summary>
    /// Consulta los datos de un pago por su ID (IPN).
    /// </summary>
    public async Task<MpPaymentInfo?> GetPaymentInfoAsync(string paymentId)
    {
        var settings = await GetSettingsAsync();
        var accessToken = GetAccessToken(settings);
        if (string.IsNullOrWhiteSpace(accessToken)) return null;

        var msg = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{paymentId}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _http.SendAsync(msg);
        if (!response.IsSuccessStatusCode) return null;
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MpPaymentInfo>(body);
    }

    private async Task<PaymentSettings?> GetSettingsAsync() =>
        await _db.PaymentSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Provider == "mercadopago");

    private string GetAccessToken(PaymentSettings? settings) =>
        !string.IsNullOrWhiteSpace(settings?.AccessToken)
            ? settings.AccessToken
            : _config["MercadoPago:AccessToken"] ?? string.Empty;

    private static string? NormalizeBaseUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimEnd('/');

    private static string? BuildPictureUrl(string baseUrl, string? pictureUrl)
    {
        if (string.IsNullOrWhiteSpace(pictureUrl)) return null;
        if (pictureUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            pictureUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return pictureUrl;
        return pictureUrl.StartsWith('/') ? $"{baseUrl}{pictureUrl}" : $"{baseUrl}/{pictureUrl}";
    }
}
