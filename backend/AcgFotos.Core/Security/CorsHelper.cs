using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace AcgFotos.Core.Security
{
    public class CorsHelper
    {
        public static void ConfigureService(IServiceCollection services, IConfiguration configuration)
        {
            var allowedOriginsConfig = configuration.GetSection("AllowedOrigins").Get<string>();
            var origins = new List<string>().ToArray();
          
            if (!string.IsNullOrEmpty(allowedOriginsConfig))
            {
                origins = allowedOriginsConfig.Split(";");
            }
            services.AddCors(options =>
            {
                // AllowCredentials() es necesario para que el navegador envíe/acepte la cookie
                // HttpOnly del refresh token en requests cross-origin. No es compatible con
                // AllowAnyOrigin(): hay que devolver el origin explícito.
                options.AddPolicy("AllowCustomOrigins", builder =>
                {
                    builder
                        .WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        // El front lee el nombre de archivo de las descargas (plantillas/export XLSX);
                        // sin exponerlo, el navegador oculta el header en cross-origin.
                        .WithExposedHeaders("Content-Disposition")
                        .AllowCredentials();
                });

                // En dev reflejamos el origin del request (equivalente a "cualquiera" pero
                // devolviendo el origin concreto) para poder habilitar credentials.
                options.AddPolicy("AllowAll", builder =>
                {
                    builder
                        .SetIsOriginAllowed(_ => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders("Content-Disposition")
                        .AllowCredentials();
                });
            });
        }
    
        public static void UseCors(IApplicationBuilder app,
                                   IConfiguration configuration,
                                   IWebHostEnvironment env)
        {
            var allowedOriginsConfiguration = configuration.GetSection("AllowedOrigins").Get<string>();
            var hasOrigins = !string.IsNullOrEmpty(allowedOriginsConfiguration);

            if (env.IsDevelopment())
            {
                app.UseCors("AllowAll");
                return;
            }

            if (hasOrigins)
            {
                app.UseCors("AllowCustomOrigins");
                return;
            }

            // Non-dev sin orígenes configurados: rechazar todo cross-origin.
            // Si AllowedOrigins quedó vacío por error, fallar cerrado en vez de abrir CORS.
            // Sin app.UseCors() no se agrega el middleware → preflights y origen distinto fallan.
            // Logueamos para que el deploy note el misconfig.
            var logger = app.ApplicationServices.GetService(typeof(Microsoft.Extensions.Logging.ILoggerFactory))
                as Microsoft.Extensions.Logging.ILoggerFactory;
            logger?.CreateLogger(nameof(CorsHelper))
                .LogWarning("AllowedOrigins vacío en ambiente {Env}: cross-origin requests serán rechazadas. " +
                            "Configurar 'AllowedOrigins' en appsettings o variables de entorno.",
                            env.EnvironmentName);
        }
    }
}
