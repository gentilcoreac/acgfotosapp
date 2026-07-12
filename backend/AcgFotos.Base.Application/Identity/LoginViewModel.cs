using System.ComponentModel.DataAnnotations;

namespace AcgFotos.Base.Application.Identity
{
    public class LoginViewModel
    {
        [Required]
        public string UserName { get; set; }


        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

    }
}
