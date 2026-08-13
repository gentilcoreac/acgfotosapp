using FluentValidation;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.InputValidators;

/// <summary>
/// Validación de forma del input de perfil de marca de agua (ADR-15). Descubierta por convención de
/// nombre en <c>ExtendedEntityAppServiceBase.CheckInputValidations</c>.
/// </summary>
public class PerfilMarcaAguaInputDtoValidator : AbstractValidator<PerfilMarcaAguaInputDto>
{
    public PerfilMarcaAguaInputDtoValidator()
    {
        this.RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "Nombre"))
            .MaximumLength(100).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "Nombre", 100));

        this.RuleFor(x => x.Capas.Count)
            .GreaterThanOrEqualTo(1).WithMessage("Un perfil necesita al menos una capa.")
            .LessThanOrEqualTo(3).WithMessage("Un perfil admite hasta 3 capas.");

        this.RuleForEach(x => x.Capas).ChildRules(capa =>
        {
            // Id 0 = capa nueva sin contenido: no existe ese camino, se sube por SubirCapaAsync (D14).
            capa.RuleFor(c => c.Id)
                .GreaterThan(0)
                .WithMessage("Las capas nuevas se suben con su imagen antes de guardar el perfil.");

            capa.RuleFor(c => c.EscalaPorcentaje)
                .InclusiveBetween(1f, 100f).WithMessage("La escala debe estar entre 1% y 100% del ancho de la foto.");

            capa.RuleFor(c => c.MargenPorcentaje)
                .InclusiveBetween(0f, 50f).WithMessage("El margen debe estar entre 0% y 50% del ancho de la foto.");

            // El piso no es 0: un paso de 0 no avanza la grilla y dejaría el mosaico dibujando para
            // siempre sobre el mismo punto.
            capa.RuleFor(c => c.SeparacionPorcentaje)
                .InclusiveBetween(1f, 200f)
                .When(c => c.ModoColocacion == ModoColocacionMarcaAgua.Repetida)
                .WithMessage("La separación debe estar entre 1% y 200% del ancho de la foto.");

            capa.RuleFor(c => c.AnguloGrados)
                .InclusiveBetween(-180f, 180f).WithMessage("El ángulo debe estar entre -180° y 180°.");

            capa.RuleFor(c => c.Opacidad)
                .InclusiveBetween(0f, 1f).WithMessage("La opacidad debe estar entre 0 y 1.");

            capa.RuleFor(c => c.ModoColocacion).IsInEnum();
            capa.RuleFor(c => c.ModoFusion).IsInEnum();

            capa.RuleFor(c => c.Posicion)
                .NotNull()
                .When(c => c.ModoColocacion == ModoColocacionMarcaAgua.PosicionFija)
                .WithMessage("Una capa en posición fija necesita elegir una de las 9 posiciones.");
        });
    }
}
