namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>
/// Input de subida del PNG de una capa (design.md D14), ya leído del multipart. Sin
/// <see cref="PerfilMarcaAguaId"/> crea el perfil en el mismo paso.
/// </summary>
public class SubirCapaMarcaAguaInput
{
    public long? PerfilMarcaAguaId { get; set; }
    public string? NombrePerfilSiNuevo { get; set; }
    public byte[] Contenido { get; set; } = Array.Empty<byte>();
}
