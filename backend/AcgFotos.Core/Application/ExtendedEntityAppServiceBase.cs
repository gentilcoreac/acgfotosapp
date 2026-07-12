using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;
using AutoMapper;
using FluentValidation;
using AcgFotos.Core.Data;
using AcgFotos.Core.Domain;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;

namespace AcgFotos.Core.Application
{
    public class ExtendedEntityAppServiceBase<E, I, O, R, C> : IExtendedEntityAppServiceBase<E, I, O, R, C>
        where E : class, IEntityBase, new()
        where I : class, IDto, new()
        where O : class, IDto, new()
        where R : class, IDto, new()
        where C : class, IListaPaginadaCriteriaBase, new()
    {
        #region Variables & Properties

        protected IAppContext AppContext { get; }
        protected IUnitOfWork UnitOfWork { get; }
        protected IEntityBaseRepository<E> EntityRepository { get; }
        protected IMapper Mapper { get; }

        protected Domain.IValidator<E> UpdateValidator { get; private set; }
        protected Domain.IValidator<E> DeleteValidator { get; private set; }

        #endregion

        public ExtendedEntityAppServiceBase(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<E> entityRepository,
            IAppContext appContext,
            IMapper mapper)
        {
            this.UnitOfWork = unitOfWork;
            this.EntityRepository = entityRepository;
            this.AppContext = appContext;
            this.Mapper = mapper;
        }

        public virtual async Task<IEnumerable<R>> ExportAsync(C criteria)
        {
            var entities = await this.EntityRepository.GetAllAsync();
            return this.Mapper.Map<IEnumerable<E>, IEnumerable<R>>(entities);
        }

        public virtual async Task<PaginationSet<R>> SearchAsync(C criteria)
        {
            var page = await this.EntityRepository.PaginateAsync(criteria);
            var dtos = this.Mapper.Map<IEnumerable<E>, IEnumerable<R>>(page.Items).ToList();
            return new PaginationSet<R>
            {
                Page = page.Page,
                TotalCount = page.TotalCount,
                TotalPages = page.TotalPages,
                Items = dtos
            };
        }

        public virtual async Task<IEnumerable<R>> GetAllAsync()
        {
            var entities = await this.EntityRepository.GetAllAsync();
            return this.Mapper.Map<IEnumerable<E>, IEnumerable<R>>(entities);
        }

        /// <summary>
        /// Devuelve la entidad para edición. Sobreescribir si se necesitan Includes específicos.
        /// </summary>
        protected virtual Task<E> GetEntityToUpdateAsync(long id)
        {
            return this.EntityRepository.GetByIdAsync(id);
        }

        public virtual async Task<O> GetByIdAsync(long id)
        {
            var entity = await this.EntityRepository.GetByIdAsync(id);
            return this.ToOutput(entity);
        }

        /// <summary>
        /// Convierte la entidad a su DTO de salida. Default usa AutoMapper (compatibilidad con
        /// servicios no migrados); override para usar Mapperly u otro mapper por servicio.
        /// </summary>
        protected virtual O ToOutput(E entity) => this.Mapper.Map<E, O>(entity);

        public virtual async Task DeleteByIdAsync(long id)
        {
            using (var scope = TransactionScopeFactory.CreateReadCommitted())
            {
                this.BeforeDelete(id);

                this.EntityRepository.DeleteById(id);

                await this.UnitOfWork.CommitAsync();

                this.AfterDelete(id);

                scope.Complete();
            }
        }

        /// <summary>
        /// Alta y edición sobre la entidad tracked. Camino único: load-or-new → SetValues
        /// (escalares, nativo de EF) → SyncCollections (hook por entidad, no-op default) → commit.
        /// Para flujos no estándar, overridear este método entero.
        /// </summary>
        public virtual async Task<O> UpdateAsync(I dto)
        {
            this.CheckInputValidations(dto);

            O outputDto;
            using (var scope = TransactionScopeFactory.CreateReadCommitted())
            {
                var isNew = (dto.Id == 0);
                var entity = isNew ? new E() : await this.GetEntityToUpdateAsync(dto.Id);

                if (isNew)
                {
                    this.ExecuteAdd(entity);
                }
                else if (entity == null)
                {
                    throw new BusinessValidationException(MessagesAPI.ErrorEntityNotFoundForUpdate);
                }

                this.EntityRepository.SetValues(entity, dto);
                this.SyncCollections(entity, dto);

                this.CheckBusinessValidations(entity);
                this.BeforeUpdate(entity, dto, isNew);

                await this.UnitOfWork.CommitAsync();

                outputDto = this.ToOutput(entity);
                dto.Id = outputDto.Id;
                this.AfterUpdate(entity, dto, isNew);

                scope.Complete();
            }

            return outputDto;
        }

        // protected virtual void SetupContext(E newEntity)
        // {
            // NOTA: acá vivía SetupContext/SetAppContextTenant (estampado de tenant por AppService). El
            // estampado + guard multi-tenant se centralizó en AcgFotosDbContext.SaveChanges (ADR-0004 §Enmiendas); 
            // para setup por entidad antes de persistir está BeforeUpdate(entity, dto, isNew).
        // }

        #region Hooks

        protected virtual void BeforeUpdate(E entity, I dto, bool isNew)
        {
        }

        protected virtual void AfterUpdate(E entity, I dto, bool isNew)
        {
        }

        /// <summary>
        /// Reconcilia las colecciones hijas contra el input. No-op default. Override por entidad
        /// con colecciones, típicamente vía <see cref="ChildCollectionSync.SyncBy{TChild,TKey}"/>
        /// matcheando por clave de negocio.
        /// </summary>
        protected virtual void SyncCollections(E entity, I dto)
        {
        }

        protected virtual void BeforeDelete(long entityId)
        {
        }

        protected virtual void AfterDelete(long entityId)
        {
        }

        protected virtual void ExecuteAdd(E newEntity)
        {
            this.EntityRepository.Add(newEntity);
        }

        [Obsolete(LegacyApi.ExecuteEditMessage)]
        protected virtual void ExecuteEdit(E currenEntity, E newEntity)
        {
#pragma warning disable CS0618 // legacy chain — la deprecación se propaga al caller para forzar la migración
            this.EntityRepository.Edit(currenEntity, newEntity);
#pragma warning restore CS0618
        }

        #endregion

        #region Validations

        protected virtual void CheckInputValidations<G>(G dto)
        {
            // Buscamos la clase validator del DTO y ejecutamos validación. Si no existe, no pasa nada.
            var entityNamespaceName = typeof(G).Namespace;
            var applicationNameAssembly = entityNamespaceName.Replace(".Dtos", "").Replace(".Inputs", "");
            var applicationAssembly = Assembly.Load(applicationNameAssembly);

            if (applicationAssembly != null)
            {
                var fullName = applicationAssembly.FullName;
                var items = fullName.Split(',');

                if (items.Length > 0)
                {
                    var assemblyName = items[0] + ".InputValidators." + typeof(G).Name + "Validator";
                    var className = applicationAssembly.FullName;
                    var type = Type.GetType(assemblyName + ", " + className);
                    if (type != null)
                    {
                        var instance = Activator.CreateInstance(type);

                        if (instance is IValidator validator)
                        {
                            var context = new ValidationContext<object>(dto);
                            var result = validator.Validate(context);
                            if (!result.IsValid)
                            {
                                throw new BusinessValidationException(MessagesAPI.ErrorDataEntryValues,
                                                                        result.Errors);
                            }
                        }
                    }
                }
            }
        }

        protected virtual void CheckBusinessValidations(E entity)
        {
            if (this.UpdateValidator != null)
            {
                var result = this.UpdateValidator.Validate(entity);

                if (!result.IsValid)
                {
                    throw new BusinessValidationException(MessagesAPI.ErrorBussinesRules, result.Errors);
                }
            }
        }

        public void AddUpdateValidator(Domain.IValidator<E> validator)
        {
            this.UpdateValidator = validator;
        }

        public void AddDeleteValidator(Domain.IValidator<E> validator)
        {
            this.DeleteValidator = validator;
        }

        #endregion
    }
}
