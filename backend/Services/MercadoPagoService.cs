using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContaNexo.API.Services;

/// <summary>
/// Integración con Mercado Pago Checkout Pro.
/// Crea preferencias de pago y procesa IPN (Instant Payment Notification).
/// </summary>
public class MercadoPagoService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<MercadoPagoService> _log;

    public MercadoPagoService(HttpClient http, IConfiguration config, ILogger<MercadoPagoService> log)
    {
        _http = http;
        _config = config;
        _log = log;
    }

    private string AccessToken => _config["MercadoPago:AccessToken"] ?? "";
    private string PublicKey => _config["MercadoPago:PublicKey"] ?? "";

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
        var mpItems = items.Select(i => new MpPreferenceItem(
            i.Title, i.Quantity, i.UnitPrice, "COP",
            i.PictureUrl != null ? $"{baseUrl}{i.PictureUrl}" : null
        )).ToList();

        var request = new MpCreatePreferenceRequest(
            mpItems,
            new MpBackUrls(
                $"{baseUrl}/checkout/{orderId}/success",
                $"{baseUrl}/checkout/{orderId}/failure",
                $"{baseUrl}/checkout/{orderId}/pending"
            ),
            $"{baseUrl}/api/payment/webhook",
            orderNumber,
            "approved"
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var msg = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/checkout/preferences");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        msg.Content = content;

        var response = await _http.SendAsync(msg);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _log.LogError("MP create preference failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Error al crear preferencia de pago: {response.StatusCode}");
        }

        var pref = JsonSerializer.Deserialize<MpPreferenceResponse>(body);
        return new MpPreferenceResult(pref!.Id, pref.InitPoint ?? "", pref.SandboxInitPoint ?? "");
    }

    /// <summary>
    /// Consulta los datos de un pago por su ID (IPN).
    /// </summary>
    public async Task<MpPaymentInfo?> GetPaymentInfoAsync(string paymentId)
    {
        var msg = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{paymentId}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        var response = await _http.SendAsync(msg);
        if (!response.IsSuccessStatusCode) return null;
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MpPaymentInfo>(body);
    }
}
