using System.Collections.Generic;

namespace AcgFotos.Core.AuditLog
{
    public interface IAuditLogRepository
    {
        /// <summary>Inserta un lote de entradas de auditoría (INSERT multi-fila parametrizado).</summary>
        void WriteLogs(IReadOnlyList<AuditLogModel> entries);
    }
}
