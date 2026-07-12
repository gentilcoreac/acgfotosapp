using System.Security.Cryptography;

namespace AcgFotos.Fotos.Domain.Services;

/// <summary>
/// Genera los códigos que las familias canjean para entrar a su álbum (ADR-02): cortos y legibles
/// porque se reparten impresos en una tarjeta, pero con entropía suficiente (31^8 ≈ 2^40 dentro
/// del tenant; el canje además se rate-limitea en Fase 2). La unicidad la garantiza el índice
/// único (TenantId, Codigo): con este espacio la colisión es despreciable y, si ocurriera, el
/// commit falla y el fotógrafo simplemente reintenta el alta.
/// </summary>
public static class GeneradorCodigoAcceso
{
    /// <summary>Sin 0/O, 1/I/L: el código se lee en voz alta o se tipea desde una tarjeta.</summary>
    private const string Alfabeto = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    /// <summary>Devuelve un código con formato <c>XXXX-XXXX</c>, ej. "K7F3-9QMD".</summary>
    public static string Generar()
    {
        var chars = RandomNumberGenerator.GetItems<char>(Alfabeto, 8);
        return $"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}";
    }
}
