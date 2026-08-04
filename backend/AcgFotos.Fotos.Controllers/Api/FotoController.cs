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
            [FromForm] long grupoId,
            [FromForm] long? participanteId,
            List<IFormFile> archivos)
        {
            if (archivos == null || archivos.Count == 0)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorGeneric,
                    new List<string> { "No se recibió ningún archivo." });
            }

            var input = new SubirFotosInput { GrupoId = grupoId, ParticipanteId = participanteId };
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
        public async Task<ActionResult<List<FotoDto>>> Get([FromQuery] long grupoId, [FromQuery] long? participanteId)
        {
            return this.Ok(await _fotoAppService.ListarAsync(grupoId, participanteId));
        }

        // Derivados con watermark para la galería admin (404 hasta que el worker deje la foto
        // Lista: el front muestra "procesando" con el estado del listado y reintenta).
        [HttpGet]
        [Route("{id:long}/thumb")]
        public async Task<IActionResult> GetThumb(long id)
        {
            var bytes = await _fotoAppService.ObtenerThumbAsync(id);
            return bytes == null ? this.NotFound() : this.File(bytes, "image/webp");
        }

        [HttpGet]
        [Route("{id:long}/preview")]
        public async Task<IActionResult> GetPreview(long id)
        {
            var bytes = await _fotoAppService.ObtenerPreviewAsync(id);
            return bytes == null ? this.NotFound() : this.File(bytes, "image/webp");
        }

        /// <summary>El original limpio, SOLO admin (ADR-06): es lo que va al laboratorio a imprimir.</summary>
        [HttpGet]
        [Route("{id:long}/original")]
        public async Task<IActionResult> GetOriginal(long id)
        {
            var descarga = await _fotoAppService.ObtenerOriginalAsync(id);
            return descarga == null
                ? this.NotFound()
                : this.File(descarga.Contenido, "image/jpeg", descarga.NombreArchivo);
        }

        [HttpDelete]
        [Route("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            return await _fotoAppService.EliminarAsync(id)
                ? this.Ok(new { Success = true })
                : this.NotFound();
        }

        /// <summary>Conteo para el diálogo de confirmación, ANTES de encolar (spec `regeneracion-derivados`).</summary>
        [HttpGet]
        [Route("regenerar/conteo")]
        public async Task<ActionResult<int>> GetConteoParaRegenerar([FromQuery] long eventoId)
        {
            return this.Ok(await _fotoAppService.ContarParaRegenerarAsync(eventoId));
        }

        [HttpPost]
        [Route("regenerar")]
        public async Task<ActionResult<int>> Regenerar([FromQuery] long eventoId)
        {
            return this.Ok(await _fotoAppService.RegenerarAsync(eventoId));
        }
    }
}
