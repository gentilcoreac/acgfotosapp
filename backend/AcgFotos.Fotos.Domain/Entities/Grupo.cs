using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// División dentro del evento (7ºA, 7ºB...). Existe para que las fotos grupales
/// (<see cref="Foto.ParticipanteId"/> null) se compartan entre todos los participantes del grupo.
/// </summary>
public class Grupo : MultiTenantEntityBase
{
    public long EventoId { get; set; }
    public Evento Evento { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public ICollection<Participante> Participantes { get; set; } = new List<Participante>();
}
