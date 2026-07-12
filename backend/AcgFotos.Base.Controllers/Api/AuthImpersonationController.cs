using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Identity;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Core.AuditLog;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.ExtensionMethods;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Security;
using AcgFotos.Core.Session;
using AcgFotos.Base.Infrastructure.Repositories;

namespace AcgFotos.Base.Controllers.Api
{
    /// <summary>
    /// Impersonalización (ADR-0002): un usuario <b>root</b> opera "como" un usuario concreto de otro tenant.
    /// El token resultante va scopeado al usuario destino (<c>isRoot=false</c>) + claim firmado
    /// <c>impersonatedBy</c> (mínimo privilegio + auditoría), en lugar de los headers <c>Simulated*</c>
    /// client-controlled. La sesión se sostiene a través de los refresh por una cookie-overlay firmada
    /// (ver <c>AuthController.Refresh</c> + <see cref="IImpersonationManager"/>).
    ///
    /// Rutas agrupadas bajo <c>api/auth/impersonation/*</c> (<c>start</c>/<c>stop</c>/<c>users</c>); se separó
    /// del <c>AuthController</c> por cohesión (era un controller con muchos métodos).
    /// </summary>
    [Produces("application/json")]
    [Route("api/auth/impersonation")]
    [SensitiveEndpoint]
    // Sin rate limiting estricto: son acciones autenticadas de root (no fuerza bruta). Quedan bajo el
    // límite global; root navegando/impersonando rápido ya no trip el balde de auth.
    public class AuthImpersonationController : ApiControllerBase
    {
        private readonly IAppContext _appContext;
        private readonly IConfiguration _configuration;
        private readonly IJwtSecurityTokenFactory _jwtSecurityTokenFactory;
        private readonly ITenantAppService _tenantAppService;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IImpersonationManager _impersonation;

        public AuthImpersonationController(
            IAppContext appContext,
            IConfiguration configuration,
            IJwtSecurityTokenFactory jwtSecurityTokenFactory,
            ITenantAppService tenantAppService,
            IUsuarioRepository usuarioRepository,
            IImpersonationManager impersonation)
        {
            _appContext = appContext;
            _configuration = configuration;
            _jwtSecurityTokenFactory = jwtSecurityTokenFactory;
            _tenantAppService = tenantAppService;
            _usuarioRepository = usuarioRepository;
            _impersonation = impersonation;
        }

        [HttpPost]
        [ProducesResponseType(typeof(TokenModelDto), 200)]
        [Route("start")]
        [Authorize]
        public async Task<IActionResult> Impersonate([FromBody] ImpersonateViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(ApiErrorResponse.FromMany(this.ModelState.GetErrors()));
            }

            // El "root real": si el token es root, es el usuario logueado; si ya está impersonando, es el
            // impersonatedBy (su identidad subyacente). Permite re-impersonar para cambiar de destino.
            var realRootUserId = _impersonation.ResolveRealRootUserId();
            if (realRootUserId == null)
            {
                return this.StatusCode((int)HttpStatusCode.Forbidden, ApiErrorResponse.From(MessagesAPI.ErrorDataInvalid));
            }

            // Defensa en profundidad: el root real debe seguir siendo root (el claim ya lo garantiza, pero
            // se revalida contra DB por si el usuario perdió el rol root entre tanto).
            var rootTenantId = _configuration.GetValue<long>("RootTenantId");
            var rootUser = await _usuarioRepository.GetByIdIgnoringTenantFilterAsync(realRootUserId.Value);
            if (rootUser == null || rootUser.TenantId != rootTenantId)
            {
                return this.StatusCode((int)HttpStatusCode.Forbidden, ApiErrorResponse.From(MessagesAPI.ErrorDataInvalid));
            }

            var target = await _usuarioRepository.GetByIdIgnoringTenantFilterAsync(model.UserId);
            if (target == null || target.TenantId != model.TenantId)
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorUserNotFound));
            }

            if (!(await _tenantAppService.IsActiveAsync(model.TenantId)))
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorTenantNotActive));
            }

            // La licencia del usuario destino NO bloquea la impersonalización: es una herramienta de root
            // para diagnosticar/configurar el tenant (ver ADR-0002).
            var tokenModel = _jwtSecurityTokenFactory.CreateInstance(target.Id,
                                                                     target.UserName,
                                                                     target.Email,
                                                                     target.TenantId,
                                                                     target.Administrador,
                                                                     target.SecurityStamp,
                                                                     impersonatedBy: realRootUserId.Value);
            _impersonation.SetOverlayCookie(target.TenantId, target.Id, realRootUserId.Value);
            return this.Ok(_impersonation.ToDto(tokenModel));
        }

        [HttpPost]
        [ProducesResponseType(typeof(TokenModelDto), 200)]
        [Route("stop")]
        [Authorize]
        public async Task<IActionResult> StopImpersonation()
        {
            if (!_appContext.ImpersonatedBy.HasValue)
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorDataInvalid));
            }

            _impersonation.ClearOverlayCookie();

            var rootUser = await _usuarioRepository.GetByIdIgnoringTenantFilterAsync(_appContext.ImpersonatedBy.Value);
            if (rootUser == null)
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorUserNotFound));
            }

            // Token normal del root (sin claim impersonatedBy).
            var tokenModel = _jwtSecurityTokenFactory.CreateInstance(rootUser.Id,
                                                                     rootUser.UserName,
                                                                     rootUser.Email,
                                                                     rootUser.TenantId,
                                                                     rootUser.Administrador,
                                                                     rootUser.SecurityStamp);
            return this.Ok(_impersonation.ToDto(tokenModel));
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ImpersonatableUsuarioDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 403)]
        [Route("users/{tenantId:long}")]
        [Authorize]
        public async Task<IActionResult> ImpersonatableUsers(long tenantId)
        {
            // Mismo gate que impersonate: solo root (o un root ya impersonando) puede listar usuarios de
            // otro tenant.
            if (_impersonation.ResolveRealRootUserId() == null)
            {
                return this.StatusCode((int)HttpStatusCode.Forbidden, ApiErrorResponse.From(MessagesAPI.ErrorDataInvalid));
            }

            var users = await _usuarioRepository.GetByTenantIdIgnoringFilterAsync(tenantId);
            var dtos = users
                .Select(u => new ImpersonatableUsuarioDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Email = u.Email,
                })
                .ToList();
            return this.Ok(dtos);
        }
    }
}
