using System.ComponentModel.DataAnnotations;

namespace AcgFotos.Base.Application.Identity
{
    public class ChangePasswordInputModel
    {
        [Required(ErrorMessage = "PasswordRequired")]
        public string CurrentPassword { get; set; }


        [Required(ErrorMessage = "ErrorPasswordConfirmIsRequired")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "ErrorPasswordConfirmIsRequired")]
        [Compare("NewPassword", ErrorMessage = "ErrorPasswordConfirmNotMatch")]
        public string NewConfirmPassword { get; set; }
    }
}

