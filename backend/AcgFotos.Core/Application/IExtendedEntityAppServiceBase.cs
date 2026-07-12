using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Core.Domain;

namespace AcgFotos.Core.Application
{
    public interface IExtendedEntityAppServiceBase<E, I, O, R, C>
             where E : class, IEntityBase, new()
             where I : class, IDto, new()
             where O : class, IDto, new()
             where R : class, IDto, new()
             where C : class, IListaPaginadaCriteriaBase, new()
    {
        Task<PaginationSet<R>> SearchAsync(C criteria);

        Task<IEnumerable<R>> ExportAsync(C criteria);

        Task<IEnumerable<R>> GetAllAsync();

        Task<O> GetByIdAsync(long id);

        Task<O> UpdateAsync(I dto);

        Task DeleteByIdAsync(long id);
    }
}
