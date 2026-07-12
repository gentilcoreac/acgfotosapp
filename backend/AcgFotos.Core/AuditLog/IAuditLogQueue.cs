namespace AcgFotos.Core.AuditLog
{
    /// <summary>
    /// Cola en memoria entre el AuditLogFilter (productor, corre en el camino del request) y el
    /// AuditLogBackgroundWriter (consumidor, persiste en lote fuera del request). Ver ADR-0005.
    /// </summary>
    public interface IAuditLogQueue
    {
        /// <summary>
        /// Encola sin bloquear jamás. Devuelve false si la cola está llena (la entrada se
        /// descarta: el audit log nunca debe frenar la request).
        /// </summary>
        bool TryEnqueue(AuditLogModel entry);
    }
}
