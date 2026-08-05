using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using AcgFotos.Core.AutoFac;
using AcgFotos.Fotos.Application.Familias;
using AcgFotos.Fotos.Application.Imaging;
using AcgFotos.Fotos.Application.Procesamiento;
using AcgFotos.Fotos.Application.Storage;
using AcgFotos.Fotos.Application.Tarjetas;
using AcgFotos.Fotos.Controllers.Background;
using AcgFotos.Fotos.Domain.Repositories;
using AcgFotos.Fotos.Infrastructure.Imaging;
using AcgFotos.Fotos.Infrastructure.Persistence.Ef.Repositories;
using AcgFotos.Fotos.Infrastructure.Storage;
using AcgFotos.Fotos.Infrastructure.Tarjetas;

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

            builder.RegisterType<ValidadorAssetMarcaAgua>()
                .As<IValidadorAssetMarcaAgua>()
                .SingleInstance(); // sin estado propio (sólo lee OpcionesFotos, ya singleton)

            builder.RegisterType<EventoRepository>()
                .As<IEventoRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<GrupoRepository>()
                .As<IGrupoRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<FotoRepository>()
                .As<IFotoRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<PedidoRepository>()
                .As<IPedidoRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<CodigoAccesoRepository>()
                .As<ICodigoAccesoRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<PerfilMarcaAguaRepository>()
                .As<IPerfilMarcaAguaRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<OpcionesPublicacionRepository>()
                .As<IOpcionesPublicacionRepository>()
                .InstancePerLifetimeScope();

            // Cascada de resolución de marca de agua (ADR-15 §4, design.md D3): sin estado propio,
            // pero sus repos son InstancePerLifetimeScope (ChangeTracker de EF), así que se registra igual.
            builder.RegisterType<ConfiguracionMarcaAguaResolver>()
                .As<IConfiguracionMarcaAguaResolver>()
                .InstancePerLifetimeScope();

            builder.RegisterType<FamiliaTokenFactory>()
                .As<IFamiliaTokenFactory>()
                .InstancePerLifetimeScope(); // usa IOptions<JwtSecurityTokenConfig>, no sin estado

            builder.RegisterType<FotoStorage>()
                .As<IFotoStorage>()
                .InstancePerLifetimeScope(); // envuelve al IStorageProvider registrado por la base

            builder.RegisterType<QrCoderGeneradorQr>()
                .As<IGeneradorQr>()
                .SingleInstance(); // sin estado

            // Pipeline de procesamiento en background (ADR-04): cola singleton + worker hosted.
            // El host resuelve IEnumerable<IHostedService> del container, así el vertical suma
            // su worker sin tocar el Startup de la plataforma.
            builder.RegisterType<FotoProcesamientoQueue>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<FotoProcesamientoWorker>()
                .As<IHostedService>()
                .SingleInstance();

            builder.Register(c =>
                {
                    var configuration = c.Resolve<IConfiguration>();
                    var opciones = new OpcionesFotos();
                    configuration.GetSection("Fotos").Bind(opciones);
                    return opciones;
                })
                .SingleInstance();

            base.Load(builder);
        }
    }
}
