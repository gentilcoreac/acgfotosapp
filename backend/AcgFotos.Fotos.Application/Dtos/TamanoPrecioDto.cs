using AcgFotos.Core.Application;

namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>Fila del catálogo de tamaños del evento. Id 0 = fila nueva (sync por Id en el update).</summary>
public class TamanoPrecioDto : DtoBase
{
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}
