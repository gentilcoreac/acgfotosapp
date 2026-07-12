namespace AcgFotos.Fotos.Application.Tarjetas;

/// <summary>Puerto de generación de QR (la implementación QRCoder vive en Infrastructure).</summary>
public interface IGeneradorQr
{
    /// <summary>PNG del QR que codifica <paramref name="contenido"/> (típicamente la URL de canje).</summary>
    byte[] GenerarPng(string contenido);
}
