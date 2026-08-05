using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Una capa de un <see cref="PerfilMarcaAgua"/>: un PNG con transparencia ya rasterizado por el
/// front (logo subido o texto diseñado, ADR-15 §2 — el mismo mecanismo para ambos) más su
/// colocación. La API nunca dibuja: coloca, escala (sólo hacia abajo), rota y funde este bitmap.
/// </summary>
public class CapaMarcaAgua : MultiTenantEntityBase
{
    public long PerfilMarcaAguaId { get; set; }
    public PerfilMarcaAgua PerfilMarcaAgua { get; set; } = null!;

    /// <summary>Orden de composición dentro del perfil (p. ej. logo primero, trama de texto encima).</summary>
    public int Orden { get; set; }

    /// <summary>Identificador no adivinable con el que se arma la key de storage del asset PNG.</summary>
    public Guid StorageKey { get; set; }

    public int AnchoPx { get; set; }
    public int AltoPx { get; set; }

    public ModoColocacionMarcaAgua ModoColocacion { get; set; }

    /// <summary>Sólo aplica con <see cref="ModoColocacionMarcaAgua.PosicionFija"/>.</summary>
    public PosicionMarcaAgua? Posicion { get; set; }

    /// <summary>Escala del asset en % del ancho de la foto. La composición sólo escala hacia abajo (ADR-15 §8).</summary>
    public float EscalaPorcentaje { get; set; }

    /// <summary>Margen respecto al borde, en % del ancho de la foto. Sólo aplica con posición fija.</summary>
    public float MargenPorcentaje { get; set; }

    public float AnguloGrados { get; set; }

    /// <summary>0-1.</summary>
    public float Opacidad { get; set; } = 1f;

    public ModoFusionMarcaAgua ModoFusion { get; set; }
}
