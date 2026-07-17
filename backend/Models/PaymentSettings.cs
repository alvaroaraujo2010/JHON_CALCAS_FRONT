namespace ContaNexo.API.Models;

public class PaymentSettings
{
    public int Id { get; set; }
    public string Provider { get; set; } = "mercadopago";
    public bool IsActive { get; set; } = true;
    public bool UseSandbox { get; set; } = false;
    public string PublicKey { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
