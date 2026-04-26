using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Corely.IAM.Web.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    private const int STATIC_ASSET_CACHE_SECONDS = 86400;
    private static readonly HashSet<string> CacheableStaticExtensions =
    [
        ".css",
        ".js",
        ".map",
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".svg",
        ".ico",
        ".webp",
        ".woff",
        ".woff2",
        ".ttf",
        ".eot",
        ".webmanifest",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Frame-Options"] = "DENY";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        ApplyCacheHeaders(context);

        // HSTS only applies over HTTPS; skip in development to avoid locking out HTTP
        if (!env.IsDevelopment())
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // CSP for Blazor Server + SignalR
        headers["Content-Security-Policy"] = string.Join(
            "; ",
            [
                "default-src 'self'",
                "script-src 'self' 'unsafe-inline'", // Blazor requires inline scripts for initialization
                "style-src 'self' 'unsafe-inline'", // Bootstrap uses inline styles
                "connect-src 'self' wss: ws:", // SignalR WebSocket connections
                "img-src 'self' data:", // data: URIs for inline images and favicons
                "font-src 'self'",
                "frame-ancestors 'none'", // CSP equivalent of X-Frame-Options: DENY
                "form-action 'self'",
                "base-uri 'self'",
                "object-src 'none'",
            ]
        );

        await next(context);
    }

    private static void ApplyCacheHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;
        if (IsStaticAssetRequest(context.Request.Path))
        {
            headers["Cache-Control"] = $"public, max-age={STATIC_ASSET_CACHE_SECONDS}";
            headers.Remove("Pragma");
            return;
        }

        headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        headers["Pragma"] = "no-cache";
    }

    private static bool IsStaticAssetRequest(PathString path)
    {
        if (!path.HasValue)
        {
            return false;
        }

        var pathValue = path.Value!;
        if (
            pathValue.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
            || pathValue.StartsWith("/_content", StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        var extension = System.IO.Path.GetExtension(pathValue);
        return !string.IsNullOrWhiteSpace(extension)
            && CacheableStaticExtensions.Contains(extension);
    }
}
