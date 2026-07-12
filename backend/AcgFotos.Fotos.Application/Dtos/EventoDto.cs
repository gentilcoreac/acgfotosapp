namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>Detalle para ver/editar: los datos del evento + su catálogo de tamaños.</summary>
public class EventoDto : EventoHeaderDto
{
    public List<TamanoPrecioDto> TamanosPrecios { get; set; } = new();
}
