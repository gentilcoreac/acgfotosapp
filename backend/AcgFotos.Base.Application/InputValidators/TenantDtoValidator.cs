using FluentValidation;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Base.Application.Dtos;

namespace AcgFotos.Base.Application.InputValidators
{
    // Las reglas comentadas del legacy (ColorPrimario/EmailPort/EmailFrom/etc.) referenciaban campos
    // que ya no existen en TenantDto — se sacaron. Nombre/Codigo espejan gen_Tenants (TenantEFConfig:
    // HasMaxLength(100).IsRequired() en ambos) — antes esta clase no validaba nada, así que violarlos
    // recién fallaba como DbUpdateException cruda al persistir en vez de un 400 de negocio.
    public class TenantDtoValidator : AbstractValidator<TenantDto>
    {
        public TenantDtoValidator()
        {
            this.RuleFor(x => x.Codigo)
                .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, AcgFotos.Core.Localization.PublicResources.Fields.Fields.Code));
            this.RuleFor(x => x.Codigo)
                .MaximumLength(100).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, AcgFotos.Core.Localization.PublicResources.Fields.Fields.Code, 100));
            this.RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, AcgFotos.Core.Localization.PublicResources.Fields.Fields.Name));
            this.RuleFor(x => x.Nombre)
                .MaximumLength(100).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, AcgFotos.Core.Localization.PublicResources.Fields.Fields.Name, 100));
        }
    }
}
