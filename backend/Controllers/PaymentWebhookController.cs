using ContaNexo.API.Data;
using ContaNexo.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentWebhookController(
    AppDbContext db,
    MercadoPagoService mp,
    ILogger<PaymentWebhookController> log) : ControllerBase
{
    /// <summary>
    /// IPN (Instant Payment Notification) de Mercado Pago.
    /// MP envía una notificación con topic=payment y el ID del pago.
    /// Consultamos el pago y actualizamos la orden.
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] MercadoPagoService.MpPaymentNotification? notification)
    {
        if (notification == null)
        {
            // MP envía a veces un GET sin body como "ping"
            return Ok();
        }

        log.LogInformation("MP Webhook recibido: topic={Topic}, id={Id}", notification.Topic, notification.Id);

        // Si el topic es "payment", consultamos el pago
        if (notification.Topic == "payment" || notification.Type == "payment")
        {
            var paymentId = notification.Id;
            if (string.IsNullOrEmpty(paymentId))
                return BadRequest(new { message = "ID de pago requerido" });

            var payment = await mp.GetPaymentInfoAsync(paymentId);
            if (payment == null)
            {
                log.LogWarning("MP payment {PaymentId} no encontrado", paymentId);
                return NotFound();
            }

            log.LogInformation("MP payment {PaymentId}: status={Status}, ref={Ref}",
                paymentId, payment.Status, payment.ExternalReference);

            if (!string.IsNullOrEmpty(payment.ExternalReference))
            {
                var order = await db.Orders.FirstOrDefaultAsync(o =>
                    o.OrderNumber == payment.ExternalReference);
                if (order != null)
                {
                    order.MpPaymentId = paymentId;
                    order.MpPaymentStatus = payment.Status;
                    order.PaymentMethod = payment.PaymentMethodId;
                    order.Status = payment.Status switch
                    {
                        "approved" => "paid",
                        "pending" => "pending",
                        "in_process" => "processing",
                        "rejected" => "cancelled",
                        "refunded" => "cancelled",
                        "cancelled" => "cancelled",
                        _ => order.Status
                    };
                    order.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    log.LogInformation("Orden {OrderNumber} actualizada a {Status}",
                        order.OrderNumber, order.Status);
                }
            }
        }

        return Ok();
    }

    /// <summary>
    /// GET del webhook (MP envía un GET para verificar el endpoint).
    /// </summary>
    [HttpGet("webhook")]
    public IActionResult WebhookGet()
    {
        return Ok(new { status = "ok" });
    }
}
