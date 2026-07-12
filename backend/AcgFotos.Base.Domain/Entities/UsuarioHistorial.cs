using AcgFotos.Core.Domain;
using System;

namespace AcgFotos.Base.Domain.Entities
{
    public class UsuarioHistorial : MultiTenantEntityBase
    {
        public long UsuarioId { get; set; }
        public string Periodo { get; set; }
        public DateTime FechaLastLogin { get; set; }

        // Sin navegación a Usuario a propósito: no hay FK física a gen_Usuarios.
        // El historial de logins debe sobrevivir al borrado del usuario; UsuarioId
        // queda como columna escalar y los reportes resuelven el usuario con JOIN explícito.
        // (Patrón alineado con el proyecto derivado Raymos.)
    }
}
