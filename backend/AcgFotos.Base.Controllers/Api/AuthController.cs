using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using AcgFotos.Base.Application.Constantes;
using AcgFotos.Base.Application.Identity;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Core.AuditLog;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.ExtensionMethods;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Localization.PublicResources.Fields;
using AcgFotos.Core.Security;
using AcgFotos.Core.Session;


namespace AcgFotos.Base.Controllers.Api
{
    // Este controller NO tiene [ApiController].
    // Los endpoints de auth devuelven errores con la forma estándar de la API (ApiErrorResponse → { message, errors },
    // mensajes localizados de MessagesAPI), NO ValidationProblemDetails automático.
    // Por eso los chequeos manuales de ModelState.IsValid abajo son intencionales.
    //
    // Sobre el enmascaramiento en login:
    //  Cuando user == null se devuelve el mensaje genérico ErrorCredentialsInvalid (no se distingue user inexistente de password mal).
    //  Los otros branches (email no confirmado, lockout, tenant inactivo, password mal con "intentos restantes") sí revelan que el user existe
    //   decisión UX-friendly, mitigada por el rate limiting y el lockout policy activos.

    [Produces("application/json")]
    [Route("api/auth")]
    [SensitiveEndpoint]
    // El rate limiting NO va a nivel de clase: cada endpoint elige su política. Login y password van
    // estrictos (fuerza bruta); refresh/logout/confirmar-cuenta quedan bajo el límite global permisivo.
    public class AuthController : ApiControllerBase
    {
        private readonly IAppContext _appContext;
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ISecurityManagerService _securityManagerService;
        private readonly IJwtSecurityTokenFactory _jwtSecurityTokenFactory;
        private readonly IUsuarioHistorialAppService _usuarioHistorialRepository;
        private readonly ITenantAppService _tenantAppService;
        private readonly ILicensesAppService _licensesAppService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IImpersonationManager _impersonation;
        private readonly IUsuarioRepository _usuarioRepository;

        public AuthController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager,
            IConfiguration configuration,
            IUsuarioHistorialAppService usuarioHistorialRepository,
            ISecurityManagerService securityManagerService,
            IJwtSecurityTokenFactory jwtSecurityTokenFactory,
            IAppContext appContext,
            ITenantAppService tenantAppService,
            ILicensesAppService licensesAppService,
            IRefreshTokenService refreshTokenService,
            IImpersonationManager impersonation,
            IUsuarioRepository usuarioRepository
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _securityManagerService = securityManagerService;
            _jwtSecurityTokenFactory = jwtSecurityTokenFactory;
            _usuarioHistorialRepository = usuarioHistorialRepository;
            _tenantAppService = tenantAppService;
            _licensesAppService = licensesAppService;
            _refreshTokenService = refreshTokenService;
            _impersonation = impersonation;
            _usuarioRepository = usuarioRepository;
            _appContext = appContext;
        }

