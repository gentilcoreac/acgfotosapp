using FluentValidation;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Base.Application.Dtos;

namespace AcgFotos.Base.Application.InputValidators {
    public class MenuDtoValidator : AbstractValidator<MenuDto> {
        public MenuDtoValidator() {
            this.RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, AcgFotos.Core.Localization.PublicResources.Fields.Fields.Name));
            this.RuleFor(x => x.Nombre)
                .MaximumLength(50).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, AcgFotos.Core.Localization.PublicResources.Fields.Fields.Name, 50));
            this.RuleFor(x => x.Codigo)
                .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, AcgFotos.Core.Localization.PublicResources.Fields.Fields.Code));
            this.RuleFor(x => x.Codigo)
                .MaximumLength(50).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, AcgFotos.Core.Localization.PublicResources.Fields.Fields.Code, 50));
            //RuleFor(x => x.TipoUso).NotEmpty().WithMessage(string.Format(Mensajes.CampoRequerido, Campos.TipoUso));
            //RuleFor(x => x.Nivel).NotEmpty().WithMessage(string.Format(Mensajes.CampoRequerido, Campos.Nivel));
            this.RuleFor(x => x.Orden)
                .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, AcgFotos.Core.Localization.PublicResources.Fields.Fields.Order));
            //RuleFor(x => x.MenuPadreId).NotEmpty().WithMessage(string.Format(Mensajes.CampoRequerido, Campos.MenuPadreId));
        }
    }
}
