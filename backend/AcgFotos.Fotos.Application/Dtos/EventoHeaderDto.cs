using AcgFotos.Core.Application;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Dtos;

public class EventoHeaderDto : HeaderDtoBase
{
    public string Nombre { get; set; } = string.Empty;
    public string? LugarOrganizacion { get; set; }
    public DateTime? Fecha { get; set; }
    public DateTime? FechaExpiracion { get; set; }
    public EstadoEvento Estado { get; set; }

    /// <summary>Null ⇒ usa el perfil/opciones default del estudio (ADR-15 §5). Sólo el id: el ABM
    /// de Eventos elige de un combo, no necesita el perfil/opciones completos acá.</summary>
    public long? PerfilMarcaAguaId { get; set; }
    public long? OpcionesPublicacionId { get; set; }
}
