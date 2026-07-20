using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;

namespace AcgFotos.Core.Email
{
    internal class Correo
    {
        private string _servidorSMTP;

        private int _puerto;

        private bool _ssl;

        private bool _enabled;

        private string _correoRemitente;

        private string _nombreRemitente;

        private List<string> _correosDestinatarios;

        private string _asunto;

        private string _mensaje;

        private string _usuario;

        private string _contrasenia;

        private string _archivoAdjunto;

        private bool _forceTo;

        private string _forceToEmail;

        private List<string>? _copiaOculta;

        private bool _usaLoginAnonimo;

        private bool _bypassCertificadoSsl;

        public string ServidorSMTP
        {
            set
            {
                _servidorSMTP = value;
            }
        }

        public int Puerto
        {
            set
            {
                _puerto = value;
            }
        }

        public bool SSL
        {
            set
            {
                _ssl = value;
            }
        }

        public bool ForceTo
        {
            set
            {
                _forceTo = value;
            }
        }

        public string ForceToEmail
        {
            set
            {
                _forceToEmail = value;
            }
        }

        public string CorreoRemitente
        {
            set
            {
                _correoRemitente = value;
            }
        }

        public string NombreRemitente
        {
            set
            {
                _nombreRemitente = value;
            }
        }

        public List<string> CorreosDestinatarios
        {
            set
            {
                _correosDestinatarios = value;
            }
        }

        public string Asunto
        {
            set
            {
                _asunto = value;
            }
        }

        public string Mensaje
        {
            set
            {
                _mensaje = value;
            }
        }

        public string Usuario
        {
            set
            {
                _usuario = value;
            }
        }

        public string Contrasenia
        {
            set
            {
                _contrasenia = value;
            }
        }

        public string ArchivoAdjunto
        {
            set
            {
                _archivoAdjunto = value;
            }
        }

        public bool Enabled
        {
            set
            {
                _enabled = value;
            }
        }

        public List<string>? CopiaOculta
        {
            set
            {
                _copiaOculta = value;
            }
        }

        public bool UsaLoginAnonimo
        {
            set
            {
                _usaLoginAnonimo = value;
            }
        }

        public bool BypassCertificadoSsl
        {
            set
            {
                _bypassCertificadoSsl = value;
            }
        }

        internal Correo()
        {
        }

        internal Correo(string usuario)
        {
            _usuario = usuario;
        }

        internal async Task EnviarAsync(List<Attachment> files = null)
        {
            try
            {
                MailMessage mailMessage = new MailMessage();
                SmtpClient smtpClient = new SmtpClient();
                mailMessage.From = new MailAddress(_correoRemitente, _nombreRemitente, Encoding.UTF8);

                if (_forceTo)
                {
                    mailMessage.To.Add(_forceToEmail);
                }
                else
                {
                    foreach (string correoDest in _correosDestinatarios)
                    {
                        if (!string.IsNullOrEmpty(correoDest))
                            mailMessage.To.Add(correoDest);
                    }

                    if (_copiaOculta != null && _copiaOculta.Count >= 1)
                    {
                        foreach (string copiaOcultum in _copiaOculta)
                        {
                            if (!string.IsNullOrEmpty(copiaOcultum))
                                mailMessage.Bcc.Add(copiaOcultum);
                        }
                    }
                }

                mailMessage.Subject = _asunto;
                mailMessage.SubjectEncoding = Encoding.UTF8;
                mailMessage.Body = _mensaje;
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.IsBodyHtml = true;
                mailMessage.Priority = MailPriority.High;
                if (!string.IsNullOrEmpty(_archivoAdjunto))
                {
                    mailMessage.Attachments.Add(new Attachment(_archivoAdjunto));
                }

                if (files != null)
                {
                    foreach (Attachment file in files)
                    {
                        mailMessage.Attachments.Add(file);
                    }
                }

                if (_enabled)
                {
                    if (_usaLoginAnonimo)
                    {
                        // Anónimo requiere el flag explícito — no se infiere de credenciales vacías.
                        smtpClient.Credentials = null;
                        smtpClient.UseDefaultCredentials = false;
                    }
                    else if (!String.IsNullOrEmpty(_usuario) && !String.IsNullOrEmpty(_contrasenia))
                    {
                        smtpClient.Credentials = new NetworkCredential(_usuario, _contrasenia);
                    }
                    else
                    {
                        throw new BusinessValidationException(MessagesAPI.ErrorEmailCredentials);
                    }

                    if (_puerto == 0)
                    {
                        throw new BusinessValidationException(MessagesAPI.ErrorEmailPort);
                    }

                    smtpClient.Port = _puerto;
                    if (string.IsNullOrEmpty(_servidorSMTP))
                    {
                        throw new BusinessValidationException(MessagesAPI.ErrorEmailSmtpServer);
                    }

                    smtpClient.Host = _servidorSMTP;
                    smtpClient.EnableSsl = _ssl;

                    if (_bypassCertificadoSsl)
                    {
                        // SYSLIB0014: ServicePointManager está obsoleto en net6+ y no afecta a HttpClient.
                        // Acá se mantiene porque sigue siendo el mecanismo válido para bypass de cert SSL con SmtpClient legacy.
                        // TODO: migrar todo Correo.cs a MailKit cuando se priorice el refactor de email.
#pragma warning disable SYSLIB0014
                        ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
#pragma warning restore SYSLIB0014
                    }

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (SmtpException)
            {
                // Re-tirar preservando stack trace; el ExceptionHandlingMiddleware tipa la respuesta.
                throw;
            }
        }
    }
}
