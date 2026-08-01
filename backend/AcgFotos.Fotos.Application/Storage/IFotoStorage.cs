using AcgFotos.Fotos.Application.Imaging;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Storage;

/// <summary>
/// Puerto de storage del vertical: TODO lo de fotos vive en la zona privada del
/// <c>IStorageProvider</c> (ADR-05/ADR-06) — los originales no se sirven jamás y los derivados
/// solo por endpoint autenticado o URL firmada. Las keys las arma <see cref="FotoStorageKeys"/>.
/// </summary>
public interface IFotoStorage
{
    Task GuardarOriginalAsync(Foto foto, byte[] contenido);

    Task<byte[]> LeerOriginalAsync(Foto foto);

    /// <summary>Guarda preview y thumb (ambos JPEG con watermark) del resultado del pipeline.</summary>
    Task GuardarDerivadosAsync(Foto foto, DerivadosFoto derivados);

    Task<byte[]> LeerThumbAsync(Foto foto);

    Task<byte[]> LeerPreviewAsync(Foto foto);

    /// <summary>Borra original + derivados (los que existan) — el borrado de una foto limpia todo.</summary>
    Task EliminarAsync(Foto foto);

    /// <summary>Bytes del asset PNG de una capa de marca de agua ya subida.</summary>
    Task<byte[]> LeerCapaMarcaAguaAsync(long perfilMarcaAguaId, Guid storageKey);

    /// <summary>
    /// Capa embebida en el binario para el último escalón de la cascada (sin perfiles cargados):
    /// reproduce el watermark de texto del pipeline pre-ADR-15, congelado a PNG. No es storage real
    /// (no hay tenant ni upload de por medio) — vive acá para que <c>IImageProcessor</c> reciba
    /// siempre bytes de capa por el mismo camino, sin una rama especial.
    /// </summary>
    Task<byte[]> LeerCapaMarcaAguaDefaultAsync();
}
