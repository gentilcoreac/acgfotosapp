namespace AcgFotos.Fotos.Domain.Entities;

public enum ModoColocacionMarcaAgua
{
    /// <summary>Repetida en mosaico sobre toda la foto (el modo actual, más difícil de recortar).</summary>
    Repetida = 0,

    /// <summary>Una sola vez, en <see cref="CapaMarcaAgua.Posicion"/>.</summary>
    PosicionFija = 1,
}
