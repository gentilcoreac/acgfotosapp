using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

// Hay dos colecciones de integracion (host con authz OFF y host con authz ON) que comparten la MISMA
// base AcgFotos_Tests y la resetean con Respawn antes de cada test. xUnit corre colecciones distintas en
// PARALELO por defecto -> se pisarian la base. Se desactiva el paralelismo global: todo corre en serie.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AcgFotos.Api.IntegrationTests.Infrastructure
{
    internal static class TestCultureInitializer
    {
        // La API resuelve los mensajes (MessagesAPI) en en-US (el ApiClient manda header cultureInfo=en-US).
        // Las aserciones leen MessagesAPI en el HILO DEL TEST, cuya cultura por defecto es la del SO: en una
        // maquina con locale es-* el "expected" salia en español y no matcheaba la respuesta en ingles.
        // Se fija la cultura del proceso de test a en-US para que ambos lados coincidan en cualquier SO
        // (determinismo; el suite original dependia implicitamente de correr en un SO en-US).
        [ModuleInitializer]
        public static void Init()
        {
            var enUs = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = enUs;
            CultureInfo.DefaultThreadCurrentUICulture = enUs;
        }
    }
}
