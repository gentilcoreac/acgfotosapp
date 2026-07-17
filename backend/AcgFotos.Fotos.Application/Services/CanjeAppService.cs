using AcgFotos.Core.Exceptions;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.Familias;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Application.Services;

public class CanjeAppService : ICanjeAppService
{
    // Mismo mensaje para código inexistente/inactivo/evento no vigente: no darle a quien prueba
    // códigos al azar ninguna pista de cuál de esos casos fue (ADR-02, canje rate-limiteado).
    private const string CodigoInvalidoMensaje = "Código inválido o vencido.";

    private readonly ICodigoAccesoRepository _codigoAccesoRepository;
    private readonly IFamiliaTokenFactory _familiaTokenFactory;

    public CanjeAppService(ICodigoAccesoRepository codigoAccesoRepository, IFamiliaTokenFactory familiaTokenFactory)
    {
        _codigoAccesoRepository = codigoAccesoRepository;
        _familiaTokenFactory = familiaTokenFactory;
    }

    public async Task<CanjeResultDto> CanjearAsync(CanjeInputDto input)
    {
        var codigo = (input.Codigo ?? string.Empty).Trim().ToUpperInvariant();
        var codigoAcceso = await _codigoAccesoRepository.GetVigenteConEventoAsync(codigo);

        if (codigoAcceso == null || !codigoAcceso.Activo)
        {
            throw new BusinessValidationException(CodigoInvalidoMensaje);
        }

        var evento = codigoAcceso.Participante.Grupo.Evento;
        var vigente = evento.Estado == EstadoEvento.Publicado
            && (evento.FechaExpiracion == null || evento.FechaExpiracion >= DateTime.UtcNow);

        if (!vigente)
        {
            throw new BusinessValidationException(CodigoInvalidoMensaje);
        }

        var participante = codigoAcceso.Participante;
        var token = _familiaTokenFactory.Crear(evento.TenantId, evento.Id, new[] { participante.Id });

        return new CanjeResultDto
        {
            Token = token.Token,
            ValidTo = token.ValidTo,
            EventoId = evento.Id,
            NombreEvento = evento.Nombre,
            Participantes = new List<ParticipanteSesionDto>
            {
                new() { Id = participante.Id, Nombre = participante.Nombre },
            },
        };
    }
}
