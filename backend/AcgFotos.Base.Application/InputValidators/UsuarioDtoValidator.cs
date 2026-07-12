using FluentValidation;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Base.Application.Dtos;


namespace AcgFotos.Base.Application.InputValidators
{
    class UsuarioDtoValidator : AbstractValidator<UsuarioDto>
    {
        public UsuarioDtoValidator()
        {
            this.RuleFor(x => x.Telefono).Must((entity, tel) => {
                if (entity.Telefono != null) {
                    if (tel.ToString().Length >= 10) { 
                        return true;
                    } else {
                        return false;
                    }
                }
                return true;
            }).WithMessage(string.Format(MessagesAPI.ErrorPhoneNotValid, "minima",  10));

            this.RuleFor(x => x.Telefono).Must((entity, tel) => {
                if (entity.Telefono != null) {
                    if (tel.ToString().Length <= 13) {
                        return true;
                    } else {
                        return false;
                    }
                }
                return true;
            }).WithMessage(string.Format(MessagesAPI.ErrorPhoneNotValid, "maxima", 13));
   
            this.RuleFor(x => x.Email).EmailAddress().WithMessage(string.Format(MessagesAPI.ErrorEmailNotValid, AcgFotos.Core.Localization.PublicResources.Fields.Fields.Email));

            this.RuleFor(x => x.ProfilePicture).Must((entity, pp) => {
                if (entity.ProfilePicture != null)
                {
                    if (pp.Length < 2 * 1024 * 1024)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }).WithMessage(string.Format(MessagesAPI.ErrorFileNotValid, "2"));
        }
    }
}
