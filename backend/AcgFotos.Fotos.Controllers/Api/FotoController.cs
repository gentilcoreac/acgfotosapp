using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;

namespace AcgFotos.Fotos.Controllers.Api
{
    /// <summary>
    /// Upload masivo y listado de fotos del admin. No es un CRUD Extended: la foto se sube y se
    /// procesa; no se edita. Los bytes de imagen NUNCA salen por acá (regla de performance:
    /// la API no proxya imágenes; la galería admin definirá su propia entrega).
    /// </summary>
    [ApiController]
    [Route("api/fotos/fotos")]
    public class FotoController : ApiControllerBase
    {
        // Un lote grande de cámara (~20 MB por JPEG) entra igual; el front sube de a tandas.
        private const long MaxRequestBytes = 512L * 1024 * 1024;

        private readonly IFotoAppService _fotoAppService;

        public FotoController(IFotoAppService fotoAppService)
        {
            _fotoAppService = fotoAppService;
        }

        [HttpPost]
        [Route("upload")]
        [RequestSizeLimit(MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
        public async Task<ActionResult<List<FotoDto>>> Upload(
            [FromForm] long cursoId,
            [FromForm] long? albumId,
            List<IFormFile> archivos)
        {
            if (archivos == null || archivos.Count == 0)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorGeneric,
                    new List<string> { "No se recibió ningún archivo." });
            }

            var input = new SubirFotosInput { CursoId = cursoId, AlbumId = albumId };
            foreach (var archivo in archivos)
            {
                using var ms = new MemoryStream();
                await archivo.CopyToAsync(ms);
                input.Archivos.Add(new ArchivoFotoInput(archivo.FileName, ms.ToArray()));
            }

            return this.Ok(await _fotoAppService.SubirAsync(input));
        }

        [HttpGet]
        [Route("")]
        public async Task<ActionResult<List<FotoDto>>> Get([FromQuery] long cursoId, [FromQuery] long? albumId)
        {
            return this.Ok(await _fotoAppService.ListarAsync(cursoId, albumId));
        }
    }
}
