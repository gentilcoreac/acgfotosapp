using FluentValidation;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Base.Application.Dtos;

namespace AcgFotos.Base.Application.InputValidators
{
    public class EndpointDtoValidator : AbstractValidator<EndpointDto>
    {
        public EndpointDtoValidator()
        {
            this.RuleFor(x => x.ModuleName)
                .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "ModuleName"));
            this.RuleFor(x => x.ModuleName)
                .MaximumLength(100).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "ModuleName", 100));
            this.RuleFor(x => x.ControllerName)
                .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "ControllerName"));
            this.RuleFor(x => x.ControllerName)
                .MaximumLength(100).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "ControllerName", 100));
            this.RuleFor(x => x.ActionName)
                .NotEmpty().WithMessage(string.Format(MessagesAPI.ErrorFieldIsRequired, "ActionName"));
            this.RuleFor(x => x.ActionName)
                .MaximumLength(100).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "ActionName", 100));
            this.RuleFor(x => x.HttpMethod)
                .MaximumLength(10).WithMessage(string.Format(MessagesAPI.WarnFieldMaxLength, "HttpMethod", 10));
        }
    }
}
