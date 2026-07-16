using AcgFotos.Core.Application;
using AcgFotos.Fotos.Application.Criterias;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.IServices;

public interface IGrupoAppService : IExtendedEntityAppServiceBase<Grupo,
                                                                  GrupoInputDto,
                                                                  GrupoDto,
                                                                  GrupoHeaderDto,
                                                                  GrupoCriteria>
{
    /// <summary>Tarjetas imprimibles del grupo (una por participante, con código y QR de canje);
    /// null si el grupo no existe en el tenant.</summary>
    Task<TarjetasGrupoDto?> GetTarjetasAsync(long grupoId);
}
