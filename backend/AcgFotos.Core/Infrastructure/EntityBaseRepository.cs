using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcgFotos.Core.Application;
using AcgFotos.Core.Domain;
using AcgFotos.Core.ExtensionMethods;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;

namespace AcgFotos.Core.Data
{
    public class EntityBaseRepository<T> : IEntityBaseRepository<T>
           where T : class, IEntityBase, new()
    {

        protected IDbContext DbContext { get; }
        protected IAppContext AppContext { get; }

        #region Constructor

        public EntityBaseRepository(
            IDbContext dbContext,
            IAppContext appContext)
        {
            this.DbContext = dbContext;
            this.AppContext = appContext;
        }
        #endregion

        #region Queries

        public virtual Task<T> GetByIdAsync(long id)
        {
            return this.DbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public virtual async Task<IReadOnlyList<T>> GetAllAsync()
        {
            return await this.DbContext.Set<T>().ToListAsync();
        }

        public virtual async Task<IReadOnlyList<T>> GetByIdsAsync(IReadOnlyList<long> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return new List<T>();
            }
            return await this.DbContext.Set<T>()
                .AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();
        }

        public virtual Task<bool> ExistsAsync(long id)
        {
            return this.DbContext.Set<T>().AnyAsync(x => x.Id == id);
        }

        public virtual Task<PaginationSet<T>> PaginateAsync(IListaPaginadaCriteriaBase criteria)
        {
            return this.BuildPaginationAsync(this.DbContext.Set<T>().AsNoTracking(), criteria);
        }

        #endregion

        #region Pagination helper

        /// <summary>
        /// Aplica orden + paginación + count sobre una query y materializa los items.
        /// El caller (repo concreto) ya aplicó filtros e includes sobre la query.
        /// </summary>
        protected async Task<PaginationSet<TResult>> BuildPaginationAsync<TResult>(
            IQueryable<TResult> query,
            IListaPaginadaCriteriaBase criteria)
        {
            var totalCount = await query.CountAsync();

            if (!string.IsNullOrEmpty(criteria.OrderBy))
            {
                var orderExpression = criteria.OrderBy.ToPascalCase()
                    + (criteria.DescendingOrder ? " DESC" : " ASC");
                query = query.OrderBy(orderExpression);
            }

            if (criteria.PageSize > 0)
            {
                query = query
                    .Skip(criteria.Page * criteria.PageSize)
                    .Take(criteria.PageSize);
            }

            var items = await query.ToListAsync();

            return new PaginationSet<TResult>
            {
                Page = criteria.Page,
                TotalCount = totalCount,
                TotalPages = criteria.PageSize > 0
                    ? (int)Math.Ceiling((decimal)totalCount / criteria.PageSize)
                    : 1,
                Items = items
            };
        }

        #endregion

        public virtual void Add(T entity)
        {
            // Se comentan estas lineas porque esto debería estar contemplado en el AppService
            //var collections = entity.GetCollections<T>();
            //if (collections.Any())
            //{
            //    foreach (var collection in collections)
            //    {
            //        foreach (IEntityBase item in collection)
            //        {
            //            if (item is IMultiTenantEntityBase multiTenantEntityBase)
            //            {
            //                if (multiTenantEntityBase.TenantId == 0)
            //                {
            //                    multiTenantEntityBase.TenantId = this.AppContext.TenantId;
            //                }
            //            }
            //        }
            //    }
            //}

            this.DbContext.Entry(entity);
            this.DbContext.Set<T>().Add(entity);
        }

        public void SetValues(T entity, object source)
        {
            this.DbContext.Entry(entity).CurrentValues.SetValues(source);
        }

        [Obsolete(LegacyApi.EntityBaseRepositoryEditMessage)]
        public virtual void Edit(T currentEntity, T entityToEdit)
        {
            // Se comentan estas lineas porque esto debería estar contemplado en el AppService
            //if (entityToEdit is IMultiTenantEntityBase multiTenantEntityBase)
            //{
            //    multiTenantEntityBase.TenantId = this.AppContext.TenantId;
            //}
            entityToEdit.CopyToOnlyPrimitiveValues(currentEntity);
            var collectionsToEdit = entityToEdit.GetCollections<T>();
            var existingCollections = currentEntity.GetCollections<T>();

            var index = 0;

            foreach (var existingCollection in existingCollections)
            {

                var colToEdit = collectionsToEdit.ToList()[index];
                this.UpdateEntityColllection(colToEdit, existingCollection);
                index++;
            }
        }

        public virtual void Delete(T entity)
        {
            var dbEntityEntry = this.DbContext.Entry(entity);
            dbEntityEntry.State = EntityState.Deleted;
        }

        public void DeleteById(long id)
        {
            var itemDelete = this.DbContext.Set<T>().FirstOrDefault(x => x.Id == id);
            if (itemDelete != null)
            {
                this.Delete(itemDelete);
            }
        }

        public void Detached(T entity)
        {
            var dbEntityEntry = this.DbContext.Entry(entity);
            dbEntityEntry.State = EntityState.Detached;
        }

        protected void UpdateEntityColllection(IEnumerable sourceCollection,
                                                IEnumerable targetCollection)
        {
            this.AddItemToCollection(sourceCollection, targetCollection);
            this.RemoveItemToCollection(sourceCollection, targetCollection);
        }

        private void AddItemToCollection(IEnumerable sourceCollection,
                                          IEnumerable targetCollection)
        {
            if (sourceCollection != null)
            {

                if (targetCollection == null)
                {
                    throw new Exception(MessagesAPI.ErrorCollectionNotInitiated);
                }
                var addMethod = targetCollection
                    .GetType()
                    .GetMethods()
                    .Where(m => m.Name == "Add" &&
                                m.GetParameters().Length == 1)
                    .FirstOrDefault();

                foreach (IEntityBase item in sourceCollection)
                {
                    // Se comentan estas lineas porque esto debería estar contemplado en el AppService
                    //if (item is IMultiTenantEntityBase multiTenantEntityBase)
                    //{
                    //    multiTenantEntityBase.TenantId = this.AppContext.TenantId;
                    //}
                    if (item.Id == 0)
                    {
                        addMethod.Invoke(targetCollection, new object[] { item });
                    }
                    else
                    {
                        var targetItem = GetCollectionItemByID(targetCollection, item.Id);
                        item.CopyToOnlyPrimitiveValues(targetItem);

                        if (targetItem == null)
                        {
                            // agregamos recursivo
                            addMethod.Invoke(targetCollection, new object[] { item });
                        }
                        else
                        {
                            // editamos recursivo
                            var collectionsToEdit = item.GetCollections<T>();
                            var existingCollections = targetItem.GetCollections<T>();

                            var index = 0;

                            foreach (var existingCollection in existingCollections)
                            {
                                if (collectionsToEdit.Count() > 0)
                                {
                                    var colToEdit = collectionsToEdit.ToList()[index];
                                    this.UpdateEntityColllection(colToEdit, existingCollection);
                                    index++;
                                }
                            }
                        }

                    }
                }
            }
        }

        private static IEntityBase GetCollectionItemByID(IEnumerable targetCollection, long id)
        {
            foreach (IEntityBase targetItem in targetCollection)
            {
                if (targetItem.Id == id)
                {
                    return targetItem;
                }
            }
            return null;
        }

        private void RemoveItemToCollection(IEnumerable sourceCollection,
                                            IEnumerable targetCollection)
        {
            var itemsToDelete = new List<IEntityBase>();

            if (targetCollection == null)
            {
                return;
            }
            foreach (IEntityBase target in targetCollection)
            {
                var itemFounded = false;

                if (sourceCollection != null)
                {
                    foreach (IEntityBase source in sourceCollection)
                    {
                        if (source.Id == target.Id)
                        {
                            itemFounded = true;
                        }
                    }
                }
                if (!itemFounded)
                {
                    itemsToDelete.Add(target);
                }
            }
            // Remove un-existing items
            foreach (var itemToDelete in itemsToDelete)
            {

                var collectionsToRemove = itemToDelete.GetCollections<T>();

                foreach (var collectionToRemove in collectionsToRemove)
                {
                        var subItemsToDelete = new List<IEntityBase>();

                        foreach (IEntityBase subItemToDelete in collectionToRemove)
                        {
                            subItemsToDelete.Add(subItemToDelete);
                        }

                        foreach (var subItemToDelete in subItemsToDelete)
                        {
                            this.DbContext.Entry(subItemToDelete).State = EntityState.Deleted;
                        }
                }

                this.DbContext.Entry(itemToDelete).State = EntityState.Deleted;
            }
        }
    }
}
