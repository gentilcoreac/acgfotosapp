namespace AcgFotos.Base.Infrastructure.Repositories.Projections
{
    public class RolHeaderProjection
    {
        public long Id { get; set; }
        public string Descripcion { get; set; }
        public bool EsDefaultParaNuevoTenant { get; set; }
    }
}
