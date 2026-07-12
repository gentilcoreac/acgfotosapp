namespace AcgFotos.Base.Infrastructure.Repositories.Projections
{
    /// <summary>
    /// Shape de proyección SQL para listado de Tenants. Vive en Infrastructure porque es lo que
    /// el repo materializa con .Select(t => new TenantHeaderProjection { ... }). El AppService
    /// lo mapea a TenantHeaderDto (Application) — separación de capas: Infrastructure no
    /// referencia Application.
    /// </summary>
    public class TenantHeaderProjection
    {
        public long Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string TituloWeb { get; set; }
        public bool Activo { get; set; }
        public string HostName { get; set; }
    }
}
