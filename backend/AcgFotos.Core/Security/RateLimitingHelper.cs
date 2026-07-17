using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcgFotos.Core.Security
{
    // Rate limiting en capas:
    //   - global (default): permisivo, cubre TODO el tráfico legítimo (incluido refresh/logout, que NO
    //     son vectores de fuerza bruta). Partición por usuario logueado, o por IP si es anónimo.
    //   - "login": estricto, por IP. Solo /api/auth/token (defensa de credential stuffing).
    //   - "password": estricto, por IP. Endpoints de password (olvide/resetear/cambiar): enumeración/reset.
    //   - "canje": estricto, por IP. Solo /api/fotos/canje (fuerza bruta de códigos de acceso, ADR-02).
    //
    // Refresh, logout, confirmar-cuenta e impersonation start/stop quedan SOLO bajo el límite global:
    // antes estaban en el balde estricto de auth y rompían sesiones activas durante uso intenso.
    //
    // Límites configurables vía appsettings ("RateLimiting"). A propósito NO se overridean por ambiente
    // (mismos valores en dev y prod) para detectar en dev los topes que se darían en producción.
    //
    // OJO partición por IP: usa RemoteIpAddress. Detrás de un reverse-proxy/Cloudflare hay que respetar
    // X-Forwarded-For (ForwardedHeaders + KnownProxies en Startup), si no todos los clientes comparten la
    // IP del proxy y el límite por IP se vuelve un límite compartido. Ver tareas pendientes (Cloudflare).
    public static class RateLimitingHelper
    {
        public const string LoginPolicy = "login";
        public const string PasswordPolicy = "password";
        public const string CanjePolicy = "canje";

        public static void ConfigureService(IServiceCollection services, IConfiguration configuration)
        {
            var global = ReadWindow(configuration, "RateLimiting:Global", permitDefault: 100, windowSecondsDefault: 60);
            var login = ReadWindow(configuration, "RateLimiting:Login", permitDefault: 10, windowSecondsDefault: 60);
            var password = ReadWindow(configuration, "RateLimiting:Password", permitDefault: 5, windowSecondsDefault: 60);
            var canje = ReadWindow(configuration, "RateLimiting:Canje", permitDefault: 10, windowSecondsDefault: 60);

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Política global (default): aplica a todo. Partición por usuario si hay token, sino por IP.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var partitionKey = httpContext.User?.Identity?.IsAuthenticated == true
                        ? $"user:{httpContext.User.Identity.Name}"
                        : $"ip:{httpContext.Connection.RemoteIpAddress}";

                    return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                        _ => BuildWindow(global));
                });

                // Políticas estrictas por IP, una partición por endpoint sensible (no comparten balde).
                options.AddPolicy(LoginPolicy, httpContext => PerIpPartition(httpContext, LoginPolicy, login));
                options.AddPolicy(PasswordPolicy, httpContext => PerIpPartition(httpContext, PasswordPolicy, password));
                options.AddPolicy(CanjePolicy, httpContext => PerIpPartition(httpContext, CanjePolicy, canje));
            });
        }

        private static RateLimitPartition<string> PerIpPartition(HttpContext httpContext, string prefix, (int Permit, TimeSpan Window) cfg)
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter($"{prefix}:{ip}", _ => BuildWindow(cfg));
        }

        private static FixedWindowRateLimiterOptions BuildWindow((int Permit, TimeSpan Window) cfg) =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = cfg.Permit,
                Window = cfg.Window,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            };

        private static (int Permit, TimeSpan Window) ReadWindow(IConfiguration configuration, string section, int permitDefault, int windowSecondsDefault)
        {
            var permit = configuration.GetValue<int?>($"{section}:PermitLimit") ?? permitDefault;
            var windowSeconds = configuration.GetValue<int?>($"{section}:WindowSeconds") ?? windowSecondsDefault;
            return (permit, TimeSpan.FromSeconds(windowSeconds));
        }
    }
}
