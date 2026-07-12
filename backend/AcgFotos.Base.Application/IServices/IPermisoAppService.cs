using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Core.TreeView;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface IPermisoAppService : IExtendedEntityAppServiceBase<Permiso,
                                                                        PermisoInputDto,
                                                                        PermisoDto,
                                                                        PermisoHeaderDto,
                                                                        ListaPaginadaCriteriaBase>
    {
        Task<List<HierarchicalItem<long>>> GetHierarchicalItemsAsync();

        /// <summary>
        /// Cambia el flag EsRestringido de un permiso. Requiere root.
        /// Endpoint dedicado por defensa de mass assignment.
        /// </summary>
        Task SetEsRestringidoAsync(long id, bool esRestringido);
    }
}
