using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;
using AcgFotos.Core.TreeView;

namespace AcgFotos.Base.Application.Services
{
    public class EndpointAppService : EntityAppServiceBase<Endpoint,
                                                           EndpointDto,
                                                           ListaPaginadaCriteriaBase>, IEndpointAppService
    {
        private readonly IEndpointRepository _endpointRepository;

        public EndpointAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<Endpoint> entityRepository,
            IEndpointRepository endpointRepository,
            IAppContext appContext,
            IMapper mapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _endpointRepository = endpointRepository;
        }

        public override async Task<PaginationSet<EndpointDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
        {
            var page = await _endpointRepository.SearchByMatchAsync(criteria);
            return page.MapItems(this.Mapper.Map<Endpoint, EndpointDto>);
        }

        public async Task<List<HierarchicalItem<long>>> GetHierarchicalItemsAsync()
        {
            var targetList = new List<HierarchicalItem<long>>();
            var itemsResult = await this.EntityRepository.GetAllAsync();

            var groupedByModule = itemsResult.GroupBy(e => e.ModuleName);
            foreach (var moduleGroup in groupedByModule)
            {
                var groupedByController = moduleGroup
                    .GroupBy(e => new { e.ControllerName, e.ModuleName })
                    .Where(x => x.Key.ModuleName == moduleGroup.Key)
                    .ToList();
                var moduleItem = new HierarchicalItem<long>
                {
                    Id = 0,
                    Name = moduleGroup.Key,
                    IconClass = string.Empty,
                    Info = string.Empty,
                    IsExpanded = true,
                    IsSelected = false,
                    ExternalLink = false,
                    Children = new List<HierarchicalItem<long>>()
                };
                foreach (var controllerGroup in groupedByController)
                {
                    var childrens = new List<HierarchicalItem<long>>();
                    foreach (var child in controllerGroup)
                    {
                        var item = new HierarchicalItem<long>
                        {
                            Id = child.Id,
                            Name = child.HttpMethod + " - " + child.Route,
                            IconClass = string.Empty,
                            Info = string.Empty,
                            IsExpanded = true,
                            IsSelected = false,
                            ExternalLink = false,
                            Children = new List<HierarchicalItem<long>>()
                        };
                        childrens.Add(item);
                    }

                    var controllerItem = new HierarchicalItem<long>
                    {
                        Id = 0,
                        Name = controllerGroup.Key.ControllerName,
                        IconClass = string.Empty,
                        Info = string.Empty,
                        IsExpanded = true,
                        IsSelected = false,
                        ExternalLink = false,
                        Children = childrens
                    };
                    moduleItem.Children.Add(controllerItem);
                }
                targetList.Add(moduleItem);
            }

            return targetList;
        }

    }
}
