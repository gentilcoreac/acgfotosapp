namespace AcgFotos.Fotos.Application.Familias;

/// <summary>Token de sesión emitido al canjear un código (ADR-11): sin usuario de plataforma.</summary>
public class FamiliaTokenModel
{
    public string Token { get; set; } = null!;
    public DateTime ValidTo { get; set; }
}

public interface IFamiliaTokenFactory
{
    /// <summary>
    /// Emite el JWT de sesión de familia. <paramref name="participanteIds"/> ya viene listo para
    /// llevar más de un participante (hermanos / persona en dos grupos, docs/05) aunque el canje
    /// de hoy solo emita uno.
    /// </summary>
    FamiliaTokenModel Crear(long tenantId, long eventoId, IReadOnlyCollection<long> participanteIds);
}
