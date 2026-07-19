using AcgFotos.Core.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.InputValidators;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Application.Mappers.Mapperly;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Application.Services;

/// <summary>
/// El alcance sale de <see cref="IAppContext.FamiliaParticipanteIds"/> / <see cref="IAppContext.FamiliaEventoId"/>
/// (claims del JWT de familia, ADR-11) — nunca de un parámetro del request. No hereda
/// <c>ExtendedEntityAppServiceBase</c>: un pedido no se "edita", se confirma una única vez (ADR-07).
/// La validación de FORMA (largos, cantidades, duplicados) vive en
/// <see cref="PedidoConfirmarInputDtoValidator"/> — FluentValidation, mismo mecanismo que usan los
/// AppServices CRUD Extended, instanciado a mano acá porque no hay un <c>UpdateAsync</c> base que lo
/// descubra por convención. Acá solo quedan las reglas de NEGOCIO que necesitan repos (si la foto es
/// visible para la sesión, si el tamaño sigue activo en el catálogo del evento).
/// </summary>
public class FamiliaPedidoAppService : IFamiliaPedidoAppService
{
    private const string SinSesionMensaje = "Sesión de familia inválida.";

    private readonly IEventoRepository _eventoRepository;
    private readonly IFotoRepository _fotoRepository;
    private readonly IEntityBaseRepository<Pedido> _pedidoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppContext _appContext;
    private readonly EventoMapper _eventoMapper;

    public FamiliaPedidoAppService(
        IEventoRepository eventoRepository,
        IFotoRepository fotoRepository,
        IEntityBaseRepository<Pedido> pedidoRepository,
        IUnitOfWork unitOfWork,
        IAppContext appContext,
        EventoMapper eventoMapper)
    {
        _eventoRepository = eventoRepository;
        _fotoRepository = fotoRepository;
        _pedidoRepository = pedidoRepository;
        _unitOfWork = unitOfWork;
        _appContext = appContext;
        _eventoMapper = eventoMapper;
    }

    public async Task<List<TamanoPrecioDto>> ListarTamanosPreciosAsync()
    {
        if (_appContext.FamiliaEventoId is not long eventoId)
        {
            return new List<TamanoPrecioDto>();
        }

        var evento = await _eventoRepository.GetByIdWithTamanosReadOnlyAsync(eventoId);
        return evento?.TamanosPrecios
            .Where(t => t.Activo)
            .OrderBy(t => t.Orden)
            .Select(_eventoMapper.ToDto)
            .ToList() ?? new List<TamanoPrecioDto>();
    }

    public async Task<PedidoConfirmadoDto> ConfirmarAsync(PedidoConfirmarInputDto input)
    {
        if (_appContext.FamiliaEventoId is not long eventoId || _appContext.FamiliaParticipanteIds.Count == 0)
        {
            throw new ForbiddenException(SinSesionMensaje);
        }

        // Normalizado ANTES de validar: el validator chequea largo/vacío sobre el valor que
        // realmente se va a persistir, no sobre lo que mandó el cliente con espacios de sobra.
        var normalizado = new PedidoConfirmarInputDto
        {
            NombreContacto = (input.NombreContacto ?? string.Empty).Trim(),
            TelefonoContacto = (input.TelefonoContacto ?? string.Empty).Trim(),
            Items = input.Items,
        };

        var validacion = new PedidoConfirmarInputDtoValidator().Validate(normalizado);
        if (!validacion.IsValid)
        {
            throw new BusinessValidationException(MessagesAPI.ErrorDataEntryValues, validacion.Errors);
        }

        var nombreContacto = normalizado.NombreContacto;
        var telefonoContacto = normalizado.TelefonoContacto;

        var evento = await _eventoRepository.GetByIdWithTamanosReadOnlyAsync(eventoId)
            ?? throw new BusinessValidationException("El evento de la sesión ya no existe.");

        var tamanosActivos = evento.TamanosPrecios.Where(t => t.Activo).ToDictionary(t => t.Id);
        var tamanoIdsPedidos = input.Items.Select(i => i.TamanoPrecioId).Distinct();
        if (tamanoIdsPedidos.Any(id => !tamanosActivos.ContainsKey(id)))
        {
            throw new BusinessValidationException("Uno de los tamaños elegidos ya no está disponible.");
        }

        var fotosVisibles = await _fotoRepository.ListarParaFamiliaAsync(_appContext.FamiliaParticipanteIds);
        var fotoIdsVisibles = fotosVisibles.Select(f => f.Id).ToHashSet();
        if (input.Items.Any(i => !fotoIdsVisibles.Contains(i.FotoId)))
        {
            throw new BusinessValidationException("Una de las fotos del pedido no pertenece a tu álbum.");
        }

        var pedido = new Pedido
        {
            ParticipanteId = _appContext.FamiliaParticipanteIds.First(),
            NombreContacto = nombreContacto,
            TelefonoContacto = telefonoContacto,
            Estado = EstadoPedido.Pendiente,
            CreadoEn = DateTime.UtcNow,
        };

        foreach (var item in input.Items)
        {
            var precioUnitario = tamanosActivos[item.TamanoPrecioId].PrecioUnitario;
            pedido.Items.Add(new PedidoItem
            {
                FotoId = item.FotoId,
                TamanoPrecioId = item.TamanoPrecioId,
                Cantidad = item.Cantidad,
                PrecioUnitarioSnapshot = precioUnitario,
            });
        }

        pedido.Total = pedido.Items.Sum(i => i.Cantidad * i.PrecioUnitarioSnapshot);

        _pedidoRepository.Add(pedido);
        await _unitOfWork.CommitAsync();

        return new PedidoConfirmadoDto
        {
            Id = pedido.Id,
            Estado = pedido.Estado,
            Total = pedido.Total,
            CreadoEn = pedido.CreadoEn,
            Items = pedido.Items.Select(i => new PedidoItemConfirmadoDto
            {
                FotoId = i.FotoId,
                TamanoPrecioId = i.TamanoPrecioId,
                Cantidad = i.Cantidad,
                PrecioUnitarioSnapshot = i.PrecioUnitarioSnapshot,
            }).ToList(),
        };
    }
}
