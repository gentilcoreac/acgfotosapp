using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Infrastructure.Configurations {
    class AuditLogEFConfig : IEntityTypeConfiguration<Auditoria> {
        public void Configure(EntityTypeBuilder<Auditoria> builder) {
            builder.HasKey(e => e.Id);

            builder.ToTable("gen_AuditLogs");

            // Límites acordes al contenido real (nombres de controller/action, verbos, paths):
            // antes era todo nvarchar(max). AuditLogRepository trunca en escritura a estos mismos
            // largos, así un valor hostil (ej. un User-Agent de 10k) no puede reventar el INSERT.
            builder.Property(x => x.Metodo).HasMaxLength(100);
            builder.Property(x => x.Servicio).HasMaxLength(100);

            builder.Property(x => x.FechaHora).IsRequired();

            builder.Property(x => x.UsuarioId);
            builder.Property(x => x.ImpersonatedBy);

            builder.Property(x => x.HttpMethod).HasMaxLength(10);
            builder.Property(x => x.RequestAbsolutePath).HasMaxLength(2000);
            // Parametros y ResultContent quedan nvarchar(max) (genuinamente variables), pero el
            // repositorio los capea en escritura ("AuditLog:MaxContentChars", default 8000) para
            // acotar el tamaño de fila — en dev ya había filas con Parametros de ~2,2 MB.
            builder.Property(x => x.Parametros);

            builder.Property(x => x.ClientIP).HasMaxLength(50);
            builder.Property(x => x.ClientUserAgent).HasMaxLength(1000);

            builder.Property(x => x.ResultStatusCode).HasMaxLength(10);
            builder.Property(x => x.ResultContent);

            // La grilla de auditoría ordena por FechaHora (paginada): sin índice, cada página
            // implica un TOP-N sort sobre TODA la tabla (append-only, crece sin tope). Como
            // FechaHora es monotónica, los INSERT pegan siempre en la última página del índice
            // (costo de mantenimiento mínimo, sin splits). Los demás filtros de la pantalla son
            // Contains (no indexables); FechaDesde/Hasta del criteria hoy no se aplican en el
            // repo — cuando se cableen, este mismo índice les sirve de rango.
            builder.HasIndex(x => x.FechaHora).HasDatabaseName("ix_auditlog_fechahora");
        }
    }
}
