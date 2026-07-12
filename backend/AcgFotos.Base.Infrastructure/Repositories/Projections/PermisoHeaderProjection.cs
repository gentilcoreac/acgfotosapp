namespace AcgFotos.Base.Infrastructure.Repositories.Projections
{
    public class PermisoHeaderProjection
    {
        public long Id { get; set; }
        public string Nombre { get; set; }
        public string CodigoPermiso { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
        public string AplicacionDescripcion { get; set; }
        public string PermisoPadreDescripcion { get; set; }
    }
}
