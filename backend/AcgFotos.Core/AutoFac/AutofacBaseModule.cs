using System.Reflection;
using Autofac;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AcgFotos.Core.Security.Repository;
using AcgFotos.Core.AuditLog;
using AcgFotos.Core.Data;
using AcgFotos.Core.Email;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization;
using AcgFotos.Core.Logs;
using AcgFotos.Core.Security;
using AcgFotos.Core.Session;
using AcgFotos.Core.Utils;

namespace AcgFotos.Core.AutoFac
{
    public abstract class AutofacBaseModule : Autofac.Module
    {
        public abstract string GetModuleName();

        public abstract void LoadDbContext(ContainerBuilder builder);

        protected override void Load(ContainerBuilder builder)
        {

            var currentAssem = this.GetType().Namespace;


            builder.RegisterType<UnitOfWork>().
                As<IUnitOfWork>().
                InstancePerLifetimeScope();

            this.LoadDbContext(builder);

            builder.RegisterType<TimeManager>().
                As<ITimeManager>().
                InstancePerLifetimeScope();

            builder.RegisterType<LocalizationResourcesManager>().
                As<ILocalizationResourcesManager>().
                InstancePerLifetimeScope();

            builder.Register(c => new AppContextFilterAttribute(
                    c.Resolve<IAppContext>())).InstancePerLifetimeScope();

            builder.Register(c => new AuditLogFilterAttribute(
                    c.Resolve<IAppContext>(),
                    c.Resolve<IAuditLogQueue>(),
                    c.Resolve<ITimeManager>())).InstancePerLifetimeScope();

            builder.Register(c => new CustomFilterExceptionAttribute(
                    c.Resolve<ILoggerFactory>(),
                    c.Resolve<IEnrichLogger>())).InstancePerLifetimeScope();

            builder.Register(c => new LoggerActionFilterAttribute(
                    c.Resolve<ILoggerFactory>(),
                    c.Resolve<IEnrichLogger>())).InstancePerLifetimeScope();

            builder.Register(c => new EndpointAuthoritation(
                    c.Resolve<IConfiguration>(),
                    c.Resolve<IAppCache>(),
                    c.Resolve<ISecurityRepository>(),
                    c.Resolve<IAuthzVersion>(),
                    c.Resolve<IAuditLogQueue>(),
                    c.Resolve<ITimeManager>())).InstancePerLifetimeScope();

            builder.RegisterGeneric(typeof(EntityBaseRepository<>))
                    .As(typeof(IEntityBaseRepository<>))
                    .InstancePerDependency();

            builder.RegisterType<EmailSender>()
                .As<IEmailSender>()
                .InstancePerLifetimeScope();

            builder.RegisterType<SecurityRepository>()
                .As<ISecurityRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<AuthzVersionReader>()
                .As<IAuthzVersion>()
                .InstancePerLifetimeScope();

            builder.RegisterType<AuditLogRepository>()
                .As<IAuditLogRepository>()
                .InstancePerLifetimeScope();

            // Singleton: una sola cola compartida entre todos los requests (productores) y el
            // AuditLogBackgroundWriter (consumidor único, accede al Reader vía el tipo concreto).
            builder.RegisterType<AuditLogQueue>()
                .AsSelf()
                .As<IAuditLogQueue>()
                .SingleInstance();

            builder.RegisterType<AppContext>()
                .As<IAppContext>()
                .InstancePerLifetimeScope();

            builder.Register(c => new ExceptionHandlingMiddleware(
                    c.Resolve<RequestDelegate>(),
                    c.Resolve<ILoggerFactory>(),
                    c.Resolve<IAuditLogQueue>())).InstancePerLifetimeScope();


            var domainAssembly = Assembly.GetEntryAssembly();
            var infraestructureAssembly = Assembly.GetEntryAssembly();


            // Intenta cargar y registrar los proyectos por reflection. De no ser posible, el hilo de ejecución no se detiene y el error
            // capturado por el catch es interceptado por el logger.
            try
            {
                domainAssembly = Assembly.Load(this.GetModuleName() + ".Domain");

                builder.RegisterAssemblyTypes(new Assembly[] { domainAssembly })
                   .Where(t => t.Name.EndsWith("Repository"))
                   .AsImplementedInterfaces();

                builder.RegisterAssemblyTypes(new Assembly[] { domainAssembly })
                       .Where(t => t.Name.EndsWith("Validator"))
                       .AsImplementedInterfaces();
         
            } catch {
            }

            try
            {
                infraestructureAssembly = Assembly.Load(this.GetModuleName() + ".Infraestructure");
                builder.RegisterAssemblyTypes(new Assembly[] { infraestructureAssembly })
                   .Where(t => t.Name.EndsWith("Repository"))
                   .AsImplementedInterfaces();
            } catch { }

            var applicationAssembly = Assembly.Load(this.GetModuleName() + ".Application");

            builder.RegisterAssemblyTypes(applicationAssembly)
                   .Where(t => t.Name.EndsWith("AppService"))
                   .AsImplementedInterfaces();

            // Mappers (Mapperly partial classes) registrados por convención por sufijo "Mapper".
            // AsSelf porque no implementan interfaz (Mapperly genera métodos partial concretos).
            builder.RegisterAssemblyTypes(applicationAssembly)
                   .Where(t => t.Name.EndsWith("Mapper"))
                   .AsSelf()
                   .InstancePerLifetimeScope();
        }
    }
}
