using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Inputs;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;

namespace AcgFotos.Base.Controllers.Api
{
    [ApiController]
    [Route("api/general/permisos")]
    public class PermisoController : ExtendedEntityApiControllerBase<Permiso,
                                                                     PermisoInputDto,
                                                                     PermisoDto,
                                                                     PermisoHeaderDto,
                                                                     ListaPaginadaCriteriaBase,
                                                                     IPermisoAppService>
    {
        public PermisoController(IPermisoAppService permisoAppService) : base(permisoAppService)
        {
        }

        [Route("hierarchical-items")]
        [HttpGet]
        public async Task<IActionResult> HierarchicalItems()
        {
            var result = await this.AppService.GetHierarchicalItemsAsync();
            return this.Ok(result);
        }

        /// <summary>
        /// Cambia el flag EsRestringido de un permiso. Endpoint dedicado, separado del Update normal:
        /// el PermisoInputDto NO expone EsRestringido para defender mass assignment a nivel de tipo.
        /// Solo root puede tocarlo (validación en AppService).
        /// </summary>
        [HttpPost]
        [Route("{id}/set-es-restringido")]
        public async Task<IActionResult> SetEsRestringido(long id, [FromBody] SetEsRestringidoInput input)
        {
            await this.AppService.SetEsRestringidoAsync(id, input.EsRestringido);
            return this.Ok();
        }
    }
}
