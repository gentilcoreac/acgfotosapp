using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace AcgFotos.Core.Data {
    public interface IDbContext {

        EntityEntry Entry([NotNullAttribute] object entity);

        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        void Commit();

        Task CommitAsync();
    }
}
