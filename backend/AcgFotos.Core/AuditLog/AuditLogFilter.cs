using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using AcgFotos.Core.Localization;
using AcgFotos.Core.Session;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace AcgFotos.Core.AuditLog
{
    public class AuditLogFilterAttribute : ActionFilterAttribute
    {
        private DateTime _startDateTime;
        private readonly IAuditLogQueue _auditLogQueue;
        private readonly IAppContext _appContext;
        private readonly ITimeManager _timeManager;

        public AuditLogFilterAttribute(
            IAppContext appContext,
            IAuditLogQueue auditLogQueue,
            ITimeManager timeManager)
        {
            _appContext = appContext;
            _auditLogQueue = auditLogQueue;
            _timeManager = timeManager;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _startDateTime = _timeManager.DateTimeNow;
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            try
            {
                var timeEnd = _timeManager.DateTimeNow;
                var duracion = (timeEnd - _startDateTime).TotalSeconds;
                this.WriteTransaction(context, duracion);
            }
            catch (Exception)
            {
                // Silenciado intencionalmente: un fallo del audit log no debe propagar a la request
                // del usuario. Si surge necesidad de diagnóstico, inyectar ILogger y loguear acá.
            }

            base.OnResultExecuted(context);
        }

        private void WriteTransaction(ResultExecutedContext filterContext, double duracion)
        {
            var audioLogModel = new AuditLogModel
            {
                Servicio = ((ControllerBase)filterContext.Controller).ControllerContext.ActionDescriptor.ControllerName,
                Metodo = ((ControllerBase)filterContext.Controller).ControllerContext.ActionDescriptor.ActionName,
                // Null-safe: si no hay IP remota (p.ej. TestServer, o un transporte sin peer), NO debe
                // tirar NRE y perder silenciosamente la auditoria. Coherente con AuditDenied (que ya usa ?.).
                ClientIP = filterContext.HttpContext.Connection.RemoteIpAddress?.ToString(),
                ResultStatusCode = filterContext.HttpContext.Response.StatusCode.ToString(),
                ClientUserAgent = filterContext.HttpContext.Request.Headers["User-Agent"].ToString(),
                HttpMethod = filterContext.HttpContext.Request.Method,
                Duracion = duracion,
                RequestAbsolutePath = filterContext.HttpContext.Request.Path,
                FechaHora = _timeManager.DateTimeNow,
            };

            if (!_appContext.IsAnonymous)
            {
                audioLogModel.UsuarioId = _appContext.UserId;
                // Impersonalización (ADR-0002): UsuarioId queda con la identidad efectiva (usuario destino)
                // e ImpersonatedBy con el root real → doble atribución en el audit log.
                audioLogModel.ImpersonatedBy = _appContext.ImpersonatedBy;
            }

            var controllerName = ((ControllerBase)filterContext.Controller).ControllerContext.ActionDescriptor.ControllerName;
            var hasSensitiveAttribute = HasSensitiveAttribute(filterContext.ActionDescriptor);
            var isSensitive = hasSensitiveAttribute || AuditLogSanitizer.IsSensitiveController(controllerName);

            if (filterContext.Result is ObjectResult objectResult)
            {
                if (objectResult.Value != null)
                {
                    var type = objectResult.Value.GetType();
                    try
                    {
                        if (type.Name != "MemoryStream")
                        {
                            var jsonString = JsonSerializer.Serialize(objectResult.Value);
                            audioLogModel.ResultContent = AuditLogSanitizer.SanitizeResponse(jsonString, isSensitive);
                        }
                    }
                    catch (Exception)
                    {
                        // Silenciado intencionalmente: si el ResultContent no se puede serializar
                        // (ej. referencias circulares), el audit log se guarda sin él.
                    }
                }
            }

            if (filterContext.HttpContext.Request.Method == HttpMethod.Post.ToString())
            {
                string rawRequest;
                filterContext.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                using (var stream = new StreamReader(filterContext.HttpContext.Request.Body))
                {
                    rawRequest = stream.ReadToEnd();
                }
                var bodyOrQuery = string.IsNullOrEmpty(rawRequest)
                    ? filterContext.HttpContext.Request.Query.ToString()
                    : rawRequest;
                audioLogModel.Parametros = AuditLogSanitizer.SanitizeRequestBody(bodyOrQuery, isSensitive);
            }
            else
            {
                audioLogModel.Parametros = filterContext.HttpContext.Request.QueryString.Value;
            }

            // Encola y devuelve: la persistencia la hace AuditLogBackgroundWriter en lote,
            // fuera del camino del request (ADR-0005).
            _auditLogQueue.TryEnqueue(audioLogModel);
        }

        private static bool HasSensitiveAttribute(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor descriptor)
        {
            if (descriptor is not ControllerActionDescriptor cad)
            {
                return false;
            }

            return cad.MethodInfo.GetCustomAttribute<SensitiveEndpointAttribute>(inherit: true) != null
                || cad.ControllerTypeInfo.GetCustomAttribute<SensitiveEndpointAttribute>(inherit: true) != null;
        }
    }
}
