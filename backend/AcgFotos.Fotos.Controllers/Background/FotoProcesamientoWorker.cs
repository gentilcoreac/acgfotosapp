using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Application.Procesamiento;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Controllers.Background;

/// <summary>
/// Único consumidor de la <see cref="FotoProcesamientoQueue"/> (ADR-04: BackgroundService con
/// canal en memoria, sin colas externas). Por ítem abre un scope propio y arma el contexto del
/// tenant con SetSystemContext — el filtro global y el estampado multi-tenant aplican igual que
/// en un request de ese tenant. Al arrancar re-encola lo que quedó Pendiente de una corrida
/// anterior (reinicio a mitad de un upload).
/// </summary>
public class FotoProcesamientoWorker : BackgroundService
{
    private readonly FotoProcesamientoQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FotoProcesamientoWorker> _logger;

    public FotoProcesamientoWorker(
        FotoProcesamientoQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<FotoProcesamientoWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await this.ReencolarPendientesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            FotoAProcesar item;
            try
            {
                item = await _queue.Reader.ReadAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; // shutdown: lo no procesado queda Pendiente y lo levanta el próximo arranque
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var appContext = scope.ServiceProvider.GetRequiredService<IAppContext>();
                appContext.SetSystemContext(item.TenantId, item.UserId, isAdmin: true);

                var procesador = scope.ServiceProvider.GetRequiredService<IFotoProcesadorAppService>();
                await procesador.ProcesarAsync(item.FotoId, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // El procesador ya deja la foto en Error; esto cubre fallos antes de llegar a él
                // (p. ej. DB caída). El worker nunca muere por una foto.
                _logger.LogError(ex, "Worker de fotos: falló el ítem {FotoId} (tenant {TenantId}).",
                    item.FotoId, item.TenantId);
            }
        }
    }

    private async Task ReencolarPendientesAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var fotoRepository = scope.ServiceProvider.GetRequiredService<IFotoRepository>();

            var pendientes = await fotoRepository.GetPendientesTodosLosTenantsAsync();
            foreach (var foto in pendientes)
            {
                // UserId 0: contexto sintético del sistema (fot_Fotos no estampa usuario).
                _queue.Encolar(new FotoAProcesar(foto.Id, foto.TenantId, UserId: 0));
            }

            if (pendientes.Count > 0)
            {
                _logger.LogInformation("Worker de fotos: re-encoladas {Count} fotos pendientes.", pendientes.Count);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Worker de fotos: falló el barrido inicial de pendientes.");
        }
    }
}
