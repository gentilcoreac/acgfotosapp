using AcgFotos.Core.Application;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>
/// Fila de capa dentro de un perfil. Se usa tanto en la salida (listado/detalle) como en el input de
/// edición: <see cref="PerfilMarcaAguaInputDto.Capas"/> sólo acepta filas con <see cref="DtoBase.Id"/>
/// existente — una capa nueva nace siempre con contenido real, subida por el endpoint dedicado
/// (design.md D14), nunca por acá.
/// </summary>
public class CapaMarcaAguaDto : DtoBase
{
    public int Orden { get; set; }
    public Guid StorageKey { get; set; }
    public int AnchoPx { get; set; }
    public int AltoPx { get; set; }
    public ModoColocacionMarcaAgua ModoColocacion { get; set; }
    public PosicionMarcaAgua? Posicion { get; set; }
    public float EscalaPorcentaje { get; set; }
    public float MargenPorcentaje { get; set; }

    /// <summary>Cada cuánto se repite la marca, en % del ancho de la foto. Sólo aplica en modo Repetida.</summary>
    public float SeparacionPorcentaje { get; set; }

    public float AnguloGrados { get; set; }
    public float Opacidad { get; set; }
    public ModoFusionMarcaAgua ModoFusion { get; set; }
}

/// <summary>Detalle/listado de un perfil de marca de agua (ADR-15). El listado también trae las capas: spec 7.2 pide renderizar la marca real por fila.</summary>
public class PerfilMarcaAguaDto : DtoBase
{
    public string Nombre { get; set; } = string.Empty;
    public bool EsDefault { get; set; }
    public bool MarcarThumb { get; set; }
    public List<CapaMarcaAguaDto> Capas { get; set; } = new();

    /// <summary>Avisos en términos de la consecuencia real (D12), p. ej. "las familias van a ver estas fotos sin ninguna protección".</summary>
    public List<string> Avisos { get; set; } = new();
}

/// <summary>
/// Input de alta/edición. Las capas llegan como filas existentes (Id ≠ 0): alta y edición de
/// contenido van por <c>SubirCapaAsync</c>, acá sólo se reconcilian colocación/orden y bajas.
/// </summary>
public class PerfilMarcaAguaInputDto : DtoBase
{
    public string Nombre { get; set; } = string.Empty;
    public bool EsDefault { get; set; }
    public bool MarcarThumb { get; set; } = true;
    public List<CapaMarcaAguaDto> Capas { get; set; } = new();
}

/// <summary>Respuesta de subir el PNG de una capa (design.md D14): el perfil completo, ya con la capa nueva persistida, más los avisos del asset recién subido.</summary>
public class CapaMarcaAguaSubidaDto
{
    public required PerfilMarcaAguaDto Perfil { get; init; }
    public required CapaMarcaAguaDto Capa { get; init; }
    public required List<string> Avisos { get; init; }
}

/// <summary>Conjunto de resolución/calidad de publicación (ADR-15 §5, capacidad `opciones-publicacion`), eje independiente del perfil de marca.</summary>
public class OpcionesPublicacionDto : DtoBase
{
    public string Nombre { get; set; } = string.Empty;
    public bool EsDefault { get; set; }
    public int LadoMayorPreview { get; set; }
    public int LadoMayorThumb { get; set; }
    public int Calidad { get; set; }
}
