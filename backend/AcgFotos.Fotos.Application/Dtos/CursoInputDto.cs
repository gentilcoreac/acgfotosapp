using AcgFotos.Core.Application;

namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>
/// Input de alta/edición. Los álbumes viajan como filas completas y se reconcilian por Id en
/// <c>CursoAppService.SyncCollections</c> (Id 0 = alta con código de acceso generado; ausente =
/// baja, bloqueada si el álbum tiene fotos). TenantId no se expone: lo estampa el DbContext.
/// </summary>
public class CursoInputDto : DtoBase
{
    public long EventoId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public List<AlbumDto> Albumes { get; set; } = new();
}
