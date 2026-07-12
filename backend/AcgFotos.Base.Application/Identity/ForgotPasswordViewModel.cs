using System.ComponentModel.DataAnnotations;

namespace AcgFotos.Base.Application.Identity
{
    public class ForgotPasswordViewModel
    {
        [Required]
        //[EmailAddress]
        //[DataType(DataType.EmailAddress)]
        public string EmailOrUsername { get; set; }
    }
}
