namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// Usuario candidato a impersonalización (ADR-0002): datos mínimos para el selector del front
    /// (id como target + nombre/usuario para mostrar). Read-only; lista **todos** los usuarios del
    /// tenant elegido (no solo administradores).
    /// </summary>
    public class ImpersonatableUsuarioDto
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
    }
}
