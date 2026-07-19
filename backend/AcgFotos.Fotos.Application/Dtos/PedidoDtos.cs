using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>Línea del carrito que la familia confirma: una foto, un tamaño, N copias.</summary>
public class PedidoItemInputDto
{
    public long FotoId { get; set; }
    public long TamanoPrecioId { get; set; }
    public int Cantidad { get; set; }
}

/// <summary>
/// Confirmación del pedido (ADR-07): el precio NUNCA viaja acá — se toma del catálogo vigente del
/// evento al confirmar (<see cref="PedidoItemInputDto"/> no tiene campo de precio a propósito).
/// </summary>
public class PedidoConfirmarInputDto
{
    public string NombreContacto { get; set; } = string.Empty;
    public string TelefonoContacto { get; set; } = string.Empty;
    public List<PedidoItemInputDto> Items { get; set; } = new();
}

/// <summary>Línea confirmada, con el precio unitario que quedó congelado.</summary>
public class PedidoItemConfirmadoDto
{
    public long FotoId { get; set; }
    public long TamanoPrecioId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitarioSnapshot { get; set; }
}

/// <summary>Resultado de confirmar: lo mínimo para que la familia vea "quedó registrado".</summary>
public class PedidoConfirmadoDto
{
    public long Id { get; set; }
    public EstadoPedido Estado { get; set; }
    public decimal Total { get; set; }
    public DateTime CreadoEn { get; set; }
    public List<PedidoItemConfirmadoDto> Items { get; set; } = new();
}
