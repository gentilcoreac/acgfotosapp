using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AcgFotos.Core.AuditLog;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;

namespace AcgFotos.Core.Exceptions
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IAuditLogQueue _auditLogQueue;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILoggerFactory logger, IAuditLogQueue auditLogQueue)
        {
            _next = next;
            _logger = logger.CreateLogger<ILogger>();
            _auditLogQueue = auditLogQueue;
        }

        public async Task Invoke(HttpContext context)
        {
            var response = context.Response;
            response.StatusCode = (int)HttpStatusCode.OK;
            var startedAt = DateTime.Now;

            try
            {
                await _next(context);
            }

            // Business Validation Exception
            catch (BusinessValidationException ex)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.ContentType = "application/json";

                // Forma estándar { message, errors }: titular + detalle de validación (From quita el
                // titular del detalle). Sin traceId: es un error accionable, no algo que soporte rastree.
                var result = System.Text.Json.JsonSerializer.Serialize(
                    ApiErrorResponse.From(ex.Message, ex.Errors?.Select(e => e.ErrorMessage)));

                await context.Response.WriteAsync(result);

                this.AuditException(context, ex, startedAt);
            }

            // Forbidden (regla de negocio, no el catálogo de EndpointAuthoritation)
            catch (ForbiddenException ex)
            {
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.ContentType = "application/json";

                var result = System.Text.Json.JsonSerializer.Serialize(ApiErrorResponse.From(ex.Message));
                await context.Response.WriteAsync(result);

                this.AuditException(context, ex, startedAt);
            }

            catch (Exception ex)
            {
                response.ContentType = "application/json";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var message = ex.Message;

                if (ex is BusinessException ||
                     ex is KeyNotFoundException)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
                else if (ex is ArgumentException)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
                // ADO SQL Exceptions
                else if (ex is PostgresException)
                {
                    message = string.Format(MessagesAPI.ErrorSqlServer, ex.Message);
                }
                // EF Exceptions
                else if (ex is DbUpdateException)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;

                    // SqlState: código SQLSTATE de Postgres (equivalente al Number de SqlException de
                    // SQL Server). Referencia: https://www.postgresql.org/docs/current/errcodes-appendix.html
                    if (ex.InnerException is PostgresException pgEx)
                    {
                        switch (pgEx.SqlState)
                        {
                            case "23503": // foreign_key_violation
                                message = MessagesAPI.SqlError547;
                                break;
                            case "23505": // unique_violation
                                message = MessagesAPI.SqlError2627;
                                break;
                            case "3D000": // invalid_catalog_name
                                message = MessagesAPI.SqlError4060;
                                break;
                            case "28P01": // invalid_password
                            case "28000": // invalid_authorization_specification
                                message = MessagesAPI.SqlError18456;
                                break;
                            case "23502": // not_null_violation
                                message = MessagesAPI.SqlError515;
                                break;
                            case "22001": // string_data_right_truncation
                                message = MessagesAPI.SqlError8152;
                                break;
                            case "42P01": // undefined_table
                                message = MessagesAPI.SqlError208;
                                break;
                            default:
                                message = MessagesAPI.SqlErrorDefault;
                                break;
                        }
                    }
                    else
                    {
                        message = MessagesAPI.SqlErrorUnknown;
                    }
                }

                // Invalid operation
                else if (ex is InvalidOperationException)
                {
                    message = string.Format(MessagesAPI.ErrorInvalidOperation, ex.Message);
                }
                // Other internal exceptions
                else
                {
                    message = MessagesAPI.ErrorGenericInternal;
                }

                // Forma estándar { message, errors, traceId }: error técnico → message amable, sin detalle
                // accionable, y un traceId de correlación para que soporte lo cruce con el audit log.
                var result = System.Text.Json.JsonSerializer.Serialize(
                    ApiErrorResponse.Technical(message, context.TraceIdentifier));
                await context.Response.WriteAsync(result);

                this.AuditException(context, ex, startedAt);
            }
        }

        /// <summary>
        /// Audita la request que terminó en excepción (ADR-0005): en el path de excepción los
        /// result filters no corren, así que el AuditLogFilter nunca la registraría y los
        /// "problemas" — justo lo que root mira en el log — quedaban invisibles. Registra tipo y
        /// mensaje de la excepción (el stack completo ya va a gen_LogInfos vía Serilog en
        /// CustomFilterExceptionAttribute). Nunca propaga errores: la respuesta ya fue escrita.
        /// </summary>
        private void AuditException(HttpContext context, Exception exception, DateTime startedAt)
        {
            try
            {
                var descriptor = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
                var appContext = RequestHeaderHandler.Encode(context.Request);

                var model = new AuditLogModel
                {
                    FechaHora = DateTime.Now,
                    Duracion = (DateTime.Now - startedAt).TotalSeconds,
                    Servicio = descriptor?.ControllerName,
                    Metodo = descriptor?.ActionName,
                    Parametros = context.Request.QueryString.Value,
                    HttpMethod = context.Request.Method,
                    RequestAbsolutePath = context.Request.Path,
                    ClientIP = context.Connection.RemoteIpAddress?.ToString(),
                    ClientUserAgent = context.Request.Headers["User-Agent"].ToString(),
                    ResultStatusCode = context.Response.StatusCode.ToString(),
                    // Incluye el traceId que el cliente ve en el toast de errores técnicos: así soporte
                    // localiza esta entrada del audit log a partir del "ref" que le pasa el usuario.
                    ResultContent = $"{exception.GetType().Name}: {exception.Message} [traceId: {context.TraceIdentifier}]"
                };

                if (appContext != null && !appContext.IsAnonymous)
                {
                    model.UsuarioId = appContext.UserId;
                    model.ImpersonatedBy = appContext.ImpersonatedBy;
                }

                _auditLogQueue.TryEnqueue(model);
            }
            catch (Exception)
            {
                // El audit de la excepción no debe generar una segunda falla.
            }
        }
    }
}
