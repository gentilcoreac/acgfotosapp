using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Procesamiento;

/// <summary>
/// Resuelve qué perfil de marca de agua y qué opciones de publicación aplican a las fotos de un
/// evento, con la cascada de ADR-15 §4: evento → default del tenant → (ninguno, cae a
/// <c>OpcionesFotos</c>). Un solo lugar que responde "qué se aplica a esta foto", usado por el
/// procesamiento normal, la regeneración y la vista previa del editor (design.md D3).
/// </summary>
public interface IConfiguracionMarcaAguaResolver
{
    Task<(PerfilMarcaAgua? Perfil, OpcionesPublicacion? Opciones)> ResolverAsync(long eventoId);
}
