using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class UsuarioGrupoDto : DtoBase
    {
        public long GrupoId { get; set; }
        public long UsuarioId { get; set; }

        // Datos de display del miembro (solo se llenan en el detalle/getById, para que el front
        // muestre los miembros actuales como chips en edición sin tener que cargar todos los usuarios).
        public string UsuarioUserName { get; set; }
        public string UsuarioNombre { get; set; }
        public string UsuarioApellido { get; set; }

        // Licencia activa del miembro (solo detalle/getById). La usa el ABM para avisar si el grupo
        // mezcla usuarios de distinta licencia (§11.2): cada uno solo recibe los roles que su licencia
        // incluye. null = sin licencia activa.
        public long? UsuarioTipoLicenciaActivaId { get; set; }
    }
}
