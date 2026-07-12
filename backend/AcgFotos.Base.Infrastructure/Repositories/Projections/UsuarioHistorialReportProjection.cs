using System;

namespace AcgFotos.Base.Infrastructure.Repositories.Projections
{
    /// <summary>
    /// Fila del historial de logins resuelta con LEFT JOIN explícito a gen_Usuarios
    /// (no hay FK/navegación: el historial sobrevive al borrado del usuario).
    /// Si el usuario fue borrado, Nombre/Apellido/UserName/Email quedan en null
    /// pero la fila igual se cuenta.
    /// </summary>
    public class UsuarioHistorialReportProjection
    {
        public long Id { get; set; }
        public long UsuarioId { get; set; }
        public long TenantId { get; set; }
        public string Periodo { get; set; }
        public DateTime FechaLastLogin { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }
}
