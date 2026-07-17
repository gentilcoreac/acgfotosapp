using AcgFotos.Fotos.Application.Dtos;

namespace AcgFotos.Fotos.Application.IServices;

public interface ICanjeAppService
{
    Task<CanjeResultDto> CanjearAsync(CanjeInputDto input);
}
