using AcgFotos.Core.Storage;
using AcgFotos.Fotos.Application.Imaging;
using AcgFotos.Fotos.Application.Storage;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Storage;

/// <summary>
/// Adaptador del vertical sobre el <see cref="IStorageProvider"/> de la plataforma. TODAS las keys
/// van con prefijo <c>private/</c>: en FileSystem eso las manda a App_Data (fuera de wwwroot) y en
/// el provider S3/R2 futuro al bucket privado — nada de fotos queda jamás servible estáticamente.
/// </summary>
public class FotoStorage : IFotoStorage
{
    private const string PrefijoPrivado = "private/";
    private const string JpegContentType = "image/jpeg";

    private readonly IStorageProvider _storageProvider;

    public FotoStorage(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public Task GuardarOriginalAsync(Foto foto, byte[] contenido) =>
        _storageProvider.SaveAsync(
            PrefijoPrivado + FotoStorageKeys.Original(foto),
            contenido,
            JpegContentType,
            StorageVisibility.Private);

    public Task<byte[]> LeerOriginalAsync(Foto foto) =>
        _storageProvider.ReadAsync(PrefijoPrivado + FotoStorageKeys.Original(foto));

    public async Task GuardarDerivadosAsync(Foto foto, DerivadosFoto derivados)
    {
        await _storageProvider.SaveAsync(
            PrefijoPrivado + FotoStorageKeys.Preview(foto),
            derivados.PreviewJpeg,
            JpegContentType,
            StorageVisibility.Private);

        await _storageProvider.SaveAsync(
            PrefijoPrivado + FotoStorageKeys.Thumb(foto),
            derivados.ThumbJpeg,
            JpegContentType,
            StorageVisibility.Private);
    }
}
