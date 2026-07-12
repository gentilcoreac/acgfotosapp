using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AcgFotos.Core.AuditLog
{
    /// <summary>
    /// Único consumidor de la AuditLogQueue: junta las entradas en lotes y las persiste con un
    /// INSERT multi-fila (AuditLogRepository.WriteLogs), fuera del camino del request (ADR-0005).
    /// Tras el primer ítem espera una ventana corta ("AuditLog:FlushMilliseconds", default 1000)
    /// para coalescer la ráfaga. En el shutdown drena lo pendiente antes de salir.
    /// Un lote que falla se descarta (se loguea el error): el audit log no debe tumbar al writer.
    /// </summary>
    public class AuditLogBackgroundWriter : BackgroundService
    {
        private const int DefaultBatchSize = 100;
        private const int DefaultFlushMilliseconds = 1000;

        private readonly AuditLogQueue _queue;
        private readonly IAuditLogRepository _repository;
        private readonly ILogger<AuditLogBackgroundWriter> _logger;
        private readonly int _batchSize;
        private readonly TimeSpan _flushDelay;

        public AuditLogBackgroundWriter(
            AuditLogQueue queue,
            IAuditLogRepository repository,
            IConfiguration configuration,
            ILogger<AuditLogBackgroundWriter> logger)
        {
            _queue = queue;
            _repository = repository;
            _logger = logger;
            _batchSize = configuration.GetValue<int?>("AuditLog:BatchSize") ?? DefaultBatchSize;
            _flushDelay = TimeSpan.FromMilliseconds(
                configuration.GetValue<int?>("AuditLog:FlushMilliseconds") ?? DefaultFlushMilliseconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var buffer = new List<AuditLogModel>(_batchSize);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!await _queue.Reader.WaitToReadAsync(stoppingToken))
                    {
                        break;
                    }

                    // Ventana de coalescencia: dejar que la ráfaga se acumule antes de escribir.
                    await Task.Delay(_flushDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break; // shutdown: cae al drenaje final
                }

                this.DrainAndWrite(buffer);
            }

            // Drenaje final en el shutdown: persistir lo que quedó encolado.
            this.DrainAndWrite(buffer);
        }

        private void DrainAndWrite(List<AuditLogModel> buffer)
        {
            while (_queue.Reader.TryRead(out var entry))
            {
                buffer.Add(entry);
                if (buffer.Count >= _batchSize)
                {
                    this.Flush(buffer);
                }
            }

            this.Flush(buffer);
        }

        private void Flush(List<AuditLogModel> buffer)
        {
            if (buffer.Count == 0)
            {
                return;
            }

            try
            {
                _repository.WriteLogs(buffer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log: falló el INSERT en lote; se descartan {Count} entradas.", buffer.Count);
            }
            finally
            {
                buffer.Clear();
            }
        }
    }
}
