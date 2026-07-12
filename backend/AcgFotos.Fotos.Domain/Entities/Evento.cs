using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Una graduación en un colegio: raíz del vertical Fotos. Debajo cuelgan los cursos (y sus
/// álbumes/fotos), el catálogo de tamaños/precios del evento y, vía álbumes, los pedidos.
/// </summary>
public class Evento : MultiTenantEntityBase
{
    public string Nombre { get; set; } = null!;
    public string? Colegio { get; set; }
    public DateTime? Fecha { get; set; }

    /// <summary>Hasta cuándo las familias pueden ver el álbum y pedir. Null = sin límite.</summary>
    public DateTime? FechaExpiracion { get; set; }

    public EstadoEvento Estado { get; set; } = EstadoEvento.Borrador;

    public ICollection<Curso> Cursos { get; set; } = new List<Curso>();
    public ICollection<TamanoPrecio> TamanosPrecios { get; set; } = new List<TamanoPrecio>();
}
