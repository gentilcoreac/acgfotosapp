using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AcgFotos.Core.Email
{
    public interface IEmailSender
    {
        Task SendAsync(string asunto, string mensaje, List<string> destinatarios, Dictionary<string, string> parametros = null, List<string> copiaOculta = null);

        Task SendAsync(string asunto, string mensaje, string destinatario, Dictionary<string, string> parametros = null, List<string> copiaOculta = null);

        Task SendAsync(string asunto, string mensaje, string destinatario, Stream file, string fileName, Dictionary<string, string> parametros = null, List<string> copiaOculta = null);

        List<string> ObtenerParametros();
    }
}
