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
}
