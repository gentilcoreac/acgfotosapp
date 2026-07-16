using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Controllers;
using AcgFotos.Fotos.Application.Criterias;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Controllers.Api
{
    [ApiController]
    [Route("api/fotos/grupos")]
    public class GrupoController : ExtendedEntityApiControllerBase<Grupo,
                                                                   GrupoInputDto,
                                                                   GrupoDto,
                                                                   GrupoHeaderDto,
                                                                   GrupoCriteria,
                                                                   IGrupoAppService>
    {
        public GrupoController(IGrupoAppService grupoAppService) : base(grupoAppService)
        {
        }

        /// <summary>Tarjetas imprimibles del grupo: una por participante con código y QR de canje.</summary>
        [HttpGet]
        [Route("{id:long}/tarjetas")]
        public async Task<ActionResult<TarjetasGrupoDto>> GetTarjetas(long id)
        {
            var tarjetas = await this.AppService.GetTarjetasAsync(id);
            return tarjetas == null ? this.NotFound() : this.Ok(tarjetas);
        }
    }
}