        #region Métodos Anónimos
        [HttpPost]
        [ProducesResponseType(typeof(TokenModelDto), 200)]
        [Route("token")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingHelper.LoginPolicy)]
        public async Task<IActionResult> CreateToken([FromBody] LoginViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorUserPassRequired));
            }

            var user = await _userManager.FindByNameAsync(model.UserName).ConfigureAwait(false);

            if (user == null)
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorCredentialsInvalid));
            }

            if (!user.EmailConfirmed)
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorEmailNotValid));
            }

            if (await _userManager.IsLockedOutAsync(user).ConfigureAwait(false))
            {
                return this.BadRequest(ApiErrorResponse.From(string.Format(MessagesAPI.ErrorAccountBlocked, user.UserName)));
            }

            if (!user.Administrador && !(await _tenantAppService.IsActiveAsync(user.TenantId)))
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorTenantNotActive));
            }

            if (await _userManager.CheckPasswordAsync(user, model.Password).ConfigureAwait(false))
            {
                await _usuarioHistorialRepository.UpdateUsuarioHistoriaAsync(user.Id, user.TenantId);

                var tokenModel = _jwtSecurityTokenFactory.CreateInstance(user.Id,
                                                                         user.UserName,
                                                                         user.Email,
                                                                         user.TenantId,
                                                                         user.Administrador,
                                                                         user.SecurityStamp);
                await _userManager.ResetAccessFailedCountAsync(user);

                if (!tokenModel.IsRoot)
                {
                    var licenseResult = await _licensesAppService.ValidateUserLicenseAsync(user.UserName, user.TenantId);
                    if (licenseResult == LicenseValidationResult.NoActiveLicense)
                        return this.BadRequest(ApiErrorResponse.From(string.Format(MessagesAPI.ErrorAccountNotActivelicense, user.UserName)));
                    if (licenseResult == LicenseValidationResult.Expired)
                        return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorLicenseExpired));
                }

                // Emite el refresh token en cookie HttpOnly (no va en el body — shape compat).
                var issuedRefresh = await _refreshTokenService.IssueAsync(user.Id, user.TenantId);
                this.SetRefreshCookie(issuedRefresh);

                var tokenDto = new TokenModelDto
                {
                    Token = tokenModel.Token,
                    ValidTo = tokenModel.ValidTo,
                    TenantId = tokenModel.TenantId,
                    HasVerifiedEmail = tokenModel.HasVerifiedEmail
                };
                return this.Ok(tokenDto);
            }

            // Wrong password: Identity incrementa AccessFailedCount y, si el user tiene
            // LockoutEnabled=true y el count alcanza MaxFailedAccessAttempts, marca LockoutEnd
            // automaticamente. Solo reportamos el estado resultante.
            var failResult = await _userManager.AccessFailedAsync(user).ConfigureAwait(false);
            if (!failResult.Succeeded)
            {
                return this.BadRequest(ApiErrorResponse.From(failResult.Errors.Select(x => x.Description).FirstOrDefault()));
            }

            if (await _userManager.IsLockedOutAsync(user).ConfigureAwait(false))
            {
                return this.BadRequest(ApiErrorResponse.From(string.Format(MessagesAPI.ErrorAccountBlocked, user.UserName)));
            }

            var maxAttempts = _configuration.GetValue<int?>("ErroresIngresoPermitidos") ?? 5;
            var remaining = Math.Max(0, maxAttempts - user.AccessFailedCount);
            return this.BadRequest(ApiErrorResponse.From(string.Format(MessagesAPI.ErrorInputPasswordWrong, remaining)));
        }

        [HttpPost]
        [ProducesResponseType(typeof(IdentityResult), 200)]
        [Route("confirmar-cuenta")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmarCuenta([FromBody] ConfirmarCuentaViewModel model)
        {

            if (model.UserId <= 0 || model.Code == null)
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorRequiredInfoGet));
            }

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());

            if (user == null)
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorUserNotFound));
            }

            var userHasPassword = await _userManager.HasPasswordAsync(user);
            if (user.EmailConfirmed && userHasPassword)
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.SuccessAccountConfirm2));
            }

            if (!user.EmailConfirmed && !string.IsNullOrEmpty(model.Code))
            {
                var result = await _userManager.ConfirmEmailAsync(user, model.Code);

                // Si no encuentra el token en memoria de aplicación ni en base de datos retorna BadRequest
                if ((user.TokenExpirationDate < DateTime.Now || user.EmailConfirmationToken != model.Code) && !result.Succeeded)
                {
                    return this.BadRequest(ApiErrorResponse.FromMany(result.Errors.Select(x => x.Description)));
                }
            }

            var passConfirmationResult = await _userManager.AddPasswordAsync(user, model.Password);

            if (passConfirmationResult.Succeeded)
            {
                return this.Ok(string.Format(MessagesAPI.SuccessAccountConfirm, user.UserName));
            }
            else
            {
                return this.BadRequest(ApiErrorResponse.FromMany(passConfirmationResult.Errors.Select(x => x.Description)));
            }
        }

        #endregion

        [HttpPost]
        [ProducesResponseType(typeof(TokenModelDto), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 401)]
        [Route("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh()
        {
            // El refresh token viaja en cookie HttpOnly, no en el body.
            var rawRefresh = this.Request.Cookies[RefreshCookieName];
            if (string.IsNullOrEmpty(rawRefresh))
            {
                return this.Unauthorized(ApiErrorResponse.From(MessagesAPI.ErrorCredentialsInvalid));
            }

            var validation = await _refreshTokenService.ValidateAsync(rawRefresh);
            if (!validation.IsValid)
            {
                this.ClearRefreshCookie();
                return this.Unauthorized(ApiErrorResponse.From(MessagesAPI.ErrorCredentialsInvalid));
            }

            var user = await _userManager.FindByIdAsync(validation.UserId.ToString());
            if (user == null || await _userManager.IsLockedOutAsync(user).ConfigureAwait(false))
            {
                this.ClearRefreshCookie();
                return this.Unauthorized(ApiErrorResponse.From(MessagesAPI.ErrorCredentialsInvalid));
            }

            if (!user.Administrador && !(await _tenantAppService.IsActiveAsync(user.TenantId)))
            {
                this.ClearRefreshCookie();
                return this.Unauthorized(ApiErrorResponse.From(MessagesAPI.ErrorTenantNotActive));
            }

            var tokenModel = _jwtSecurityTokenFactory.CreateInstance(user.Id,
                                                                     user.UserName,
                                                                     user.Email,
                                                                     user.TenantId,
                                                                     user.Administrador,
                                                                     user.SecurityStamp);

            if (!tokenModel.IsRoot)
            {
                var licenseResult = await _licensesAppService.ValidateUserLicenseAsync(user.UserName, user.TenantId);
                if (licenseResult == LicenseValidationResult.NoActiveLicense)
                {
                    this.ClearRefreshCookie();
                    return this.Unauthorized(ApiErrorResponse.From(string.Format(MessagesAPI.ErrorAccountNotActivelicense, user.UserName)));
                }
                if (licenseResult == LicenseValidationResult.Expired)
                {
                    this.ClearRefreshCookie();
                    return this.Unauthorized(ApiErrorResponse.From(MessagesAPI.ErrorLicenseExpired));
                }
            }

            // Rotación: revoca el refresh usado y emite uno nuevo (detección de replay).
            var issuedRefresh = await _refreshTokenService.RotateAsync(rawRefresh, user.Id, user.TenantId);
            this.SetRefreshCookie(issuedRefresh);

            // Si hay una impersonalización en curso (cookie-overlay firmada de este usuario, ADR-0002),
            // re-emitir el token scopeado al usuario destino en vez del de root — así la impersonalización
            // sobrevive a la rotación del refresh. El refresh token sigue siendo el del root (ancla de identidad).
            var impersonation = _impersonation.ReadOverlayTicket(user.Id);
            if (impersonation != null)
            {
                var target = await _usuarioRepository.GetByIdIgnoringTenantFilterAsync(impersonation.UserId);
                if (target != null
                    && target.TenantId == impersonation.TenantId
                    && await _tenantAppService.IsActiveAsync(impersonation.TenantId))
                {
                    var impersonationToken = _jwtSecurityTokenFactory.CreateInstance(target.Id,
                                                                                     target.UserName,
                                                                                     target.Email,
                                                                                     target.TenantId,
                                                                                     target.Administrador,
                                                                                     target.SecurityStamp,
                                                                                     impersonatedBy: impersonation.By);
                    return this.Ok(_impersonation.ToDto(impersonationToken));
                }

                // La impersonalización ya no es válida (usuario borrado/movido o tenant inactivo): se limpia
                // la overlay y se sigue como root.
                _impersonation.ClearOverlayCookie();
            }

            return this.Ok(_impersonation.ToDto(tokenModel));
        }

        [HttpPost]
        [ProducesResponseType(200)]
        [Route("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var rawRefresh = this.Request.Cookies[RefreshCookieName];
            if (!string.IsNullOrEmpty(rawRefresh))
            {
                await _refreshTokenService.RevokeAsync(rawRefresh, RefreshTokenRevokeReasons.Logout);
            }
            this.ClearRefreshCookie();
            _impersonation.ClearOverlayCookie();
            return this.Ok();
        }

        [HttpPost]
        [ProducesResponseType(typeof(IdentityResult), 200)]
        [Route("cambiar-password")]
        [EnableRateLimiting(RateLimitingHelper.PasswordPolicy)]
        public async Task<IActionResult> CambiarPassword([FromBody] ChangePasswordInputModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(ApiErrorResponse.FromMany(this.ModelState.GetErrors()));
            }

            var identityUser = await _signInManager.UserManager.FindByNameAsync(_appContext.UserName);
            if (identityUser == null)
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorUserNotFound + ": " + _appContext.UserName + "."));
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(identityUser,
                                                                              model.CurrentPassword,
                                                                              model.NewPassword);
            if (changePasswordResult.Succeeded)
            {
                // Cambió la password: revocar todos los refresh activos del usuario.
                await _refreshTokenService.RevokeAllForUserAsync(identityUser.Id, RefreshTokenRevokeReasons.PasswordChanged);
                return this.Ok(model);
            }

            return this.BadRequest(ApiErrorResponse.FromMany(changePasswordResult.Errors.Select(x => x.Description)));
        }

        [HttpPost]
        [ProducesResponseType(200)]
        [Route("olvide-password")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingHelper.PasswordPolicy)]
        public async Task<IActionResult> ForgotPasswordUsingEmail([FromBody] ForgotPasswordViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(ApiErrorResponse.FromMany(this.ModelState.Values.Select(x => x.Errors.FirstOrDefault().ErrorMessage)));
            }

            var user = await _userManager.FindByEmailAsync(model.EmailOrUsername).ConfigureAwait(false);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(model.EmailOrUsername).ConfigureAwait(false);
            }

            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false)))
            {
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorEmailNotCorrespondToRegisteredUser));
            }
            var code = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
            var codeEncode = WebUtility.UrlEncode(code);
            await _securityManagerService.SendForgotEmailAsync(Fields.PasswordRecovery,
                                                    model, user, codeEncode);

            return this.Ok();
        }

        [HttpPost]
        [ProducesResponseType(typeof(IdentityResult), 200)]
        [Route("resetear-password")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingHelper.PasswordPolicy)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(ApiErrorResponse.FromMany(this.ModelState.Values.Select(x => x.Errors.FirstOrDefault().ErrorMessage)));
            }

            var user = await _userManager.FindByEmailAsync(model.EmailOrUsername).ConfigureAwait(false);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(model.EmailOrUsername).ConfigureAwait(false);
            }
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return this.BadRequest(ApiErrorResponse.From(MessagesAPI.ErrorDataInvalid));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password).ConfigureAwait(false);

            if (result.Succeeded)
            {
                // Reset de password: revocar todos los refresh activos del usuario.
                await _refreshTokenService.RevokeAllForUserAsync(user.Id, RefreshTokenRevokeReasons.PasswordChanged);
                return this.Ok(string.Format(MessagesAPI.SuccessPasswordReset, user.UserName) );
            }
            return this.BadRequest(result.Errors.Select(x => x.Description));
        }

        #region Refresh token cookie

        private const string RefreshCookieName = "refreshToken";

        // Cookie del refresh token: HttpOnly (JS no la lee → inmune a XSS), Secure (HTTPS;
        // localhost es secure context en Chrome), SameSite=Strict, acotada a /api/auth.
        private void SetRefreshCookie(IssuedRefreshToken issued)
        {
            this.Response.Cookies.Append(RefreshCookieName, issued.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth",
                Expires = issued.ExpiresAt,
            });
        }

        private void ClearRefreshCookie()
        {
            this.Response.Cookies.Delete(RefreshCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth",
            });
        }

        #endregion
    }
}
