using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Controllers.Api
{
    /// <summary>CRUD de opciones de publicación (ADR-15 §5): resolución/calidad de los derivados, eje independiente de la marca de agua.</summary>
    [ApiController]
    [Route("api/fotos/marca-agua/opciones-publicacion")]
    public class OpcionesPublicacionController : EntityApiControllerBase<OpcionesPublicacion,
                                                                         OpcionesPublicacionDto,
                                                                         ListaPaginadaCriteriaBase,
                                                                         IOpcionesPublicacionAppService>
    {
        public OpcionesPublicacionController(IOpcionesPublicacionAppService opcionesPublicacionAppService)
            : base(opcionesPublicacionAppService)
        {
        }
    }
}
