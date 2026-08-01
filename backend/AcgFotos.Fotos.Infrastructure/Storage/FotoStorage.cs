using System.Reflection;
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
    private const string WebpContentType = "image/webp";
    private const string NombreAssetCapaPorDefecto = "marca-agua-default.png";

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

    public Task<byte[]> LeerThumbAsync(Foto foto) =>
        _storageProvider.ReadAsync(PrefijoPrivado + FotoStorageKeys.Thumb(foto));

    public Task<byte[]> LeerPreviewAsync(Foto foto) =>
        _storageProvider.ReadAsync(PrefijoPrivado + FotoStorageKeys.Preview(foto));

    public async Task EliminarAsync(Foto foto)
    {
        // DeleteAsync del provider no falla si la key no existe (una foto Pendiente/Error
        // puede no tener derivados todavía).
        await _storageProvider.DeleteAsync(PrefijoPrivado + FotoStorageKeys.Original(foto));
        await _storageProvider.DeleteAsync(PrefijoPrivado + FotoStorageKeys.Thumb(foto));
        await _storageProvider.DeleteAsync(PrefijoPrivado + FotoStorageKeys.Preview(foto));
    }

    public async Task GuardarDerivadosAsync(Foto foto, DerivadosFoto derivados)
    {
        await _storageProvider.SaveAsync(
            PrefijoPrivado + FotoStorageKeys.Preview(foto),
            derivados.Preview,
            WebpContentType,
            StorageVisibility.Private);

        await _storageProvider.SaveAsync(
            PrefijoPrivado + FotoStorageKeys.Thumb(foto),
            derivados.Thumb,
            WebpContentType,
            StorageVisibility.Private);
    }

    public Task<byte[]> LeerCapaMarcaAguaAsync(long perfilMarcaAguaId, Guid storageKey) =>
        _storageProvider.ReadAsync(PrefijoPrivado + FotoStorageKeys.CapaMarcaAgua(perfilMarcaAguaId, storageKey));

    public async Task<byte[]> LeerCapaMarcaAguaDefaultAsync()
    {
        var assembly = typeof(FotoStorage).Assembly;
        var name = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith(NombreAssetCapaPorDefecto, StringComparison.OrdinalIgnoreCase));
        await using var stream = assembly.GetManifestResourceStream(name)!;
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}
