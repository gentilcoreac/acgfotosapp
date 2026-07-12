using AcgFotos.Core.Application;
using AcgFotos.Core.Domain;
using AcgFotos.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using System.Text;
using System;
using CsvHelper.Configuration;
using AcgFotos.Core.Localization.APIResources;

namespace AcgFotos.Core.Controllers
{
    public class ExtendedEntityApiControllerBase<E, I, O, R, C, S> : ApiControllerBase
        where E : class, IEntityBase, new()
        where I : class, IDto, new()
        where O : class, IDto, new()
        where R : class, IDto, new()
        where C : class, IListaPaginadaCriteriaBase, new()
        where S : class, IExtendedEntityAppServiceBase<E, I, O, R, C>
    {

        protected S AppService { get; }

        public ExtendedEntityApiControllerBase(S appService)
        {
            this.AppService = appService;
        }

        [HttpPost]
        [Route("update")]
        public virtual async Task<ActionResult<O>> Update([FromBody] I dto)
        {
            // Los controllers con [ApiController] no necesitan comprobar ModelState.IsValid — ASP.NET
            // devuelve 400 automático con ValidationProblemDetails cuando el modelo es inválido.
            // El chequeo manual queda como defensa por si algún derivado se monta sin [ApiController].
            // Ver: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation#automatic-http-400-responses
            if (!this.ModelState.IsValid)
            {
                var errors = this.ModelState.Keys.SelectMany(k => this.ModelState[k].Errors).
                                            Where(x => !string.IsNullOrEmpty(x.ErrorMessage)).
                                            Select(m => m.ErrorMessage).ToArray();
                if (errors.Length == 0)
                {
                    errors = this.ModelState.Keys.Select(x => "Problema con el campo: " + x).ToArray();
                }
                throw new BusinessValidationException(MessagesAPI.ErrorGeneric, errors.ToList());
            }

            var dtoUpdated = await this.AppService.UpdateAsync(dto);
            return this.Ok(dtoUpdated);
        }

        [HttpDelete]
        [Route("{id:int}")]
        public virtual async Task<IActionResult> Delete(long id)
        {
            await this.AppService.DeleteByIdAsync(id);
            return this.Ok(new { Success = true });
        }

        [HttpGet]
        [Route("{id:int}")]
        public virtual async Task<ActionResult<O>> Get(long id)
        {
            var dto = await this.AppService.GetByIdAsync(id);
            if (dto == null)
            {
                return this.NotFound();
            }
            return this.Ok(dto);
        }

        [HttpGet]
        [Route("")]
        public virtual async Task<ActionResult<PaginationSet<R>>> Get([FromQuery] C criteria)
        {
            return await this.AppService.SearchAsync(criteria);
        }

        [HttpGet]
        [Route("csv")]
        public virtual async Task<IActionResult> GetToCsv([FromQuery] C criteria)
        {
            var rta = await this.AppService.SearchAsync(criteria);

            var cc = new CsvConfiguration(new System.Globalization.CultureInfo("es-AR"));

            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(stream: ms, encoding: new UTF8Encoding(true)))
                {
                    using (var cw = new CsvWriter(sw, cc))
                    {
                        cw.WriteRecords(rta.Items);
                    }
                    return this.File(ms.ToArray(), "text/csv", $"export_{DateTime.UtcNow.Ticks}.csv");
                }
            }
        }
    }
}
