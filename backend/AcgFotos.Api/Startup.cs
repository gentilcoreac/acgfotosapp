using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AcgFotos.Core.AuditLog;
using AcgFotos.Core.AutoFac;
using AcgFotos.Core.AutoMapper;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.ExtensionMethods;
using AcgFotos.Core.Infrastructure;
using AcgFotos.Core.Logs;
using AcgFotos.Core.Security;
using AcgFotos.Base.Infrastructure.Data;
using AcgFotos.Base.Controllers;
using AcgFotos.Api.Helpers;

namespace AcgFotos.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Hace falta para inicializar el contexto antes cuando se accede al authcontroller
            services.AddDbContext<AcgFotosDbContext>(options => DatabaseFactory.ConfigureEFProvider(options, _configuration, "AcgFotos.Base"));

            services.AddHttpContextAccessor();

            IdentityHelper.ConfigureService(services, _configuration);

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                           ForwardedHeaders.XForwardedProto;

                // Sin KnownProxies/KnownNetworks, .NET solo confía en el X-Forwarded-For si el proxy es
                // loopback. Detrás de un reverse-proxy/Cloudflare hay que declarar acá las IPs del proxy,
                // si no RemoteIpAddress queda como la IP del proxy y el rate limit por IP se comparte
                // entre todos los clientes. Configurable por "ForwardedHeaders:KnownProxies" (lista de IPs).
                var knownProxies = _configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>();
                if (knownProxies != null)
                {
                    foreach (var proxy in knownProxies)
                    {
                        if (System.Net.IPAddress.TryParse(proxy, out var ip))
                        {
                            options.KnownProxies.Add(ip);
                        }
                    }
                }
            });

            this.RegisterConfigs(services);

            CorsHelper.ConfigureService(services, _configuration);

            var authIsEnabled = _configuration.GetValue<bool>("AuthenticationEnabled");

            if (authIsEnabled)
            {
                AuthenticationHelper.ConfigureService(services, _configuration);
            }

            SwaggerHelper.ConfigureService(services, _configuration);

            // Rate limiting opcional (RateLimiting:Enabled, default true). Se puede apagar cuando el
            // límite volumétrico/por-IP lo maneje el edge (Cloudflare / Azure Front Door / APIM); el
            // limiter de la app es solo un backstop barato en memoria.
            if (_configuration.GetValue<bool?>("RateLimiting:Enabled") ?? true)
            {
                RateLimitingHelper.ConfigureService(services, _configuration);
            }

            // Purga periódica de refresh tokens expirados/revocados.
            services.AddHostedService<RefreshTokenCleanupService>();

            // Escritor en lote del audit log: drena la AuditLogQueue fuera del request (ADR-0005).
            services.AddHostedService<AuditLogBackgroundWriter>();

            // Self-check de arranque: verifica que los módulos declarados (AppModulesName) cargaron su
            // infraestructura y que el modelo EF tiene entidades (ADR-0009). Hace ruidoso un módulo mal
            // declarado al startup en vez de fallar recién en la primera query.
            services.AddHostedService<ModuleModelVerifier>();

            LogsService.ConfigureService(services, _configuration);

            services.AddLazyCache();

            services.AddDirectoryBrowser();

            // Add framework services.
            services.AddMvc();

            AutoFacRegister.RegisterControllerModules(services, _configuration);
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new AutoMapperModule(AutoFacRegister.GetApplicationModules(_configuration)));
            builder.RegisterModule(new AutofacModuleBase());
        }
        private void RegisterConfigs(IServiceCollection services)
        {
            services.Configure<JwtSecurityTokenConfig>(_configuration.GetSection("JwtSecurityToken"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.ConfigureExceptionHandler();
            }

            // HSTS solo en Production. En dev/qa el browser cachearia el upgrade y romperia
            // localhost / endpoints HTTP de qa si los hubiera.
            if (env.IsProduction())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx => {
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers",
                      "Origin, X-Requested-With, Content-Type, Accept");
                },
            });

            app.UseRouting();

            // CORS ANTES del rate limiter: si el limiter corta con 429 antes de CORS, la respuesta sale
            // sin Access-Control-Allow-Origin y el navegador la reporta como error de CORS (el SPA ni puede
            // leer el 429). Con CORS primero, el 429 lleva los headers y el front puede manejarlo.
            CorsHelper.UseCors(app, _configuration, env);

            // Si RateLimiting:Enabled=false no se registró el limiter (ver ConfigureServices); los
            // atributos [EnableRateLimiting] quedan inertes sin este middleware.
            if (_configuration.GetValue<bool?>("RateLimiting:Enabled") ?? true)
            {
                app.UseRateLimiter();
            }

            var authIsEnabled = _configuration.GetValue<bool>("AuthenticationEnabled");

            if (authIsEnabled)
            {
                app.UseAuthentication();
                app.UseAuthorization();
            }

            SwaggerHelper.UseSwagger(app);

            app.UseAntiXssMiddleware();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}
