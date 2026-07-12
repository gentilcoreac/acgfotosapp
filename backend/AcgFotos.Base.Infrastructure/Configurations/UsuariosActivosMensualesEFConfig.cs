using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class UsuariosActivosMensualesEFConfig : IEntityTypeConfiguration<UsuariosActivosMensual>
    {
        public void Configure(EntityTypeBuilder<UsuariosActivosMensual> builder)
        {
            builder.ToTable("gen_UsuariosActivosMensuales");
        }
    }
}
