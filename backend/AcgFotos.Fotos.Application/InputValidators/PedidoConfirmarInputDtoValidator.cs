using FluentValidation;
using AcgFotos.Fotos.Application.Dtos;

namespace AcgFotos.Fotos.Application.InputValidators;

/// <summary>
/// Validación de FORMA del input de confirmar pedido (largo de campos, cantidades, duplicados) —
/// no de negocio (eso pide repos: si la foto es visible para la sesión, si el tamaño está activo
/// en el catálogo del evento — se queda en <see cref="AcgFotos.Fotos.Application.Services.FamiliaPedidoAppService"/>).
/// A diferencia de <see cref="EventoInputDtoValidator"/>, acá NO hay un <c>ExtendedEntityAppServiceBase</c>
/// que lo descubra por convención (un pedido no es un CRUD Extended) — se instancia a mano en el AppService.
/// </summary>
public class PedidoConfirmarInputDtoValidator : AbstractValidator<PedidoConfirmarInputDto>
{
    private const int MaxLongitudNombre = 200;
    private const int MaxLongitudTelefono = 30;
    private const int MaxCantidadPorLinea = 50;

    public PedidoConfirmarInputDtoValidator()
    {
        this.RuleFor(x => x.NombreContacto)
            .NotEmpty().WithMessage("Ingresá un nombre de contacto válido.")
            .MaximumLength(MaxLongitudNombre).WithMessage("Ingresá un nombre de contacto válido.");

        this.RuleFor(x => x.TelefonoContacto)
            .NotEmpty().WithMessage("Ingresá un teléfono de contacto válido.")
            .MaximumLength(MaxLongitudTelefono).WithMessage("Ingresá un teléfono de contacto válido.");

        this.RuleFor(x => x.Items)
            .NotEmpty().WithMessage("El pedido no tiene fotos.");

        this.RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Cantidad)
                .InclusiveBetween(1, MaxCantidadPorLinea)
                .WithMessage("Hay una cantidad inválida en el pedido.");
        });

        this.RuleFor(x => x.Items)
            .Must(items => items.Select(i => (i.FotoId, i.TamanoPrecioId)).Distinct().Count() == items.Count)
            .WithMessage("El pedido tiene fotos repetidas con el mismo tamaño.")
            .When(x => x.Items.Count > 0);
    }
}
