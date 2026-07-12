using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AcgFotos.Core.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public List<string> ObtenerParametros()
        {
            return new List<string> { "Fwk.Email.ForceTo","Fwk.Email.ForceToEmail",
                                      "Fwk.Email.From", "Fwk.Email.Password", "Fwk.Email.User", "Fwk.Email.NombreRemitente",
                                      "Fwk.Email.SMTPServer", "Fwk.Email.Port",
                                      "Fwk.Email.UseAnonymousLogin", "Fwk.Email.BypassCertificate", "Fwk.Email.SSL",
                                      "Fwk.Email.EmailEnabled" };
        }

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendAsync(string asunto, string mensaje, string destinatario, Dictionary<string, string> parametros = null, List<string> copiaOculta = null) =>
            this.SendAsync(asunto, mensaje, new List<string> { destinatario }, parametros, copiaOculta);

        public Task SendAsync(string asunto, string mensaje, List<string> destinatarios, Dictionary<string, string> parametros = null, List<string> copiaOculta = null)
        {
            Correo correo = new Correo
            {
                CorreosDestinatarios = destinatarios,
                Asunto = asunto,
                Mensaje = mensaje,
                CopiaOculta = copiaOculta
            };
            Setup(correo, parametros);
            return correo.EnviarAsync();
        }

        public Task SendAsync(string asunto, string mensaje, string destinatario, Stream file, string fileName, Dictionary<string, string> parametros = null, List<string> copiaOculta = null)
        {
            List<Attachment> files = null;
            if (file != null)
            {
                files = new List<Attachment>
                {
                    new Attachment(file, fileName)
                };
            }

            Correo correo = new Correo
            {
                CorreosDestinatarios = new List<string> { destinatario },
                Asunto = asunto,
                Mensaje = mensaje,
                CopiaOculta = copiaOculta
            };
            Setup(correo, parametros);
            return correo.EnviarAsync(files);
        }

        private void Setup(Correo correo, Dictionary<string, string> parametros = null)
        {
            if (parametros != null && !parametros.All(kvp => string.IsNullOrEmpty(kvp.Value) || kvp.Value == "0"))
            {
                correo.ForceTo = parametros["Fwk.Email.ForceTo"].ToUpper().Equals("TRUE");
                correo.ForceToEmail = parametros["Fwk.Email.ForceToEmail"];
                correo.CorreoRemitente = parametros["Fwk.Email.From"];
                correo.Contrasenia = parametros["Fwk.Email.Password"];
                correo.Usuario = parametros["Fwk.Email.User"];
                correo.NombreRemitente = parametros["Fwk.Email.NombreRemitente"];
                correo.ServidorSMTP = parametros["Fwk.Email.SMTPServer"];
                correo.Puerto = Convert.ToInt32(parametros["Fwk.Email.Port"]);
                correo.SSL = parametros["Fwk.Email.SSL"].ToUpper().Equals("TRUE");
                correo.Enabled = parametros["Fwk.Email.EmailEnabled"].ToUpper().Equals("TRUE");
                correo.BypassCertificadoSsl = parametros["Fwk.Email.BypassCertificate"].ToUpper().Equals("TRUE");
                correo.UsaLoginAnonimo = parametros["Fwk.Email.UseAnonymousLogin"].ToUpper().Equals("TRUE");
                return;
            }

            IConfigurationSection section = _configuration.GetSection("Email");
            correo.ForceTo = section.GetValue<bool>("ForceTo");
            correo.ForceToEmail = section.GetValue<string>("ForceToEmail");
            correo.CorreoRemitente = section.GetValue<string>("From");
            correo.Contrasenia = section.GetValue<string>("Password");
            correo.Usuario = section.GetValue<string>("Usuario");
            correo.NombreRemitente = section.GetValue<string>("DisplayName");
            correo.ServidorSMTP = section.GetValue<string>("SMTPServer");
            correo.Puerto = section.GetValue<int>("Port");
            correo.SSL = section.GetValue<bool>("SSL");
            correo.Enabled = section.GetValue<bool>("EmailEnabled");
            correo.BypassCertificadoSsl = section.GetValue<bool>("BypassCertificate");
            correo.UsaLoginAnonimo = section.GetValue<bool>("UseAnonymousLogin");
        }
    }
}
