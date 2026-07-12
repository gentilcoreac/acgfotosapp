using System.ComponentModel.DataAnnotations;

namespace AcgFotos.Base.Application.Identity
{
    public class ConfirmarCuentaViewModel
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "ErrorFieldMaximumMinimumLength", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "ErrorPasswordConfirmNotMatch")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }
}
