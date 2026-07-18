using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Domain.Repositories;

public interface IFotoRepository : IEntityBaseRepository<Foto>
{
    /// <summary>El grupo del upload, dentro del tenant (define el EventoId de las fotos).</summary>
    Task<Grupo?> GetGrupoAsync(long grupoId);

    /// <summary>El participante destino existe y pertenece al grupo (guard del upload).</summary>
    Task<bool> ParticipantePerteneceAlGrupoAsync(long participanteId, long grupoId);

    /// <summary>Foto tracked para que el worker actualice estado/dimensiones.</summary>
    Task<Foto?> GetByIdTrackedAsync(long id);

    /// <summary>Listado admin: fotos del grupo, opcionalmente de un participante puntual.</summary>
    Task<List<Foto>> ListarAsync(long grupoId, long? participanteId);

    /// <summary>
    /// Fotos en Pendiente de TODOS los tenants (IgnoreQueryFilters): SOLO para el barrido de
    /// arranque del worker, que re-encola lo que quedó a medias si el proceso se reinició.
    /// </summary>
    Task<List<Foto>> GetPendientesTodosLosTenantsAsync();

    /// <summary>
    /// Fotos visibles para una sesión de familia (ADR-11): las individuales de esos participantes
    /// más las grupales (<c>ParticipanteId == null</c>) de sus grupos. Solo <c>Lista</c> — a la
    /// familia nunca se le muestra una foto Pendiente/Error.
    /// </summary>
    Task<List<Foto>> ListarParaFamiliaAsync(IReadOnlyCollection<long> participanteIds);

    /// <summary>
    /// La foto puntual, solo si es visible para esos participantes (mismo criterio que
    /// <see cref="ListarParaFamiliaAsync"/>); null si no existe, no está Lista o no le pertenece a
    /// la sesión — el 404 no distingue "no existe" de "no es tuya" (no dar pistas).
    /// </summary>
    Task<Foto?> GetVisibleParaFamiliaAsync(long fotoId, IReadOnlyCollection<long> participanteIds);
}
