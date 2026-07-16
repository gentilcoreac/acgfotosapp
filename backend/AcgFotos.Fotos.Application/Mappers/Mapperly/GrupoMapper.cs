using Riok.Mapperly.Abstractions;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Mappers.Mapperly;

[Mapper]
public partial class GrupoMapper
{
    // Header (listado): CantidadParticipantes se calcula, así que el mapeo es manual.
    public GrupoHeaderDto ToHeaderDto(Grupo entity) => new()
    {
        Id = entity.Id,
        EventoId = entity.EventoId,
        Nombre = entity.Nombre,
        CantidadParticipantes = entity.Participantes.Count,
    };

    // Detalle (getById / respuesta del update): los participantes ordenados como los ve el fotógrafo.
    public GrupoDto ToDto(Grupo entity) => new()
    {
        Id = entity.Id,
        EventoId = entity.EventoId,
        Nombre = entity.Nombre,
        CantidadParticipantes = entity.Participantes.Count,
        Participantes = entity.Participantes.OrderBy(a => a.Nombre).Select(this.ToDto).ToList(),
    };

    public ParticipanteDto ToDto(Participante entity)
    {
        var dto = this.MapParticipante(entity);
        // El código vigente: el activo más nuevo (puede haber viejos desactivados por reemplazo).
        dto.CodigoAcceso = entity.CodigosAcceso
            .Where(c => c.Activo)
            .OrderByDescending(c => c.CreadoEn)
            .FirstOrDefault()?.Codigo;
        return dto;
    }

    [MapperIgnoreSource(nameof(Participante.TenantId))]
    [MapperIgnoreSource(nameof(Participante.GrupoId))]
    [MapperIgnoreSource(nameof(Participante.Grupo))]
    [MapperIgnoreSource(nameof(Participante.CodigosAcceso))]
    [MapperIgnoreTarget(nameof(ParticipanteDto.CodigoAcceso))]
    private partial ParticipanteDto MapParticipante(Participante entity);
}
