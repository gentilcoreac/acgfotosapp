using System.Collections.Generic;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// Shape de input para Update/Create de Usuario. NO incluye campos que el cliente
    /// no debe poder setear directamente:
    ///   - Administrador: se cambia por endpoint dedicado POST /usuarios/{id}/set-administrador.
    ///   - TenantId: viene fijado por el AppContext, no del body.
    ///   - Identity (PasswordHash, SecurityStamp, ConcurrencyStamp, NormalizedEmail,
    ///     NormalizedUserName, EmailConfirmed, LockoutEnd, AccessFailedCount, LockoutEnabled,
    ///     PhoneNumberConfirmed, TwoFactorEnabled): solo Identity los toca.
    /// La defensa principal es el shape: el deserializer no encuentra estas props.
    ///
    /// SÍ se aceptan (se necesitan para el ABM):
    ///   - UserName: requerido en el alta (Identity.CreateAsync lo exige). En la edición es
    ///     inmutable: el front lo manda deshabilitado y, además, ExecuteEdit lo restaura desde
    ///     la entidad existente (red de seguridad en runtime).
    ///   - TipoLicenciaActiva: asignación de licencia del usuario; UpdateInternalAsync la procesa
    ///     (cupos, activación/desactivación). No es un campo sensible de Identity.
    /// Cualquier campo aquí que se sume después implica revisar la convención
    /// (ver CONTRIBUTING.md / "Mass assignment defense pattern").
    /// </summary>
    public class UsuarioInputDto : DtoBase
    {
        public string UserName { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public long? Telefono { get; set; }
        public byte[] ProfilePicture { get; set; }
        public bool Bloqueado { get; set; }

        public ICollection<UsuarioRolDto> Roles { get; set; }
        public ICollection<UsuarioAplicacionDto> UsuarioAplicaciones { get; set; }

        public UsuarioTipoLicenciaDto TipoLicenciaActiva { get; set; }

        public UsuarioInputDto()
        {
            this.Roles = new List<UsuarioRolDto>();
            this.UsuarioAplicaciones = new List<UsuarioAplicacionDto>();
        }
    }
}
