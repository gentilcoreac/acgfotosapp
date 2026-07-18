using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.Security;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;

namespace AcgFotos.Fotos.Controllers.Api
{
    /// <summary>
    /// Galería de la sesión de familia (ADR-11, Fase 2): consume el JWT que emite el canje
    /// (<c>api/fotos/canje</c>). Requiere <see cref="AllowFamiliaSessionAttribute"/> en cada acción —
    /// una sesión de familia no tiene fila en gen_Usuarios, así que la matriz de permisos del admin no
    /// aplica (ver <c>EndpointAuthoritation</c>); sin la marca, 403. Solo derivados con watermark
    /// (thumb/preview) — el original NUNCA se expone acá (ADR-01/ADR-06, ver <c>FotoController</c>).
    /// </summary>
    [ApiController]
    [Route("api/fotos/familia/fotos")]
    public class FamiliaGaleriaController : ApiControllerBase
    {
        private readonly IFamiliaGaleriaAppService _familiaGaleriaAppService;

        public FamiliaGaleriaController(IFamiliaGaleriaAppService familiaGaleriaAppService)
        {
            _familiaGaleriaAppService = familiaGaleriaAppService;
        }

        [HttpGet]
        [Route("")]
        [AllowFamiliaSession]
        public async Task<ActionResult<List<FotoDto>>> Get()
        {
            return this.Ok(await _familiaGaleriaAppService.ListarAsync());
        }

        [HttpGet]
        [Route("{id:long}/thumb")]
        [AllowFamiliaSession]
        public async Task<IActionResult> GetThumb(long id)
        {
            var bytes = await _familiaGaleriaAppService.ObtenerThumbAsync(id);
            return bytes == null ? this.NotFound() : this.File(bytes, "image/webp");
        }

        [HttpGet]
        [Route("{id:long}/preview")]
        [AllowFamiliaSession]
        public async Task<IActionResult> GetPreview(long id)
        {
            var bytes = await _familiaGaleriaAppService.ObtenerPreviewAsync(id);
            return bytes == null ? this.NotFound() : this.File(bytes, "image/webp");
        }
    }
}
