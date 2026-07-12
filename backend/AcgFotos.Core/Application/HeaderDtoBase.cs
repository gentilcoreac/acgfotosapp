namespace AcgFotos.Core.Application
{
    /// <summary>
    /// Base de DTOs de "header" / "summary" — el shape liviano que se devuelve en listados,
    /// grillas y selectores. Convención del proyecto:
    /// <list type="bullet">
    ///   <item>No incluir ICollection&lt;&gt; ni navegaciones pesadas (eso vive en el DTO completo).</item>
    ///   <item>Solo campos planos que la grilla/selector consume directo.</item>
    ///   <item>El DTO completo para detail/edit puede heredar de éste para no duplicar los campos básicos.</item>
    /// </list>
    /// </summary>
    public abstract class HeaderDtoBase : DtoBase
    {
    }
}
