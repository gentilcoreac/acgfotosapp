using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class TipoLicenciaHeaderDto : HeaderDtoBase
    {
        public string CodigoTipoLicencia { get; set; }

        public string Descripcion { get; set; }

        public bool EsDefaultParaNuevoTenant { get; set; }
    }
}
