namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>
/// Datos para imprimir las tarjetas de acceso de un curso (una por alumno, para repartir a las
/// familias): el front las renderiza e imprime; el QR ya viene generado como PNG en base64.
/// </summary>
public class TarjetasCursoDto
{
    public long CursoId { get; set; }
    public string NombreCurso { get; set; } = string.Empty;
    public string NombreEvento { get; set; } = string.Empty;

    public List<TarjetaAlbumDto> Tarjetas { get; set; } = new();
}

public class TarjetaAlbumDto
{
    public long AlbumId { get; set; }
    public string NombreAlumno { get; set; } = string.Empty;

    /// <summary>Código activo del álbum (null si el álbum no tiene ninguno activo).</summary>
    public string? Codigo { get; set; }

    /// <summary>URL de canje que codifica el QR (armada con <c>Fotos:UrlCanjeTemplate</c>).</summary>
    public string? UrlCanje { get; set; }

    /// <summary>PNG del QR en base64, listo para <c>&lt;img src="data:image/png;base64,..."&gt;</c>.</summary>
    public string? QrPngBase64 { get; set; }
}
