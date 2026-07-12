using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace AcgFotos.Core.Infrastructure
{
    /// <summary>
    /// Descubrimiento de assemblies por módulo (ADR-0009 D2). El <c>DbContext</c> único es
    /// <b>módulo-agnóstico</b>: en vez de listar a mano las EF configs, recorre los módulos
    /// declarados en <c>AppModulesName</c> y aplica las <c>IEntityTypeConfiguration</c> de cada
    /// <c>&lt;módulo&gt;.Infrastructure</c>. Así, agregar un módulo NO obliga a editar el contexto.
    /// </summary>
    /// <remarks>
    /// Reflection de <b>arranque</b>: corre una sola vez al construir el modelo EF (que se cachea por
    /// vida del proceso) → costo por request = 0.
    /// </remarks>
    public static class ModuleAssemblies
    {
        /// <summary>
        /// Devuelve los assemblies <c>&lt;módulo&gt;.Infrastructure</c> cargables de los módulos
        /// declarados en <c>AppModulesName</c> (separados por ';'). Un módulo sin proyecto de
        /// infraestructura (p.ej. sin store relacional) se omite silenciosamente.
        /// </summary>
        public static IReadOnlyList<Assembly> Infrastructure(IConfiguration configuration)
        {
            var raw = configuration?["AppModulesName"] ?? string.Empty;
            var modules = raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var result = new List<Assembly>();
            foreach (var module in modules)
            {
                try
                {
                    result.Add(Assembly.Load($"{module}.Infrastructure"));
                }
                catch
                {
                    // El módulo puede no tener proyecto de infraestructura todavía (o no ser relacional).
                    // No es un error: simplemente no aporta EF configs.
                }
            }

            return result;
        }
    }
}
