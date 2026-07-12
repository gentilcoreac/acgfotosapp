using System.Threading.Channels;

namespace AcgFotos.Fotos.Application.Procesamiento;

/// <summary>
/// Ítem de trabajo del pipeline: el worker corre fuera del pipeline HTTP, así que el tenant y el
/// usuario viajan con el ítem para reconstruir el contexto (SetSystemContext) antes de tocar la DB.
/// </summary>
public record FotoAProcesar(long FotoId, long TenantId, long UserId);

/// <summary>
/// Channel en memoria entre el upload y el worker de derivados (ADR-04: sin colas externas).
/// Unbounded a propósito: los ítems son livianos (3 longs), el volumen por evento es acotado y
/// perder un upload por cola llena sería peor que la memoria que ahorra un bounded. Si el proceso
/// se reinicia con ítems en vuelo, el barrido de arranque del worker re-encola lo Pendiente.
/// </summary>
public sealed class FotoProcesamientoQueue
{
    private readonly Channel<FotoAProcesar> _channel = Channel.CreateUnbounded<FotoAProcesar>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelReader<FotoAProcesar> Reader => _channel.Reader;

    public void Encolar(FotoAProcesar item) => _channel.Writer.TryWrite(item);
}
