using FluentValidation;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Fotos.Application.Dtos;

namespace AcgFotos.Fotos.Application.InputValidators;

/// <summary>
/// Validación de forma del input de Grupo. La descubre y ejecuta
/// <c>ExtendedEntityAppServiceBase.CheckInputValidations</c> por convención de nombre
/// (&lt;Dto&gt;Validator en InputValidators) — corre en el UpdateAsync heredado.
/// </summary>
public class GrupoInputDtoValidator : AbstractValidator<GrupoInputDto>
{
    public GrupoInputDtoValidator()
    {
        this.RuleFor(x => x.EventoId)
            .GreaterThan(0).WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "Evento"));

        this.RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "Nombre"))
            .MaximumLength(100).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "Nombre", 100));

        this.RuleForEach(x => x.Participantes).ChildRules(participante =>
        {
            participante.RuleFor(a => a.Nombre)
                .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "Participante"))
                .MaximumLength(150).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "Participante", 150));
        });
    }
}
