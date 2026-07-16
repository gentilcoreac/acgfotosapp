namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>Detalle para ver/editar: los datos del grupo + sus participantes con el código activo.</summary>
public class GrupoDto : GrupoHeaderDto
{
    public List<ParticipanteDto> Participantes { get; set; } = new();
}
