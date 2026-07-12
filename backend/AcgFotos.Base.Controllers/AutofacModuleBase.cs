using Autofac;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using AcgFotos.Core.Security.Repository;
using AcgFotos.Core.AuditLog;
using AcgFotos.Core.AutoFac;
using AcgFotos.Core.Cryptography;
using AcgFotos.Core.Data;
using AcgFotos.Core.Email;
using AcgFotos.Core.Storage;
using AcgFotos.Core.Logs;
using AcgFotos.Core.Security;
using AcgFotos.Core.Session;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Services;
using AcgFotos.Base.Infrastructure.Data;
using AcgFotos.Base.Infrastructure.Repositories;

namespace AcgFotos.Base.Controllers
{
    public class AutofacModuleBase : AutofacBaseModule
    {
        public override string GetModuleName()
        {
            return "AcgFotos.Base";
        }

        public override void LoadDbContext(ContainerBuilder builder)
        {
            builder.RegisterType<AcgFotosDbContext>().
                As<IDbContext>().
                InstancePerLifetimeScope();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<EncryptionService>()
                .As<IEncryptionService>()
                .InstancePerLifetimeScope();

            // Storage provider seleccionado por config "Storage:Provider" (default: FileSystem).
            // Para sumar un provider nuevo (Database, AzureBlob): implementar IStorageProvider,
            // agregar el case acá, y documentar la config. Ver guía en Docs/cleanup-log.md (Fase E).
            builder.Register<IStorageProvider>(c =>
            {
                var config = c.Resolve<IConfiguration>();
                var providerName = config.GetValue<string>("Storage:Provider") ?? "FileSystem";
                return providerName switch
                {
                    // "Database" => new DatabaseBlobStorageProvider(...),  // Fase E.2
                    // "AzureBlob" => new AzureBlobStorageProvider(...),    // Fase E.3
                    _ => new FileSystemStorageProvider(
                            c.Resolve<IWebHostEnvironment>(),
                            c.Resolve<IHttpContextAccessor>())
                };
            }).InstancePerLifetimeScope();

            builder.RegisterType<SecurityManagerService>()
                .As<ISecurityManagerService>()
                .InstancePerLifetimeScope();            

            builder.RegisterType<JwtSecurityTokenFactory>()
                .As<IJwtSecurityTokenFactory>()
                .InstancePerLifetimeScope();

            // Solo se registra como ITenantRepository. IEntityBaseRepository<Tenant> sigue
            // resolviéndose al genérico EntityBaseRepository<Tenant> del AutofacBaseModule
            // (ambas instancias comparten DbContext del scope).
            builder.RegisterType<TenantRepository>()
                .As<ITenantRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<TipoLicenciaRepository>()
                .As<ITipoLicenciaRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<AplicacionRepository>()
                .As<IAplicacionRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<UsuarioRepository>()
                .As<IUsuarioRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<EndpointRepository>()
                .As<IEndpointRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<PermisoRepository>()
                .As<IPermisoRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<GrupoRepository>()
                .As<IGrupoRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RolRepository>()
                .As<IRolRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<MenuRepository>()
                .As<IMenuRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<AuditoriaRepository>()
                .As<IAuditoriaRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<ParametroRepository>()
                .As<IParametroRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<UsuariosActivosMensualRepository>()
                .As<IUsuariosActivosMensualRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<UsuarioTipoLicenciaRepository>()
                .As<IUsuarioTipoLicenciaRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<TenantLicenciaRepository>()
                .As<ITenantLicenciaRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RefreshTokenRepository>()
                .As<IRefreshTokenRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RefreshTokenService>()
                .As<IRefreshTokenService>()
                .InstancePerLifetimeScope();

            // Los repos del módulo Reportes se registran en AutofacModuleReportes (ADR-0009 D7):
            // Base no conoce a los módulos de negocio.

            builder.RegisterType<ImpersonationManager>()
                .As<IImpersonationManager>()
                .InstancePerLifetimeScope();

            base.Load(builder);
                      
        }
    }
}
