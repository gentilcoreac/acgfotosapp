using AcgFotos.Core.Session;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using AcgFotos.Core.Logs;

namespace AcgFotos.Core.Exceptions
{
    /// <summary>
    /// Tener en cuenta que este filtro solamente esta tomando los excepciones no controladas. si dentro del controller estamos metiendo un try chach y desvolviendo un badRequest nunca va a pasar por este filtro y loguear el error
    /// </summary>
    public class CustomFilterExceptionAttribute : ExceptionFilterAttribute
    {
        private readonly ILogger _logger;
        private readonly IEnrichLogger _enrich;

        public CustomFilterExceptionAttribute(
            ILoggerFactory loggerFactory, 
            IEnrichLogger enrich)
        {
            _logger = loggerFactory.CreateLogger<ILogger>();
            _enrich = enrich;
        }

        public override void OnException(ExceptionContext context)
        {
            _enrich.EnrichProperties();
            _logger.LogError(context.Exception, context.Exception.Message);
        }
    }
}
