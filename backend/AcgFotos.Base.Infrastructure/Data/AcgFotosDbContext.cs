using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Configurations;
using AcgFotos.Core.Domain;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Infrastructure;
using AcgFotos.Core.Localization.APIResources;

namespace AcgFotos.Base.Infrastructure.Data
{
    public class AcgFotosDbContext : IdentityDbContext<Usuario,
                                                      IdentityRole<long>, long>, IDbContext
    {
        private readonly IAppContext _appContext;
        private readonly ILogger<AcgFotosDbContext> _logger;

        public AcgFotosDbContext(

           ) : base()
        {

        }


        public AcgFotosDbContext(
            DbContextOptions<AcgFotosDbContext> options,
            IAppContext appContext,
            ILogger<AcgFotosDbContext> logger) : base(options)
        {
            _appContext = appContext;
            _logger = logger;
        }

        #region Entities
        public DbSet<Aplicacion> Aplicaciones { get; set; }
        public DbSet<Auditoria> AuditLogs { get; set; }
        public DbSet<AuthzVersion> AuthzVersions { get; set; }
        public DbSet<Endpoint> Endpoints { get; set; }
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<GrupoRol> GrupoRoles { get; set; }
        public DbSet<LogInfo> LogInfos { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Parametro> Parametros { get; set; }
        public DbSet<ParametroValorTenant> ParametrosValoresTenants { get; set; }
        public DbSet<Permiso> Permisos { get; set; }
        public DbSet<PermisoEndpoint> PermisoEndpoints { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        #pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        public DbSet<Rol> Roles { get; set; }
        public DbSet<RolPermiso> RolPermisos { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<TenantAplicacion> TenantAplicaciones { get; set; }
        public DbSet<TenantFile> TenantFiles { get; set; }
        public DbSet<TenantLicencia> TenantLicencias { get; set; }
        public DbSet<TipoLicencia> TipoLicencias { get; set; }
        public DbSet<TipoLicenciaRoles> TipoLicenciaRoles { get; set; }
        public DbSet<UsuarioHistorial> UsuariosHistorial { get; set; }
        public DbSet<UsuarioGrupo> UsuarioGrupos { get; set; }
        public DbSet<UsuarioRol> UsuarioRoles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<UsuarioAplicacion> UsuariosAplicaciones { get; set; }
        public DbSet<UsuarioTipoLicencia> UsuariosTipoLicencias { get; set; }
        public DbSet<UsuariosActivosMensual> UsuariosActivosMensuales { get; set; }
        // Las entidades del módulo Reportes (pbi_*) viven en AcgFotos.Reportes.* y se registran en el
        // modelo por assembly-scan (ADR-0009 D2). El contexto único no las declara como DbSet ni las
        // conoce en compile-time; los repos las acceden vía Set<T>().
        #endregion

        // Configuración usada en design-time (migraciones) y para leer AppModulesName en
        // OnModelCreating. Mismo patrón base-path + appsettings.json que ya usaba OnConfiguring.
        private static IConfiguration BuildConfiguration() =>
            new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json")
               .Build();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ADR-0009 (D2): contexto único pero MÓDULO-AGNÓSTICO. En vez de listar a mano las EF
            // configs de cada módulo, se descubren por assembly las IEntityTypeConfiguration de cada
            // "<módulo>.Infrastructure" declarado en AppModulesName. Un módulo nuevo NO edita este
            // método: aporta sus configs en su propio assembly. (Reflection de arranque: corre 1 vez
            // al construir el modelo, que EF cachea → 0 costo por request.)
            foreach (var moduleInfraAssembly in ModuleAssemblies.Infrastructure(BuildConfiguration()))
            {
                modelBuilder.ApplyConfigurationsFromAssembly(moduleInfraAssembly);
            }

            // Identity: su config es un método estático (no IEntityTypeConfiguration) → explícita.
            IdentityRoleEDConfig.Configure(modelBuilder);

            // EFCore.NamingConventions (DatabaseFactory.ConfigureEFProvider) no pisa nombres de
            // tabla/vista fijados EXPLÍCITO por ToTable/ToView (los EFConfig del código base y las de
            // Identity los fijan en PascalCase, pensados para SQL Server) — la config explícita
            // siempre le gana a una convention de model-building. Postgres pliega a minúscula
            // cualquier identificador SIN comillas, así que sin normalizar esto a mano el nombre real
            // de la tabla ("fot_Eventos") no matchearía el SQL crudo heredado, que las referencia sin
            // comillas (TestSeed.sql, seeds de dev, queries de tests). Se corre una vez, tras registrar
            // TODAS las entidades (incluidas las de Identity).
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (tableName != null)
                {
                    entityType.SetTableName(tableName.ToLowerInvariant());
                }

                var viewName = entityType.GetViewName();
                if (viewName != null)
                {
                    entityType.SetViewName(viewName.ToLowerInvariant());
                }
            }

            // IMPORTANTE: el filtro multi-tenant se aplica DESPUÉS de registrar TODAS las entidades,
            // porque recorre el modelo ya poblado (una entity no registrada aún no recibiría el filtro).
            this.SetupMultitenantFilter(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var configuration = BuildConfiguration();

                DatabaseFactory.ConfigureEFProvider(optionsBuilder, configuration, "AcgFotos.Base");

                base.OnConfiguring(optionsBuilder);
            }

            // Loguea SQL al stdout solo en Development. Útil para debugging de queries
            // (ej. verificar que correlated subqueries de EF Core se traducen como subqueries
            // en SELECT y no como N+1).
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                optionsBuilder.LogTo(Console.WriteLine, new[] { Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuted });
            }
        }

        private void SetupMultitenantFilter(ModelBuilder modelBuilder)
        {
            Expression<Func<IMultiTenantEntityBase, bool>> filterExprInterface = c => c.TenantId == _appContext.TenantId || _appContext.IsAnonymous;
            foreach (var mutableEntityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IMultiTenantEntityBase).IsAssignableFrom(mutableEntityType.ClrType))
                {
                    // modify expression to handle correct child type
                    var parameter = Expression.Parameter(mutableEntityType.ClrType);
                    var body = ReplacingExpressionVisitor.Replace(filterExprInterface.Parameters.First(), parameter, filterExprInterface.Body);
                    var lambdaExpression = Expression.Lambda(body, parameter);

                    // set filter
                    mutableEntityType.SetQueryFilter(lambdaExpression);
                }
            }
        }

        public virtual void Commit()
        {
            try
            {
                // Pasa por el override SaveChanges() (no base.SaveChanges()) para que el bump de la
                // versión de autorización viaje en la MISMA transacción que el cambio que lo dispara.
                SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        public virtual async Task CommitAsync()
        {
            try
            {
                await SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        #region Versión de autorización (ADR-0003 §6.3)

        /// <summary>
        /// Entidades cuyo cambio puede alterar los permisos/menús EFECTIVOS de algún usuario. Guardar
        /// cualquiera de ellas incrementa <c>gen_AuthzVersion</c> → invalida el caché de endpoints de
        /// TODOS los afectados sin enumerarlos (la clave del caché lleva la versión).
        ///
        /// Es un superset de la lista del ADR-0003 §6.3: suma <see cref="UsuarioTipoLicencia"/> y
        /// <see cref="TipoLicenciaRoles"/> porque la licencia pasó a ser TOPE DURO de los roles efectivos
        /// (§10.3, posterior al ADR) — un cambio de licencia/roles-de-licencia cambia qué endpoints ve el
        /// usuario. <see cref="Endpoint"/> también está por completitud (su alta/baja se hace por SQL crudo
        /// al arrancar, fuera de este SaveChanges, pero un reinicio ya limpia el caché en RAM).
        /// </summary>
        private static readonly HashSet<Type> s_authzEntityTypes = new()
        {
            typeof(Rol), typeof(RolPermiso), typeof(Permiso), typeof(PermisoEndpoint), typeof(Endpoint),
            typeof(Menu), typeof(Grupo), typeof(GrupoRol), typeof(UsuarioGrupo), typeof(UsuarioRol),
            typeof(UsuarioTipoLicencia), typeof(TipoLicenciaRoles)
        };

        // Se sobreescriben SÓLO las sobrecargas con `acceptAllChangesOnSuccess` (el embudo canónico de EF):
        // SaveChanges()/SaveChangesAsync(ct) de la base delegan en éstas, así que el bump corre UNA sola vez.
        // (Si además sobreescribiéramos las sin-bool, el bump se contaría doble.)
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            StampAndGuardMultiTenant();
            BumpAuthzVersionIfNeeded();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            StampAndGuardMultiTenant();
            await BumpAuthzVersionIfNeededAsync(cancellationToken);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        #region Multi-tenant: estampado + guard centralizados (ADR-0004)

        /// <summary>
        /// Estampa <c>TenantId</c> y aplica el guard multi-tenant UNA sola vez, para TODO write (genérico o
        /// bespoke), sobre el <c>ChangeTracker</c> — no reflexión, no lógica repetida en cada AppService.
        /// <para><b>"Estampar-si-0" (couple-to-stamp):</b> solo se defaultea/valida cuando la entidad nueva NO
        /// trae tenant (<c>TenantId==0</c>). Los flujos que setean <c>TenantId</c> EXPLÍCITO (provisión
        /// cross-tenant de <c>BudTenantDatabaseAppService</c>, alta de <c>Usuario</c>, <c>UsuarioHistorial</c> en
        /// login) quedan exentos automáticamente — sin flags de opt-out.</para>
        /// <para><b>Guard:</b> si hay algo para defaultear y el actor no puede guardar multi-tenant
        /// (<see cref="IAppContext.AllowSaveMultiTenantEntities"/> — root sin elegir tenant), corta. Contextos
        /// anónimos (login/seed) que gestionan su propio tenant explícito no se tocan.</para>
        /// <para><b>Invariantes de integridad (defensa en profundidad, no dependen de qué exponga cada DTO):</b>
        /// (1) el <c>TenantId</c> de una entidad EXISTENTE es INMUTABLE — si un update lo cambia (p.ej. un
        /// <c>SetValues(dto)</c> con un DTO que expone <c>TenantId</c>), se corta con excepción, no se repara en
        /// silencio; (2) un alta con <c>TenantId</c> explícito DISTINTO al del contexto solo la pueden hacer root
        /// (provisión/impersonación) o contextos anónimos (login/seed) — para cualquier otro actor es un write
        /// cross-tenant y se corta.</para>
        /// </summary>
        private void StampAndGuardMultiTenant()
        {
            List<IMultiTenantEntityBase> toStamp = null;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is not IMultiTenantEntityBase mt) continue;

                if (entry.State == EntityState.Added)
                {
                    if (mt.TenantId == 0)
                    {
                        (toStamp ??= new List<IMultiTenantEntityBase>()).Add(mt);
                    }
                    else if (!_appContext.IsAnonymous && !_appContext.IsRoot
                             && mt.TenantId != _appContext.TenantId)
                    {
                        throw new BusinessValidationException(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    // Comparar valores (no solo IsModified): DbSet.Update() sobre una entidad tracked marca
                    // TODAS las propiedades como Modified aunque el valor no haya cambiado (UpdateProfileAsync).
                    var tenantProp = entry.Property(nameof(IMultiTenantEntityBase.TenantId));
                    if (tenantProp.IsModified && !Equals(tenantProp.OriginalValue, tenantProp.CurrentValue))
                    {
                        throw new BusinessValidationException(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
                    }
                }
            }

            if (toStamp == null) return;      // explícito o sin multi-tenant → nada que defaultear/validar
            if (_appContext.IsAnonymous) return; // login/seed manejan su propio TenantId explícito

            if (_appContext.TenantId == 0)
                throw new BusinessValidationException(MessagesAPI.ErrorTenantNotSelected);
            if (!_appContext.AllowSaveMultiTenantEntities())
                throw new BusinessValidationException(MessagesAPI.ErrorTenantNotRoot);

            foreach (var entity in toStamp)
                entity.TenantId = _appContext.TenantId;
        }

        #endregion

        /// <summary>
        /// ¿Hay algún cambio pendiente sobre una entidad de autorización? Usa <c>Metadata.ClrType</c>
        /// (no <c>Entity.GetType()</c>) para ignorar proxies de EF.
        /// </summary>
        private bool HasPendingAuthzChanges() =>
            ChangeTracker.Entries().Any(e =>
                (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                && s_authzEntityTypes.Contains(e.Metadata.ClrType));

        private void BumpAuthzVersionIfNeeded()
        {
            if (!HasPendingAuthzChanges()) return;
            var row = AuthzVersions.FirstOrDefault();
            if (row != null) row.Version += 1; // se persiste en el mismo base.SaveChanges → atómico
        }

        private async Task BumpAuthzVersionIfNeededAsync(CancellationToken cancellationToken)
        {
            if (!HasPendingAuthzChanges()) return;
            var row = await AuthzVersions.FirstOrDefaultAsync(cancellationToken);
            if (row != null) row.Version += 1;
        }

        #endregion
    }
}
