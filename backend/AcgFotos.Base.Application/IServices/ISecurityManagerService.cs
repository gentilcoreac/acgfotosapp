using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Identity;
using AcgFotos.Base.Domain.Entities;
using System.Threading.Tasks;

namespace AcgFotos.Base.Application.IServices
{
    public interface ISecurityManagerService
    {
        Task Register(Usuario user);
        Task RegisterManual(Usuario user, string password);
        Task ReConfirmarCuenta(long id);
        Task Delete(long id);
        //Task<bool> UserEmailConfirmed(string userName);
        Task<bool> StateUser(string userName, bool state);

        //Task EnviarCodigoViaMail(long codigo, string email, string userName);
        Task SendForgotEmailAsync(string titulo_mail, ForgotPasswordViewModel model, Usuario usuario, string codeEncode);
    }
}
