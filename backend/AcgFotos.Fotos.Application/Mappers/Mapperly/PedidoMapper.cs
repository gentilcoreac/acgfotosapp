using Riok.Mapperly.Abstractions;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Mappers.Mapperly;

[Mapper]
public partial class PedidoMapper
{
    // Header (listado): ParticipanteNombre/CantidadItems se calculan, así que el mapeo es manual.
    public PedidoHeaderDto ToHeaderDto(Pedido entity) => new()
    {
        Id = entity.Id,
        ParticipanteId = entity.ParticipanteId,
        ParticipanteNombre = entity.Participante.Nombre,
        NombreContacto = entity.NombreContacto,
        TelefonoContacto = entity.TelefonoContacto,
        Estado = entity.Estado,
        Total = entity.Total,
        CreadoEn = entity.CreadoEn,
        CantidadItems = entity.Items.Count,
    };

    // Detalle (GetById): agrega las líneas con el nombre del tamaño vigente en el catálogo.
    public PedidoDetalleDto ToDetalleDto(Pedido entity) => new()
    {
        Id = entity.Id,
        ParticipanteId = entity.ParticipanteId,
        ParticipanteNombre = entity.Participante.Nombre,
        NombreContacto = entity.NombreContacto,
        TelefonoContacto = entity.TelefonoContacto,
        Estado = entity.Estado,
        Total = entity.Total,
        CreadoEn = entity.CreadoEn,
        CantidadItems = entity.Items.Count,
        Items = entity.Items.Select(this.ToItemDetalleDto).ToList(),
    };

    private PedidoItemDetalleDto ToItemDetalleDto(PedidoItem item) => new()
    {
        FotoId = item.FotoId,
        NombreArchivoOriginal = item.Foto.NombreArchivoOriginal,
        TamanoPrecioNombre = item.TamanoPrecio.Nombre,
        Cantidad = item.Cantidad,
        PrecioUnitarioSnapshot = item.PrecioUnitarioSnapshot,
    };
}
