using AutoMapper;
using AcgFotos.Core.Data;
using AcgFotos.Core.Domain;
using AcgFotos.Core.Session;

namespace AcgFotos.Core.Application
{
    public class EntityAppServiceBase<E, D, C> : ExtendedEntityAppServiceBase<E, D, D, D, C>, IEntityAppServiceBase<E, D, C>
    where E : class, IEntityBase, new()
    where D : class, IDto, new()
    where C : class, IListaPaginadaCriteriaBase, new()
    {
        public EntityAppServiceBase(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<E> entityRepository,
            IAppContext appContext,
            IMapper mapper) : base(unitOfWork,
            entityRepository,
            appContext,
            mapper)
        {

        }
    }
}
