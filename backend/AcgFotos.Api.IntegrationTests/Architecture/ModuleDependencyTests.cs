using System.Linq;
using System.Reflection;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Architecture
{
    /// <summary>
    /// Invariante del DAG de módulos (ADR-0004 / ADR-0009 D7): la plataforma <c>Base</c> NO conoce a los
    /// verticales, y los verticales NO se referencian entre sí. Un vertical SÍ puede referenciar
    /// <c>Base.Domain</c> (ADR-0004: navegación EF a Usuario/Grupo sobre el DbContext único). Estos tests son
    /// puro reflection sobre las referencias directas — no necesitan host ni DB.
    /// Al agregar el vertical Fotos, sumar acá sus assemblies como Theory de no-referencia cruzada.
    /// </summary>
    public class ModuleDependencyTests
    {
        private static System.Collections.Generic.IEnumerable<string> DirectRefsOf(string assemblyName) =>
            Assembly.Load(assemblyName).GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => n != null)!;

        [Theory] // Base es la plataforma compartida: NUNCA referencia a un vertical (Fotos u otros).
        [InlineData("AcgFotos.Base.Domain")]
        [InlineData("AcgFotos.Base.Application")]
        [InlineData("AcgFotos.Base.Infrastructure")]
        [InlineData("AcgFotos.Base.Controllers")]
        public void Base_no_referencia_verticales(string baseAssembly)
        {
            var offenders = DirectRefsOf(baseAssembly)
                .Where(r => r!.StartsWith("AcgFotos.Fotos"))
                .ToList();
            Assert.True(offenders.Count == 0, $"{baseAssembly} no debe referenciar verticales, pero referencia: {string.Join(", ", offenders)}");
        }
    }
}
