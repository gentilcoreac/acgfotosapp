using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// División dentro del evento (7ºA, 7ºB...). Existe para que las fotos grupales
/// (<see cref="Foto.AlbumId"/> null) se compartan entre todos los álbumes del curso.
/// </summary>
public class Curso : MultiTenantEntityBase
{
    public long EventoId { get; set; }
    public Evento Evento { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public ICollection<Album> Albumes { get; set; } = new List<Album>();
}
