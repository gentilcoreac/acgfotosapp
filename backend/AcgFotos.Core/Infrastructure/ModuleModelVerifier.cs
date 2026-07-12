using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AcgFotos.Core.Data;

namespace AcgFotos.Core.Infrastructure
{
    /// <summary>
    /// Self-check de arranque (ADR-0009, mitigación del descubrimiento en runtime). El contexto único
    /// descubre las EF configs por assembly (D2); si un módulo declarado en <c>AppModulesName</c> no carga
    /// su <c>&lt;módulo&gt;.Infrastructure</c> (typo en config, falta de ProjectReference en el host, etc.),
    /// sus entidades faltarían en el modelo y el fallo recién aparecería en la primera query ("tabla
    /// inexistente"). Este verificador lo hace ruidoso al startup: loguea los módulos, qué assemblies de
    /// infraestructura cargaron y cuántas entidades quedaron en el modelo; avisa con Warning/Error si algo falta.
    /// </summary>
    public class ModuleModelVerifier : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ModuleModelVerifier> _logger;

        public ModuleModelVerifier(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<ModuleModelVerifier> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var modules = (_configuration["AppModulesName"] ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            _logger.LogInformation("[Arranque] Módulos declarados (AppModulesName): {Modules}",
                string.Join(", ", modules));

            // ¿Cargó el assembly de infraestructura de cada módulo? (de ahí salen sus EF configs)
            foreach (var module in modules)
            {
                var infraName = $"{module}.Infrastructure";
                if (TryLoad(infraName))
                {
                    _logger.LogInformation("[Arranque] Módulo '{Module}': {Assembly} OK.", module, infraName);
                }
                else
                {
                    _logger.LogWarning(
                        "[Arranque] Módulo '{Module}': no se pudo cargar {Assembly}. Si el módulo tiene store " +
                        "relacional, sus entidades NO estarán en el modelo (revisá AppModulesName y que el host " +
                        "referencie el proyecto). Si el módulo no es relacional, ignorá este aviso.",
                        module, infraName);
                }
            }

            // Cuántas entidades quedaron efectivamente en el modelo EF (fuerza/“calienta” el build del modelo).
            using var scope = _serviceProvider.CreateScope();
            if (scope.ServiceProvider.GetService<IDbContext>() is DbContext dbContext)
            {
                var entityCount = dbContext.Model.GetEntityTypes().Count();
                if (entityCount == 0)
                {
                    _logger.LogError("[Arranque] El modelo EF quedó SIN entidades. " +
                                     "El descubrimiento por assembly no aportó configs: revisá AppModulesName.");
                }
                else
                {
                    _logger.LogInformation("[Arranque] Modelo EF: {Count} entidades registradas.", entityCount);
                }
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static bool TryLoad(string assemblyName)
        {
            try
            {
                Assembly.Load(assemblyName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
