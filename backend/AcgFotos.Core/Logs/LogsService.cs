using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
// Unico lugar donde referenciamos a serilog
using Serilog;

namespace AcgFotos.Core.Logs
{
    public class LogsService
    {
        public static void ConfigureService(IServiceCollection services, IConfiguration configuration)
        {
            SerilogConfigureCreateLogger(configuration);

            // Registramos Serilog via ILoggingBuilder para no acoplar AcgFotos.Api al paquete Serilog directo.
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

            services.AddScoped<IEnrichLogger, EnrichLogger>();
            services.AddSingleton(Log.Logger);

        }

        private static void SerilogConfigureCreateLogger(IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Destructure.With<SensitiveDataDestructuringPolicy>()
                .CreateLogger();
        }
    }
}
