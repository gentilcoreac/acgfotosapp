using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Controllers.Api
{
    /// <summary>
    /// CRUD de perfiles de marca de agua (ADR-15) + subida/lectura de los assets PNG de sus capas.
    /// El alta de una capa (y, si hace falta, la del perfil que la contiene) va por
    /// <see cref="SubirCapa"/> — ver design.md D14, no el <c>Update</c> heredado.
    /// </summary>
    [ApiController]
    [Route("api/fotos/marca-agua/perfiles")]
    public class PerfilMarcaAguaController : ExtendedEntityApiControllerBase<PerfilMarcaAgua,
                                                                             PerfilMarcaAguaInputDto,
                                                                             PerfilMarcaAguaDto,
                                                                             PerfilMarcaAguaDto,
                                                                             ListaPaginadaCriteriaBase,
                                                                             IPerfilMarcaAguaAppService>
    {
        // Un logo con margen de sobra contra el techo configurado (OpcionesFotos, default 5 MB).
        private const long MaxRequestBytes = 20L * 1024 * 1024;

        public PerfilMarcaAguaController(IPerfilMarcaAguaAppService perfilMarcaAguaAppService)
            : base(perfilMarcaAguaAppService)
        {
        }

        [HttpPost]
        [Route("capas/upload")]
        [RequestSizeLimit(MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
        public async Task<ActionResult<CapaMarcaAguaSubidaDto>> SubirCapa(
            [FromForm] long? perfilMarcaAguaId,
            [FromForm] string? nombrePerfilSiNuevo,
            IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorGeneric,
                    new List<string> { "No se recibió ningún archivo." });
            }

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);

            var input = new SubirCapaMarcaAguaInput
            {
                PerfilMarcaAguaId = perfilMarcaAguaId,
                NombrePerfilSiNuevo = nombrePerfilSiNuevo,
                Contenido = ms.ToArray(),
            };

            return this.Ok(await this.AppService.SubirCapaAsync(input));
        }

        /// <summary>Lectura autenticada del asset de una capa, para el editor (nunca pública).</summary>
        [HttpGet]
        [Route("{perfilId:long}/capas/{storageKey:guid}")]
        public async Task<IActionResult> GetAssetCapa(long perfilId, Guid storageKey)
        {
            var bytes = await this.AppService.ObtenerAssetCapaAsync(perfilId, storageKey);
            return bytes == null ? this.NotFound() : this.File(bytes, "image/png");
        }
    }
}
