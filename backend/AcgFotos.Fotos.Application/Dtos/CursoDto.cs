namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>Detalle para ver/editar: los datos del curso + sus álbumes con el código activo.</summary>
public class CursoDto : CursoHeaderDto
{
    public List<AlbumDto> Albumes { get; set; } = new();
}
