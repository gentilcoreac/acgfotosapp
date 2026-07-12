using System.Transactions;

namespace AcgFotos.Core.Application
{
    /// <summary>
    /// Fábrica de <see cref="TransactionScope"/> con <see cref="IsolationLevel"/> EXPLÍCITO = ReadCommitted.
    ///
    /// El default de .NET para <c>new TransactionScope(...)</c> es <b>Serializable</b> (toma range locks →
    /// más bloqueo y deadlocks; y como la base de metadata es compartida entre tenants, los range locks sobre
    /// las tablas compartidas pueden bloquear escrituras de OTROS tenants aunque toquen filas distintas).
    /// Este helper unifica el aislamiento a ReadCommitted (el default de SQL Server) en todos los
    /// TransactionScope de escritura de la aplicación (CONC-1) — además evita el error de "distinto
    /// IsolationLevel" al anidar scopes.
    /// </summary>
    public static class TransactionScopeFactory
    {
        public static TransactionScope CreateReadCommitted() =>
            new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                TransactionScopeAsyncFlowOption.Enabled);
    }
}
