using Autofac;
using AcgFotos.Core.AutoFac;
using AcgFotos.Fotos.Application.Imaging;
using AcgFotos.Fotos.Domain.Repositories;
using AcgFotos.Fotos.Infrastructure.Imaging;
using AcgFotos.Fotos.Infrastructure.Persistence.Ef.Repositories;

namespace AcgFotos.Fotos.Controllers
{
    /// <summary>
    /// Composition root del módulo Fotos (mismo patrón que tenía Budget, ADR-0009 D7): registra TODO
    /// lo del módulo. NO se toca <c>AutofacModuleBase</c> ni el <c>AcgFotosDbContext</c> (el contexto
    /// descubre las EF configs por assembly al estar el módulo en <c>AppModulesName</c>).
    /// </summary>
    public class AutofacModuleFotos : AutofacBaseModule
    {
        // Ubica el assembly "AcgFotos.Fotos.Application" para el registro por convención (AppServices, Mappers).
        public override string GetModuleName() => "AcgFotos.Fotos";

        // Vacío: el AcgFotosDbContext ya lo registra la base.
        public override void LoadDbContext(ContainerBuilder builder) { }

        protected override void Load(ContainerBuilder builder)
        {
            // Repos y servicios del módulo se registran EXPLÍCITAMENTE acá a medida que aparecen
            // (la convención por sufijo de AutofacBaseModule busca "<módulo>.Infraestructure" y no
            // matchea "AcgFotos.Fotos.Infrastructure" — mismo caso que tenía Budget).
            builder.RegisterType<ImageSharpImageProcessor>()
                .As<IImageProcessor>()
                .SingleInstance(); // sin estado: una instancia alcanza

            builder.RegisterType<EventoRepository>()
                .As<IEventoRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<CursoRepository>()
                .As<ICursoRepository>()
                .InstancePerLifetimeScope();

            base.Load(builder);
        }
    }
}
