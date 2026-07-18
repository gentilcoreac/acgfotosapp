using AcgFotos.Core.Session;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Application.Mappers.Mapperly;
using AcgFotos.Fotos.Application.Storage;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Application.Services;

/// <summary>
/// El alcance de la sesión (qué participantes puede ver) sale de <see cref="IAppContext.FamiliaParticipanteIds"/>
/// — los claims firmados del JWT de familia (ADR-11) — nunca de un parámetro del request.
/// </summary>
public class FamiliaGaleriaAppService : IFamiliaGaleriaAppService
{
    private readonly IFotoRepository _fotoRepository;
    private readonly IFotoStorage _fotoStorage;
    private readonly IAppContext _appContext;
    private readonly FotoMapper _fotoMapper;

    public FamiliaGaleriaAppService(
        IFotoRepository fotoRepository,
        IFotoStorage fotoStorage,
        IAppContext appContext,
        FotoMapper fotoMapper)
    {
        _fotoRepository = fotoRepository;
        _fotoStorage = fotoStorage;
        _appContext = appContext;
        _fotoMapper = fotoMapper;
    }

    public async Task<List<FotoDto>> ListarAsync()
    {
        var fotos = await _fotoRepository.ListarParaFamiliaAsync(_appContext.FamiliaParticipanteIds);
        return fotos.Select(_fotoMapper.ToDto).ToList();
    }

    public async Task<byte[]?> ObtenerThumbAsync(long fotoId)
    {
        var foto = await _fotoRepository.GetVisibleParaFamiliaAsync(fotoId, _appContext.FamiliaParticipanteIds);
        return foto == null ? null : await _fotoStorage.LeerThumbAsync(foto);
    }

    public async Task<byte[]?> ObtenerPreviewAsync(long fotoId)
    {
        var foto = await _fotoRepository.GetVisibleParaFamiliaAsync(fotoId, _appContext.FamiliaParticipanteIds);
        return foto == null ? null : await _fotoStorage.LeerPreviewAsync(foto);
    }
}
