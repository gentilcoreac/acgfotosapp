using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    /// <summary>
    /// Mapea <see cref="UsuarioRolEfectivo"/> a la vista <c>vw_UsuarioRolesEfectivos</c> como entidad
    /// keyless de solo lectura. <c>ToView</c> excluye la entidad de las migraciones (la vista la
    /// crea/borra el SQL de la migración <c>AddVwUsuarioRolesEfectivos</c>), y al no tener clave EF
    /// nunca la trackea.
    /// </summary>
    public class UsuarioRolEfectivoEFConfig : IEntityTypeConfiguration<UsuarioRolEfectivo>
    {
        public void Configure(EntityTypeBuilder<UsuarioRolEfectivo> builder)
        {
            builder.HasNoKey();
            builder.ToView("vw_UsuarioRolesEfectivos");
        }
    }
}
