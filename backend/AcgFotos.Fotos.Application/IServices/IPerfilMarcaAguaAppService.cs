using AcgFotos.Core.Application;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.IServices;

public interface IPerfilMarcaAguaAppService : IExtendedEntityAppServiceBase<PerfilMarcaAgua,
                                                                            PerfilMarcaAguaInputDto,
                                                                            PerfilMarcaAguaDto,
                                                                            PerfilMarcaAguaDto,
                                                                            ListaPaginadaCriteriaBase>
{
    /// <summary>Sube el PNG de una capa (design.md D14): sin <c>PerfilMarcaAguaId</c> crea el perfil en el mismo paso.</summary>
    Task<CapaMarcaAguaSubidaDto> SubirCapaAsync(SubirCapaMarcaAguaInput input);

    /// <summary>Bytes del asset de una capa ya subida, para el editor. Null si no existe en el tenant actual.</summary>
    Task<byte[]?> ObtenerAssetCapaAsync(long perfilId, Guid storageKey);
}
