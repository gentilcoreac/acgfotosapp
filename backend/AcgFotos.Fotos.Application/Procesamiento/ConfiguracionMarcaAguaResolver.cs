using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Application.Procesamiento;

public class ConfiguracionMarcaAguaResolver : IConfiguracionMarcaAguaResolver
{
    private readonly IEventoRepository _eventoRepository;
    private readonly IPerfilMarcaAguaRepository _perfilRepository;
    private readonly IOpcionesPublicacionRepository _opcionesRepository;

    public ConfiguracionMarcaAguaResolver(
        IEventoRepository eventoRepository,
        IPerfilMarcaAguaRepository perfilRepository,
        IOpcionesPublicacionRepository opcionesRepository)
    {
        _eventoRepository = eventoRepository;
        _perfilRepository = perfilRepository;
        _opcionesRepository = opcionesRepository;
    }

    public async Task<(PerfilMarcaAgua? Perfil, OpcionesPublicacion? Opciones)> ResolverAsync(long eventoId)
    {
        var evento = await _eventoRepository.GetByIdAsync(eventoId);

        var perfil = evento?.PerfilMarcaAguaId is long perfilId
            ? await _perfilRepository.GetByIdConCapasReadOnlyAsync(perfilId)
            : await _perfilRepository.GetDefaultConCapasReadOnlyAsync();

        var opciones = evento?.OpcionesPublicacionId is long opcionesId
            ? await _opcionesRepository.GetByIdAsync(opcionesId)
            : await _opcionesRepository.GetDefaultReadOnlyAsync();

        return (perfil, opciones);
    }
}
