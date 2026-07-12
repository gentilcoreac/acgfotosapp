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
    /// <summary>Tarjetas imprimibles del curso (una por alumno, con código y QR de canje);
    /// null si el curso no existe en el tenant.</summary>
    Task<TarjetasCursoDto?> GetTarjetasAsync(long cursoId);
}
