using Microsoft.Extensions.Configuration;
using System.Threading.Channels;

namespace AcgFotos.Core.AuditLog
{
    /// <summary>
    /// Channel acotado (capacidad por "AuditLog:QueueCapacity", default 10.000) entre los filtros
    /// y el background writer. Singleton: todos los requests escriben acá y un único consumidor
    /// drena. Si el consumidor no da abasto y se llena, TryEnqueue devuelve false y esa entrada
    /// se pierde — trade-off aceptado en ADR-0005 a cambio de no bloquear nunca la respuesta.
    /// </summary>
    public sealed class AuditLogQueue : IAuditLogQueue
    {
        private const int DefaultCapacity = 10_000;

        private readonly Channel<AuditLogModel> _channel;

        public AuditLogQueue(IConfiguration configuration)
        {
            var capacity = configuration.GetValue<int?>("AuditLog:QueueCapacity") ?? DefaultCapacity;
            _channel = Channel.CreateBounded<AuditLogModel>(new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });
        }

        internal ChannelReader<AuditLogModel> Reader => _channel.Reader;

        public bool TryEnqueue(AuditLogModel entry) => _channel.Writer.TryWrite(entry);
    }
}
