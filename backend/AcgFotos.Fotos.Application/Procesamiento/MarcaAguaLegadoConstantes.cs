namespace AcgFotos.Fotos.Application.Procesamiento;

/// <summary>
/// Parámetros de colocación del watermark de texto pre-ADR-15, congelados junto con
/// <c>Imaging/Assets/marca-agua-default.png</c> (D13, design.md). Los usa tanto el último escalón de
/// la cascada (sin ningún perfil cargado, <c>FotoProcesadorAppService</c>) como el seed del perfil
/// "Estándar" (D11, <c>PerfilMarcaAguaAppService</c>) — mismo origen, un solo lugar con los números.
/// </summary>
public static class MarcaAguaLegadoConstantes
{
    // Ángulo: -0.4636 rad (diagonal clásica de proofing) en grados.
    public const float AnguloGrados = -26.565f;

    // El asset se rasterizó contra una foto de referencia de 1600px de ancho; 1516 es el ancho real
    // del tile resultante (SixLabors.Fonts, tamaño = ancho/20 = 80px, código pre-ADR-15).
    public const float EscalaPorcentaje = 1516f / 1600f * 100f;

    public const float Opacidad = 0.5f;

    /// <summary>
    /// Separación equivalente a la densidad pre-ADR-15, cuando el paso era 1.25× el ancho del tile en
    /// vez de un % de la foto. Como el tile ocupaba <see cref="EscalaPorcentaje"/> del ancho, la
    /// separación sobre la foto que reproduce ese paso es 1.25 veces esa escala.
    /// </summary>
    public const float SeparacionPorcentaje = EscalaPorcentaje * 1.25f;
}
