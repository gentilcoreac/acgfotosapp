namespace AcgFotos.Base.Application.Outputs
{
    /// <summary>
    /// Este output se utiliza para la edicion de los parametrosValor a nivel de frontend
    /// Registro que representa un Parametro al cual se le reemplaza su valor original por el ParametroValor del tenant en caso de existir.
    /// Se envia el Id del ParametroValor para tener referencia del registro y poder editarlo desde el frontend.
    /// Se envia el valor original del parametro para poder resetearlo en caso de ser necesario desde el frontend.
    /// </summary>
    public class ParametroValorOutput
    {
        public long Id { get; set; }
        public string Nombre { get; set; }
        public string Valor { get; set; }
        public string Descripcion { get; set; }
        public bool EsPrivado { get; set; }
        public string TagCategoria { get; set; }
        public int TipoDato { get; set; } // Entero, Booleano, Texto
        public long? PermisoId { get; set; }
        public long AplicacionId { get; set; }
        public long? ParametroValorUsuarioId { get; set; }
        public long? ParametroValorRolId { get; set; }
        public long? ParametroValorTenantId { get; set; }

        //Esta propiedad nos permite saber cuando el valor fue reemplazado por un valor custom para el tenant actual
        public long? ParametroValorId { get; set; }

        public string ParametroValorTenant { get; set; }
        public string ParametroValorUsuario { get; set; }
        public string ParametroValorRol { get; set; }

        //Esta propiedad sera utilizada para resetear el valor del parametro 
        public string ParametroValorDefaultValue { get; set; }

    }
}
