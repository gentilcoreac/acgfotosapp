using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Core.Domain;

namespace AcgFotos.Core.Data
{
    public interface IEntityBaseRepository<T> where T : class, IEntityBase, new()
    {
        /// <summary>Devuelve la entidad por Id o null si no existe.</summary>
        Task<T> GetByIdAsync(long id);

        /// <summary>Devuelve todas las entidades materializadas. Respeta el filtro global de tenant.</summary>
        Task<IReadOnlyList<T>> GetAllAsync();

        /// <summary>
        /// Devuelve las entidades cuyo Id está en el set indicado. Read-only.
        /// Si la lista viene vacía devuelve una lista vacía sin tocar la DB
        /// (EF traduciría WHERE Id IN () a SQL inválido).
        /// </summary>
        Task<IReadOnlyList<T>> GetByIdsAsync(IReadOnlyList<long> ids);

        /// <summary>True si existe una entidad con el Id dado (respeta filtro global de tenant).</summary>
        Task<bool> ExistsAsync(long id);

        /// <summary>
        /// Listado paginado genérico (orden + skip/take + count, sin filtros).
        /// Para filtros específicos sobreescribir o usar métodos nombrados del repo concreto.
        /// </summary>
        Task<PaginationSet<T>> PaginateAsync(IListaPaginadaCriteriaBase criteria);

        // Writes: afectan ChangeTracker en memoria. La I/O ocurre en UnitOfWork.CommitAsync.
        void Add(T entity);

        /// <summary>
        /// Copia los escalares del <paramref name="source"/> sobre la entidad tracked usando
        /// <c>DbContext.Entry(entity).CurrentValues.SetValues</c>. Solo toca propiedades que
        /// matcheen por nombre con columnas mapeadas por EF; no toca navegaciones ni colecciones.
        /// </summary>
        void SetValues(T entity, object source);

        [Obsolete(LegacyApi.EntityBaseRepositoryEditMessage)]
        void Edit(T currentEntity, T newEntity);

        void Delete(T entity);

        void DeleteById(long id);
    }
}
