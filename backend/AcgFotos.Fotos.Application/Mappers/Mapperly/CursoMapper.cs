using Riok.Mapperly.Abstractions;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Mappers.Mapperly;

[Mapper]
public partial class CursoMapper
{
    // Header (listado): CantidadAlbumes se calcula, así que el mapeo es manual.
    public CursoHeaderDto ToHeaderDto(Curso entity) => new()
    {
        Id = entity.Id,
        EventoId = entity.EventoId,
        Nombre = entity.Nombre,
        CantidadAlbumes = entity.Albumes.Count,
    };

    // Detalle (getById / respuesta del update): los álbumes ordenados como los ve el fotógrafo.
    public CursoDto ToDto(Curso entity) => new()
    {
        Id = entity.Id,
        EventoId = entity.EventoId,
        Nombre = entity.Nombre,
        CantidadAlbumes = entity.Albumes.Count,
        Albumes = entity.Albumes.OrderBy(a => a.NombreAlumno).Select(this.ToDto).ToList(),
    };

    public AlbumDto ToDto(Album entity)
    {
        var dto = this.MapAlbum(entity);
        // El código vigente: el activo más nuevo (puede haber viejos desactivados por reemplazo).
        dto.CodigoAcceso = entity.CodigosAcceso
            .Where(c => c.Activo)
            .OrderByDescending(c => c.CreadoEn)
            .FirstOrDefault()?.Codigo;
        return dto;
    }

    [MapperIgnoreSource(nameof(Album.TenantId))]
    [MapperIgnoreSource(nameof(Album.CursoId))]
    [MapperIgnoreSource(nameof(Album.Curso))]
    [MapperIgnoreSource(nameof(Album.CodigosAcceso))]
    [MapperIgnoreTarget(nameof(AlbumDto.CodigoAcceso))]
    private partial AlbumDto MapAlbum(Album entity);
}
