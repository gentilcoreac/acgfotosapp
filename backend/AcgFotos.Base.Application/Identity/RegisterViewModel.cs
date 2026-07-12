using System.ComponentModel.DataAnnotations;

namespace AcgFotos.Base.Application.Identity
{
    public class RegisterViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "ErrorFieldMaximumMinimumLength", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "ErrorPasswordConfirmIsRequired")]
        public string ConfirmPassword { get; set; }
    }
}
