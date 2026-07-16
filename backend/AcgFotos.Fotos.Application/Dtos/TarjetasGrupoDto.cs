namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>
/// Datos para imprimir las tarjetas de acceso de un grupo (una por participante, para repartir a las
/// familias): el front las renderiza e imprime; el QR ya viene generado como PNG en base64.
/// </summary>
public class TarjetasGrupoDto
{
    public long GrupoId { get; set; }
    public string NombreGrupo { get; set; } = string.Empty;
    public string NombreEvento { get; set; } = string.Empty;

    public List<TarjetaParticipanteDto> Tarjetas { get; set; } = new();
}

public class TarjetaParticipanteDto
{
    public long ParticipanteId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Código activo del participante (null si el participante no tiene ninguno activo).</summary>
    public string? Codigo { get; set; }

    /// <summary>URL de canje que codifica el QR (armada con <c>Fotos:UrlCanjeTemplate</c>).</summary>
    public string? UrlCanje { get; set; }

    /// <summary>PNG del QR en base64, listo para <c>&lt;img src="data:image/png;base64,..."&gt;</c>.</summary>
    public string? QrPngBase64 { get; set; }
}
