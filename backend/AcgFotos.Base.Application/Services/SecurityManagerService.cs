using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using AcgFotos.Core.Data;
using AcgFotos.Core.Email;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.ExtensionMethods;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Localization.PublicResources.Fields;
using AcgFotos.Core.Razor;
using AcgFotos.Core.Session;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Identity;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Services.Templates;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories;

namespace AcgFotos.Base.Application.Services
{
    public class SecurityManagerService : ISecurityManagerService
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ITenantRepository _tenantRepository;
        private readonly IConfiguration _configuration;
        private readonly IAppContext _appContext;
        private readonly IEmailSender _emailSender;
        private readonly IParametroAppService _parametroAppService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SecurityManagerService(
           UserManager<Usuario> userManager,
           IAppContext appContext,
           IEmailSender emailSender,
           IParametroAppService parametroAppService,
           ITenantRepository tenantRepository,
           IConfiguration configuration,
           IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _configuration = configuration;
            _appContext = appContext;
            _emailSender = emailSender;
            _parametroAppService = parametroAppService;
            _tenantRepository = tenantRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task Register(Usuario usuario)
        {
            var createResult = await _userManager.CreateAsync(usuario);
            if (createResult.Succeeded)
            {
                var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(usuario);

                var tokenExpirationTime = _configuration.GetSection("EmailConfirmationToken").GetValue<int>("DurationInMinutes");

                usuario.EmailConfirmationToken = emailConfirmationToken;
                usuario.TokenExpirationDate = DateTime.Now.AddMinutes(tokenExpirationTime);

                await _userManager.UpdateAsync(usuario);

                var codeEncode = WebUtility.UrlEncode(emailConfirmationToken);
                await this.SendConfirmEmailAsync(MessagesAPI.AccountCreation, usuario, codeEncode);
            }
            else
            {
                throw new BusinessValidationException(createResult.Errors.ToList()[0].Description);
            }
        }

        public async Task RegisterManual(Usuario user, string password)
        {
            var createResult = await _userManager.CreateAsync(user, password);
            if (createResult.Succeeded)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _userManager.ConfirmEmailAsync(user, token);
            }
            else
            {
                var errores = new List<ValidationFailure>();
                foreach (var item in createResult.Errors.ToList())
                {
                    var error = new ValidationFailure("", item.Description);
                    errores.Add(error);
                }
                throw new BusinessValidationException(MessagesAPI.ErrorValidation, errores);
            }

        }

