using FluentValidation;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Fotos.Application.Dtos;

namespace AcgFotos.Fotos.Application.InputValidators;

/// <summary>
/// Validación de forma del input de Evento. La descubre y ejecuta
/// <c>ExtendedEntityAppServiceBase.CheckInputValidations</c> por convención de nombre
/// (&lt;Dto&gt;Validator en InputValidators) — corre en el UpdateAsync heredado.
/// </summary>
public class EventoInputDtoValidator : AbstractValidator<EventoInputDto>
{
    public EventoInputDtoValidator()
    {
        this.RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "Nombre"))
            .MaximumLength(200).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "Nombre", 200));

        this.RuleFor(x => x.LugarOrganizacion)
            .MaximumLength(200).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "Lugar/Organización", 200));

        this.RuleForEach(x => x.TamanosPrecios).ChildRules(tamano =>
        {
            tamano.RuleFor(t => t.Nombre)
                .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "Tamaño"))
                .MaximumLength(50).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "Tamaño", 50));

            tamano.RuleFor(t => t.PrecioUnitario)
                .GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo.");
        });
    }
}
