using System;

namespace AcgFotos.Base.Application.Outputs
{
    public class GetCantidadLicenciasDisponiblesOutput
    {
        public long TipoLicenciaId { get; set; }

        public string Descripcion { get; set; }

        public int CantidadTotal { get; set; }

        public int CantidadDisponible { get; set; }

        public int CantidadAsignada { get; set; }

        public bool IsExpired { get; set; }

        public bool IsExpiringSoon { get; set; }

        public DateTime? ExpirationDate { get; set; }
    }
}