        public async Task Delete(long id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorUserNotFound);
            }
            var createResult = await _userManager.DeleteAsync(user);
            if (!createResult.Succeeded)
            {
                throw new BusinessValidationException(createResult.Errors.ToList()[0].Description);
            }
        }

        public async Task ReConfirmarCuenta(long id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) throw new BusinessValidationException(MessagesAPI.ErrorUserNotFound);

            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var tokenExpirationTime = _configuration.GetSection("EmailConfirmationToken").GetValue<int>("DurationInMinutes");

            user.EmailConfirmationToken = emailConfirmationToken;
            user.TokenExpirationDate = DateTime.Now.AddMinutes(tokenExpirationTime);

            await _userManager.UpdateAsync(user);

            var codeEncode = WebUtility.UrlEncode(emailConfirmationToken);
            await this.SendConfirmEmailAsync(MessagesAPI.ReconfirmAccount, user, codeEncode);
        }

        private async Task SendConfirmEmailAsync(string titulo_mail, Usuario user, string codeEncode)
        {
            var loginURL = _configuration.GetValue<string>("ConfirmarCuentaURL");
            var callbackUrl = loginURL + $"?userId={user.Id}&code={codeEncode}&userName={user.UserName}&cliente={_appContext.TenantId}";

            var tenant = await _tenantRepository.GetByIdReadOnlyAsync(_appContext.TenantId);

            var reconfirmaModel = new ReConfirmarCuentaDto(_configuration, _httpContextAccessor, titulo_mail, tenant.Nombre, tenant.ColorPrimarioLight, tenant.LogoHeaderLightUrl)
            {
                Url = callbackUrl,
                ButtonAction = "¡" + Fields.ConfirmAccount + "!",
                BodyMsg = string.Format(MessagesAPI.EmailWelcomeMsg, user.Nombre, user.Email),
            };

            var razorEngineInstalnce = new RazorTemplate()
            {
                Assembly = Assembly.GetExecutingAssembly(),
                Chapter = ".Services.Templates.",
                TemplateName = "Confirmar-cuenta.cshtml",
                Model = reconfirmaModel,
            };

            var asunto = $"{tenant.Nombre} - {titulo_mail}";
            var templateResulted = razorEngineInstalnce.RenderRazorViewToString();
            var parametrosRepository = await _parametroAppService.ObtenerParametrosPorNombresAsync(_emailSender.ObtenerParametros());
            await _emailSender.SendAsync(asunto, templateResulted, user.Email, parametrosRepository);
        }

        public async Task SendForgotEmailAsync(string titulo_mail, ForgotPasswordViewModel model, Usuario identityUser, string codeEncode)
        {
            var loginURL = _configuration.GetValue<string>("RecuperarClaveURL");
            var callbackUrl = loginURL + $"?userId={identityUser.Id}&code={codeEncode}&userName={identityUser.UserName}&cliente={_appContext.TenantId}";

            var tenant = await _tenantRepository.GetByIdReadOnlyAsync(identityUser.TenantId);

            var recuperarModel = new RecuperarClaveDto(_configuration, _httpContextAccessor, titulo_mail, tenant.Nombre, tenant.ColorPrimarioLight, tenant.LogoHeaderLightUrl)
            {
                Url = callbackUrl,
                ButtonAction = Fields.ResetPassword,
                BodyMsg = string.Format(MessagesAPI.EmailResetPasswordMsg, identityUser.Nombre, identityUser.Email),
            };

            var razorEngineInstalnce = new RazorTemplate()
            {
                Assembly = Assembly.GetExecutingAssembly(),
                Chapter = ".Services.Templates.",
                TemplateName = "Recuperar-clave.cshtml",
                Model = recuperarModel,
            };

            var asunto = $"{tenant.Nombre} - {titulo_mail}";
            var templateResulted = razorEngineInstalnce.RenderRazorViewToString();
            var parametrosRepository = await _parametroAppService.ObtenerParametrosPorNombresAsync(_emailSender.ObtenerParametros());
            await _emailSender.SendAsync(asunto, templateResulted, identityUser.Email, parametrosRepository);
        }

        //public async Task<bool> UserEmailConfirmed(string userName)
        //{
        //    var user = await _userManager.FindByNameAsync(userName);
        //    if (user == null)
        //    {
        //        throw new BusinessValidationException("No se pudo determinar que la cuenta este confirmada.");
        //    }
        //    return await _userManager.IsEmailConfirmedAsync(user);
        //}

        public async Task<bool> StateUser(string userName, bool state)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorUserNotFound);
            }

            // Bloqueo (ADR-0006): state=true bloquea (LockoutEnd al infinito), state=false desbloquea (LockoutEnd=null). 
            await _userManager.SetLockoutEndDateAsync(user, state ? System.DateTimeOffset.MaxValue : (System.DateTimeOffset?)null);
            return await _userManager.IsLockedOutAsync(user);
        }

        //public async Task EnviarCodigoViaMail(long codigo, string email, string userName)
        //{

        //    //var emailSection = this.configuration.GetSection("Email");
        //    //if (emailSection.GetValue<bool>("EmailEnabled"))
        //    //{
        //    //    var emailSender = new EmailSender(configuration);
        //    //    emailSender.Send(
        //    //        "Movizen - Codigo de verificación",
        //    //        $"Hola '{userName}' - '{email}'. Su código de verificación para el celular es: {codigo:D6}",
        //    //        email
        //    //    );
        //    //}
        //}


    }
}
