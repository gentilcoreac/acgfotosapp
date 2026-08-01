using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Una graduación en un lugarOrganizacion: raíz del vertical Fotos. Debajo cuelgan los grupos (y sus
/// participantes/fotos), el catálogo de tamaños/precios del evento y, vía participantes, los pedidos.
/// </summary>
public class Evento : MultiTenantEntityBase
{
    public string Nombre { get; set; } = null!;
    public string? LugarOrganizacion { get; set; }
    public DateTime? Fecha { get; set; }

    /// <summary>Hasta cuándo las familias pueden ver el participante y pedir. Null = sin límite.</summary>
    public DateTime? FechaExpiracion { get; set; }

    public EstadoEvento Estado { get; set; } = EstadoEvento.Borrador;

    /// <summary>Null ⇒ usa el perfil default del estudio (y sin ninguno cargado, <c>OpcionesFotos</c>).</summary>
    public long? PerfilMarcaAguaId { get; set; }
    public PerfilMarcaAgua? PerfilMarcaAgua { get; set; }

    /// <summary>Null ⇒ usa las opciones default del estudio (y sin ninguna cargada, <c>OpcionesFotos</c>).</summary>
    public long? OpcionesPublicacionId { get; set; }
    public OpcionesPublicacion? OpcionesPublicacion { get; set; }

    public ICollection<Grupo> Grupos { get; set; } = new List<Grupo>();
    public ICollection<TamanoPrecio> TamanosPrecios { get; set; } = new List<TamanoPrecio>();
}
