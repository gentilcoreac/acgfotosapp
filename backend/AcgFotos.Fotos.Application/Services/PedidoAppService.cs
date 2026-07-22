using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Application.Criterias;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Application.Mappers.Mapperly;
using AcgFotos.Fotos.Application.Security;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Application.Services;

/// <summary>
/// No hereda <c>ExtendedEntityAppServiceBase</c>: un pedido no se crea/edita/borra desde el admin
/// (ADR-07), solo se lista, se consulta y cambia de estado — misma forma que
/// <see cref="FamiliaPedidoAppService"/>.
/// </summary>
public class PedidoAppService : IPedidoAppService
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IEntityBaseRepository<Pedido> _pedidoBaseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppContext _appContext;
    private readonly PedidoMapper _pedidoMapper;

    public PedidoAppService(
        IPedidoRepository pedidoRepository,
        IEntityBaseRepository<Pedido> pedidoBaseRepository,
        IUnitOfWork unitOfWork,
        IAppContext appContext,
        PedidoMapper pedidoMapper)
    {
        _pedidoRepository = pedidoRepository;
        _pedidoBaseRepository = pedidoBaseRepository;
        _unitOfWork = unitOfWork;
        _appContext = appContext;
        _pedidoMapper = pedidoMapper;
    }

    public async Task<PaginationSet<PedidoHeaderDto>> SearchAsync(PedidoCriteria criteria)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(_appContext);
        var page = await _pedidoRepository.PaginateHeadersAsync(criteria, criteria.EventoId, criteria.Estado);
        return page.MapItems(_pedidoMapper.ToHeaderDto);
    }

    public async Task<PedidoDetalleDto?> GetByIdAsync(long id)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(_appContext);
        var entity = await _pedidoRepository.GetByIdWithDetalleAsync(id);
        return entity == null ? null : _pedidoMapper.ToDetalleDto(entity);
    }

    public async Task<PedidoHeaderDto> CambiarEstadoAsync(long id, EstadoPedido nuevoEstado)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(_appContext);

        var pedido = await _pedidoBaseRepository.GetByIdAsync(id)
            ?? throw new BusinessValidationException("El pedido no existe.");

        ValidarTransicion(pedido.Estado, nuevoEstado);

        pedido.Estado = nuevoEstado;
        await _unitOfWork.CommitAsync();

        var actualizado = await _pedidoRepository.GetByIdWithDetalleAsync(id);
        return _pedidoMapper.ToHeaderDto(actualizado!);
    }

    /// <summary>
    /// El workflow "normal" es lineal (Pendiente/Pagado → Impreso → Entregado — ver comentario de
    /// <see cref="EstadoPedido"/>: con pago en efectivo el pedido salta directo a Impreso, así que
    /// Pagado no es un paso obligatorio), pero el admin puede cambiar a CUALQUIER otro estado
    /// (incluido volver atrás): es una corrección manual para el caso excepcional de un click
    /// equivocado, no un flujo guiado — decisión de Alberto 2026-07-20. Lo único que se rechaza es
    /// "cambiar" al mismo estado en el que ya está (no-op sin sentido).
    /// </summary>
    private static void ValidarTransicion(EstadoPedido actual, EstadoPedido nuevo)
    {
        if (actual == nuevo)
        {
            throw new BusinessValidationException($"El pedido ya está en estado \"{actual}\".");
        }
    }

    public async Task<ListaImpresionDto> GetListaImpresionAsync(long eventoId, List<EstadoPedido> estados)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(_appContext);

        if (eventoId <= 0)
        {
            throw new BusinessValidationException("Debe seleccionar un evento.");
        }

        var items = await _pedidoRepository.GetItemsParaImpresionAsync(eventoId, estados);
        return ArmarListaImpresion(items);
    }

    /// <summary>
    /// Arma las dos vistas en memoria (volumen bajo: un evento tiene cientos de fotos, no miles de
    /// items — no justifica traducir el GroupBy a SQL). El detalle agrupa por (foto, tamaño) DENTRO
    /// de cada participante, sumando cantidades, para que un participante con dos pedidos separados
    /// pidiendo la misma foto+tamaño no salga duplicado.
    /// </summary>
    private static ListaImpresionDto ArmarListaImpresion(List<PedidoItem> items)
    {
        var agregado = items
            .GroupBy(i => (i.FotoId, i.TamanoPrecioId))
            .Select(g => new LineaAgregadoImpresionDto
            {
                FotoId = g.Key.FotoId,
                NombreArchivoOriginal = g.First().Foto.NombreArchivoOriginal,
                AnchoFoto = g.First().Foto.Ancho,
                AltoFoto = g.First().Foto.Alto,
                TamanoPrecioNombre = g.First().TamanoPrecio.Nombre,
                CantidadTotal = g.Sum(i => i.Cantidad),
            })
            .OrderBy(l => l.NombreArchivoOriginal).ThenBy(l => l.TamanoPrecioNombre)
            .ToList();

        var detalle = items
            .GroupBy(i => i.Pedido.ParticipanteId)
            .Select(g => new GrupoParticipanteImpresionDto
            {
                ParticipanteId = g.Key,
                ParticipanteNombre = g.First().Pedido.Participante.Nombre,
                Lineas = g
                    .GroupBy(i => (i.FotoId, i.TamanoPrecioId))
                    .Select(gi => new LineaParticipanteImpresionDto
                    {
                        FotoId = gi.Key.FotoId,
                        NombreArchivoOriginal = gi.First().Foto.NombreArchivoOriginal,
                        TamanoPrecioNombre = gi.First().TamanoPrecio.Nombre,
                        Cantidad = gi.Sum(i => i.Cantidad),
                    })
                    .OrderBy(l => l.NombreArchivoOriginal).ThenBy(l => l.TamanoPrecioNombre)
                    .ToList(),
            })
            .OrderBy(g => g.ParticipanteNombre)
            .ToList();

        return new ListaImpresionDto { Agregado = agregado, Detalle = detalle };
    }
}
