using System.ComponentModel.DataAnnotations;

namespace AcgFotos.Base.Application.Identity
{
    /// <summary>
    /// Pedido de impersonalización (ADR-0002): root elige un <see cref="TenantId"/> y un
    /// <see cref="UserId"/> de ese tenant para operar como ese usuario.
    /// </summary>
    public class ImpersonateViewModel
    {
        [Range(1, long.MaxValue)]
        public long TenantId { get; set; }

        [Range(1, long.MaxValue)]
        public long UserId { get; set; }
    }
}
