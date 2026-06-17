using ContaNexo.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

/// <summary>
/// Middleware que bloquea las rutas /api/* (excepto auth, health y subscription)
/// cuando la suscripción está inactiva. Retorna 402 Payment Required.
/// </summary>
public class SubscriptionFilter
{
    private readonly RequestDelegate _next;

    public SubscriptionFilter(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, AppDbContext db)
    {
        var path = ctx.Request.Path.Value ?? "";

        // Permitir siempre: auth, health, subscription, assets públicos, uploads
        if (path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/subscription", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        // Solo bloquear rutas /api/*
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            var sub = await db.Subscriptions.AsNoTracking().FirstOrDefaultAsync();
            if (sub == null || !sub.IsActive)
            {
                ctx.Response.StatusCode = 402;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(
                    """{"error":"subscription_inactive","message":"Suscripción vencida. La zona administrativa está desactivada. Contacte a su proveedor para renovar."}""");
                return;
            }
        }

        await _next(ctx);
    }
}
