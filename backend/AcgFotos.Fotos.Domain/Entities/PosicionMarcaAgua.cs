namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>Grilla de 9 posiciones fijas. Sólo aplica cuando <see cref="CapaMarcaAgua.ModoColocacion"/> es <see cref="ModoColocacionMarcaAgua.PosicionFija"/>.</summary>
public enum PosicionMarcaAgua
{
    ArribaIzquierda = 0,
    ArribaCentro = 1,
    ArribaDerecha = 2,
    CentroIzquierda = 3,
    Centro = 4,
    CentroDerecha = 5,
    AbajoIzquierda = 6,
    AbajoCentro = 7,
    AbajoDerecha = 8,
}
