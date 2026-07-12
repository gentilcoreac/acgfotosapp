using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using AcgFotos.Core.Session;
using Microsoft.Extensions.Logging;
using AcgFotos.Core.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using AcgFotos.Core.Domain;
using System.Linq.Expressions;
using System.Linq;

namespace AcgFotos.Core.Data
{
    public abstract class CustomDbContext : DbContext
    {
        private readonly IAppContext _appContext;

        private readonly ILogger _logger;
        public CustomDbContext(IAppContext appContext, ILogger logger)
        {
            _logger = logger;
            _appContext = appContext;
        }

        public virtual void Commit()
        {
            base.SaveChanges();
        }

        public virtual Task CommitAsync()
        {
            return base.SaveChangesAsync();
        }
        protected abstract void SetupDatabaseProvider(DbContextOptionsBuilder optionsBuilder, string connectionString);

        protected abstract void SetupDbConfiguration(ModelBuilder modelBuilder);

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json")
                   .Build();
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                this.SetupDatabaseProvider(optionsBuilder, connectionString);
                base.OnConfiguring(optionsBuilder);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            this.SetupDbConfiguration(modelBuilder);
            // Intentamos mover el filter para la configuración dinamina del tenant a otra clase pero deja de andar.
            // Se intento con un método de extensión, con un método estático e instanciando una clase, pero siempre deja de andar.
            this.SetupMultitenantFilter(modelBuilder);
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
    }
}
