namespace AcgFotos.Base.Application.Outputs
{
    /// <summary>
    /// Actividad de usuarios de un mes para el gráfico del homepage: <see cref="Activos"/> (usuarios
    /// distintos que iniciaron sesión, de UsuarioHistorial) y <see cref="Altas"/> (usuarios creados,
    /// por DateCreated). Una entrada por mes de la ventana pedida, con ceros para los meses sin datos.
    /// </summary>
    public class UsuariosActividadMensualOutput
    {
        public int Anio { get; set; }

        public int Mes { get; set; }

        public int Activos { get; set; }

        public int Altas { get; set; }
    }
}
