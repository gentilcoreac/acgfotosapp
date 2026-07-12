using AcgFotos.Core.Domain;

namespace AcgFotos.Core.Application
{
    public interface IEntityAppServiceBase<E, D, C> : IExtendedEntityAppServiceBase<E, D, D, D, C>
        where E : class, IEntityBase, new()
        where D : class, IDto, new()
        where C : class, IListaPaginadaCriteriaBase, new()
    {

    }
}
