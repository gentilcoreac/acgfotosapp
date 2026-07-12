using FluentValidation;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Fotos.Application.Dtos;

namespace AcgFotos.Fotos.Application.InputValidators;

/// <summary>
/// Validación de forma del input de Curso. La descubre y ejecuta
/// <c>ExtendedEntityAppServiceBase.CheckInputValidations</c> por convención de nombre
/// (&lt;Dto&gt;Validator en InputValidators) — corre en el UpdateAsync heredado.
/// </summary>
public class CursoInputDtoValidator : AbstractValidator<CursoInputDto>
{
    public CursoInputDtoValidator()
    {
        this.RuleFor(x => x.EventoId)
            .GreaterThan(0).WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "Evento"));

        this.RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "Nombre"))
            .MaximumLength(100).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "Nombre", 100));

        this.RuleForEach(x => x.Albumes).ChildRules(album =>
        {
            album.RuleFor(a => a.NombreAlumno)
                .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "Alumno"))
                .MaximumLength(150).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "Alumno", 150));
        });
    }
}
