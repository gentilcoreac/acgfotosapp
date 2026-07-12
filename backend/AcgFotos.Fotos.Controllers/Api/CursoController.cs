using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Controllers;
using AcgFotos.Fotos.Application.Criterias;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Controllers.Api
{
    [ApiController]
    [Route("api/fotos/cursos")]
    public class CursoController : ExtendedEntityApiControllerBase<Curso,
                                                                   CursoInputDto,
                                                                   CursoDto,
                                                                   CursoHeaderDto,
                                                                   CursoCriteria,
                                                                   ICursoAppService>
    {
        public CursoController(ICursoAppService cursoAppService) : base(cursoAppService)
        {
        }

        /// <summary>Tarjetas imprimibles del curso: una por alumno con código y QR de canje.</summary>
        [HttpGet]
        [Route("{id:long}/tarjetas")]
        public async Task<ActionResult<TarjetasCursoDto>> GetTarjetas(long id)
        {
            var tarjetas = await this.AppService.GetTarjetasAsync(id);
            return tarjetas == null ? this.NotFound() : this.Ok(tarjetas);
        }
    }
}
