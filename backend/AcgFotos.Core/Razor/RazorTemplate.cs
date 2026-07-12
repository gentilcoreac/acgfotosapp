using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using RazorEngine;
using RazorEngine.Templating;
using System;
using System.IO;
using System.Reflection;

namespace AcgFotos.Core.Razor
{
    /// <summary>
    /// Esta clase requiere el nombre del template, su key el modelo que bindeara y su assembly para formar la ruta del archivo..
    /// </summary>
    public class RazorTemplate
    {

        //private readonly ILogger _logger;

        /// <remarks>
        /// <para>TemplateName: nombre del archivo cshtml a leer</para>
        /// </remarks>
        public string TemplateName { get; set; }
       
        /// <remarks>
        /// <para>Assembly: se requiere ya que obtenemos el archivo por su ruta de esta manera</para>
        /// </remarks>
        public Assembly Assembly { get; set; }
        /// <remarks>
        /// <para>Model: el objeto que se parasara al modelo del archivo cshtml</para>
        /// </remarks>
        public object Model { get; set; }
        /// <remarks>
        /// <para>Chapter: ingresa los nombres de las carpetas que contienen al archivo cshtml... teniendo un punto delante y otro atrás</para>
        /// <para>Ej -> ".Email." || ".Email.Templates." || ".Templates."</para>
        /// <para>Ej -> si no esta contenido por otra carpeta usar solamente "."</para>
        /// </remarks>
        public string Chapter { get; set; }

        /// <summary>
        /// Esta clase requiere el nombre del template, su key el modelo que bindeara y su assembly para formar la ruta del archivo..
        /// </summary>
        /// 
        //public RazorTemplate(ILoggerFactory loggerFactory)
        //{
        //    _logger = loggerFactory.CreateLogger<ILogger>();
        //}

        //public RazorTemplate(ILogger logger)
        //{
        //    _logger = logger;
        //}

        public string RenderRazorViewToString()
        {
            try
            {
                var templateSource = this.ConvertRazorToString();
                var result = Engine.Razor.RunCompile(templateSource, 
                                                     Guid.NewGuid().ToString(), 
                                                     null,
                                                     this.Model);

                return result;
            }
            catch (Exception)
            {
                //_logger.LogError(ex, ex.Message);
                throw new BusinessValidationException(MessagesAPI.ErrorRazorGenerate);
            }
        }

        private string ConvertRazorToString()
        {
            try
            {
                var templateHtml = string.Empty;

                using (var rsrcStream = this.Assembly.GetManifestResourceStream(Assembly.GetName().Name + Chapter + TemplateName))
                {
                    using (var sRdr = new StreamReader(rsrcStream))
                    {
                        templateHtml = sRdr.ReadToEnd();
                    }
                }
                return templateHtml;
            }
            catch (Exception)
            {
               // _logger.LogError(ex, ex.Message);
                throw new BusinessValidationException(MessagesAPI.ErrorRazorGenerate);
            }

        }
    }
}
