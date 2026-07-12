using AcgFotos.Core.Session;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System;
using AcgFotos.Core.Controllers;

namespace AcgFotos.Core.Logs
{
    public class LoggerActionFilterAttribute : ActionFilterAttribute
    {
        private readonly ILogger _logger;
        private readonly IEnrichLogger _enrich;

        public LoggerActionFilterAttribute(
            ILoggerFactory loggerFactory, 
            IEnrichLogger enrich)
        {
            _logger = loggerFactory.CreateLogger<ILogger>();
            _enrich = enrich;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _enrich.EnrichProperties();
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result?.GetType().Name == "BadRequestObjectResult")
            {
                if (context.Result is BadRequestObjectResult badRequest)
                {
                    if (badRequest.Value is Exception exception)
                    {
                        _logger.LogError(exception, exception.Message);
                    }
                    if (badRequest.Value is ResponseModelBase responseModelBase)
                    {
                        if (responseModelBase.Exception != null)
                        {
                            _logger.LogError(responseModelBase.Exception,
                                            string.Join(";", responseModelBase.Errors));
                        }
                        else
                        {
                            _logger.LogError(string.Join(";", responseModelBase.Errors));
                        }
                    }
                }
            }
            base.OnActionExecuted(context);
        }
    }
}
