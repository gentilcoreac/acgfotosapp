namespace AcgFotos.Fotos.Application.IServices;

/// <summary>
/// Procesa UNA foto pendiente: original → derivados con watermark → estado Lista (o Error).
/// Lo invoca el worker de background dentro de un scope con SetSystemContext del tenant del ítem.
/// </summary>
public interface IFotoProcesadorAppService
{
    Task ProcesarAsync(long fotoId, CancellationToken cancellationToken = default);
}
