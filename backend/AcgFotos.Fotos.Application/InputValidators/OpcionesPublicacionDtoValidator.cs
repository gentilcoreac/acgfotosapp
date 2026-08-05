using FluentValidation;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Fotos.Application.Dtos;

namespace AcgFotos.Fotos.Application.InputValidators;

/// <summary>
/// Validación de forma del input de opciones de publicación (ADR-15 §5). Descubierta por convención
/// de nombre en <c>ExtendedEntityAppServiceBase.CheckInputValidations</c>.
/// </summary>
public class OpcionesPublicacionDtoValidator : AbstractValidator<OpcionesPublicacionDto>
{
    public OpcionesPublicacionDtoValidator()
    {
        this.RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "Nombre"))
            .MaximumLength(100).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "Nombre", 100));

        this.RuleFor(x => x.LadoMayorPreview)
            .InclusiveBetween(100, 4000).WithMessage("El lado mayor del preview debe estar entre 100 y 4000px.");

        this.RuleFor(x => x.LadoMayorThumb)
            .InclusiveBetween(50, 2000).WithMessage("El lado mayor del thumbnail debe estar entre 50 y 2000px.");

        this.RuleFor(x => x.Calidad)
            .InclusiveBetween(1, 100).WithMessage("La calidad debe estar entre 1 y 100.");
    }
}
