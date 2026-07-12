namespace AcgFotos.Fotos.Domain.Entities;

public enum EstadoEvento
{
    /// <summary>El fotógrafo lo está armando: las familias todavía no pueden entrar.</summary>
    Borrador = 0,

    /// <summary>Visible para las familias con código.</summary>
    Publicado = 1,

    /// <summary>Terminado: no se aceptan más pedidos.</summary>
    Cerrado = 2,
}
