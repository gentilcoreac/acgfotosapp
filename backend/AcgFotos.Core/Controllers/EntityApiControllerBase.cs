using AcgFotos.Core.Application;
using AcgFotos.Core.Domain;

namespace AcgFotos.Core.Controllers
{
    public class EntityApiControllerBase<E, D, C, S> : ExtendedEntityApiControllerBase<E, D, D, D, C, S>
          where E : class, IEntityBase, new()
          where D : class, IDto, new()
          where C : class, IListaPaginadaCriteriaBase, new()
          where S : class, IEntityAppServiceBase<E, D, C>
    {
        public EntityApiControllerBase(S appService) : base(appService)
        {

        }
    }
}
