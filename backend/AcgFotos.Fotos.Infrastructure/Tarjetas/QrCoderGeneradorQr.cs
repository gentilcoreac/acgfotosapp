using QRCoder;
using AcgFotos.Fotos.Application.Tarjetas;

namespace AcgFotos.Fotos.Infrastructure.Tarjetas;

public class QrCoderGeneradorQr : IGeneradorQr
{
    public byte[] GenerarPng(string contenido)
    {
        using var generator = new QRCodeGenerator();
        // ECC Q (25% de redundancia): la tarjeta impresa puede venir doblada o manchada.
        using var data = generator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule: 10);
    }
}
