using AcgFotos.Core.AuditLog;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.Localization;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;
using LazyCache;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AcgFotos.Core.Security
{
    public class EndpointAuthoritation : AuthorizeAttribute, IAuthorizationFilter
    {
        // Higiene de memoria por defecto (NO frescura — esa la da la versión): desaloja del caché a los
        // usuarios inactivos ~1h. Configurable por "Authorization:CacheSlidingMinutes". Ver ADR-0003 §6.3.
        private const int DefaultCacheSlidingMinutes = 60;

        private readonly IConfiguration _configuration;
        private readonly IAppCache _cache;
        private readonly ISecurityRepository _securityRepository;
        private readonly IAuthzVersion _authzVersion;
        private readonly IAuditLogQueue _auditLogQueue;
        private readonly ITimeManager _timeManager;

        public EndpointAuthoritation(
            IConfiguration configuration,
            IAppCache cache,
            ISecurityRepository securityRepository,
            IAuthzVersion authzVersion,
            IAuditLogQueue auditLogQueue,
            ITimeManager timeManager)
        {
            _configuration = configuration;
            _cache = cache;
            _securityRepository = securityRepository;
            _authzVersion = authzVersion;
            _auditLogQueue = auditLogQueue;
            _timeManager = timeManager;
        }
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var hasAllowAnonymous = context.ActionDescriptor
                .EndpointMetadata.Any(em => em.GetType() == typeof(AllowAnonymousAttribute));

            if (hasAllowAnonymous) return;

            var authIsEnabled = _configuration.GetValue<bool>("AuthenticationEnabled");
            var checkPermissions = _configuration.GetValue<bool>("AuthorizationEnabled");


            if (authIsEnabled && checkPermissions)
            {
                var appContext = RequestHeaderHandler.Encode(context.HttpContext.Request);

                // Sesión de familia (ADR-11): no existe fila en gen_Usuarios, así que la matriz de
                // permisos de más abajo no tiene nada que consultar. Autorización propia: solo puede
                // pisar endpoints marcados explícitamente [AllowFamiliaSession]; todo lo demás es 403
                // aunque el JWT ya haya validado (rama aparte, NO un bypass de la matriz normal).
                if (appContext.IsFamiliaSession)
                {
                    var allowsFamiliaSession = context.ActionDescriptor
                        .EndpointMetadata.Any(em => em is AllowFamiliaSessionAttribute);

                    if (!allowsFamiliaSession)
                    {
                        context.Result = new ObjectResult(
                            ApiErrorResponse.From(MessagesAPI.ErrorUserNotPrivilegesAccess))
                        {
                            StatusCode = 403
                        };
                        this.AuditDenied(context, appContext);
                    }

                    return;
                }

                // En el caso de que el usuario sera Root le permito hacer todo más allá de que
                // no tenga los permisos asigandos.
                // Si es anónimo no valida permisos ya que se supone que si llego hasta ahí es porque el controller está decorado como allowAnomymous.
                // Si no lo debería haber filtrado la autenticación salvo que el isAuthEnable esté en false
                if (!appContext.IsAnonymous)
                {
                    if (appContext.CheckPemissions())
                    {
                        var authorizedSignatures = this.GetCachedAuthorizedEndpointsByUserName(appContext);

                        if (!IsGranted(authorizedSignatures, context))
                        {
                            // 403 (no 401): el usuario ESTÁ autenticado, lo que falta es el permiso.
                            // Con 401 el front hacía un refresh inútil; con 403 muestra el error directo.
                            // Va con la forma estándar { message, errors } para un mensaje claro de "sin permiso".
                            context.Result = new ObjectResult(
                                ApiErrorResponse.From(MessagesAPI.ErrorUserNotPrivilegesAccess))
                            {
                                StatusCode = 403
                            };
                            // El short-circuit de un IAuthorizationFilter saltea los result filters
                            // (AuditLogFilter incluido), así que el deny se audita acá (ADR-0005).
                            this.AuditDenied(context, appContext);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Audita el 403 de autorización (quién intentó qué endpoint sin permiso): el evento de
        /// seguridad más valioso del log y el primer dato para diagnosticar un seed mal mapeado.
        /// No lee el body (en la etapa de autorización no corresponde consumirlo): registra la ruta,
        /// el método, el usuario y el origen. Nunca propaga errores al pipeline.
        /// </summary>
        private void AuditDenied(AuthorizationFilterContext context, IAppContext appContext)
        {
            try
            {
                var request = context.HttpContext.Request;
                var descriptor = context.ActionDescriptor as ControllerActionDescriptor;

                _auditLogQueue.TryEnqueue(new AuditLogModel
                {
                    FechaHora = _timeManager.DateTimeNow,
                    Duracion = 0,
                    Servicio = descriptor?.ControllerName,
                    Metodo = descriptor?.ActionName,
                    Parametros = request.QueryString.Value,
                    UsuarioId = appContext.UserId,
                    ImpersonatedBy = appContext.ImpersonatedBy,
                    HttpMethod = request.Method,
                    RequestAbsolutePath = request.Path,
                    ClientIP = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ClientUserAgent = request.Headers["User-Agent"].ToString(),
                    ResultStatusCode = "403",
                    ResultContent = "Denegado por autorizacion de endpoint (sin permiso mapeado)"
                });
            }
            catch (Exception)
            {
                // El audit del deny no debe afectar la respuesta 401.
            }
        }

        private static bool IsGranted(HashSet<string> authorizedSignatures, AuthorizationFilterContext context)
        {
            if (context?.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                // El MISMO mapper que usa DiscoveryEndpoints para catalogar (ADR-0004): registro y
                // chequeo salen del mismo descriptor → el match es por construcción, sin drift posible.
                var currentEndpoint = EndpointDescriptorMapper.ToDto(descriptor);
                var currentSignature = EndpointDescriptorMapper.Signature(currentEndpoint);

                // O(1) contra el HashSet (antes era un .Any() lineal sobre la lista de endpoints).
                return authorizedSignatures.Contains(currentSignature);
            }

            return false;
        }

        /// <summary>
        /// Conjunto (cacheado) de firmas de los endpoints permitidos del usuario. La clave del caché lleva
        /// la VERSIÓN de autorización: cuando alguien cambia permisos/roles/grupos/licencias, el bump (en
        /// AcgFotosDbContext) sube la versión → la clave cambia → la próxima request recomputa con el cambio.
        /// El TTL es DESLIZANTE (sólo higiene de memoria: desaloja inactivos), no de frescura. Ver ADR-0003 §6.3.
        /// </summary>
        private HashSet<string> GetCachedAuthorizedEndpointsByUserName(IAppContext appContext)
        {
            var version = _authzVersion.Get();
            var key = $"endpoints:{appContext.UserName}:{appContext.TenantId}:{version}";

            var slidingMinutes = _configuration.GetValue<int?>("Authorization:CacheSlidingMinutes")
                                 ?? DefaultCacheSlidingMinutes;
            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(slidingMinutes)
            };

            var cachedResults = _cache.GetOrAdd(key, () => BuildAuthorizedSignatures(appContext), options);
            // No persistimos un set vacío (usuario sin endpoints o data drift): se reintenta en la próxima
            // request en vez de quedar pegado hasta que cambie la versión.
            if (cachedResults == null || cachedResults.Count == 0)
            {
                _cache.Remove(key);
            }
            return cachedResults ?? new HashSet<string>();
        }

        private HashSet<string> BuildAuthorizedSignatures(IAppContext appContext)
        {
            var endpoints = _securityRepository.GetAll(appContext.UserName, appContext.TenantId);
            var signatures = new HashSet<string>(endpoints.Count);
            foreach (var e in endpoints)
            {
                signatures.Add(EndpointDescriptorMapper.Signature(e));
            }
            return signatures;
        }
    }
}

