using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class LogInfoEFConfig : IEntityTypeConfiguration<LogInfo>
    {
        public void Configure(EntityTypeBuilder<LogInfo> builder)
        {
            builder.ToTable("gen_LogInfos");
            builder.Property(e => e.Id).HasColumnType("bigint");
            builder.HasKey(e => e.Id);
        }
    }
}
