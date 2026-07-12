using AcgFotos.Core.Application;
using AcgFotos.Fotos.Application.Criterias;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.IServices;

public interface ICursoAppService : IExtendedEntityAppServiceBase<Curso,
                                                                  CursoInputDto,
                                                                  CursoDto,
                                                                  CursoHeaderDto,
                                                                  CursoCriteria>
{
}
