using AcgFotos.Core.Application;

namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>
/// Input de alta/edición. Los participantes viajan como filas completas y se reconcilian por Id en
/// <c>GrupoAppService.SyncCollections</c> (Id 0 = alta con código de acceso generado; ausente =
/// baja, bloqueada si el participante tiene fotos). TenantId no se expone: lo estampa el DbContext.
/// </summary>
public class GrupoInputDto : DtoBase
{
    public long EventoId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public List<ParticipanteDto> Participantes { get; set; } = new();
}
