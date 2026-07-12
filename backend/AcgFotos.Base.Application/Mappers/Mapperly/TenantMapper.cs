using System.Collections.Generic;
using Riok.Mapperly.Abstractions;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;

namespace AcgFotos.Base.Application.Mappers.Mapperly
{
    /// <summary>
    /// Mapeo de Tenant, 100% Mapperly (sin AutoMapper). Cubre solo el <b>read path</b>: listado
    /// (proyección/entidad → header), detalle del ABM (entidad → <see cref="TenantDto"/>), estilo
    /// (header-style / public-style) y el dto→dto que devuelve EditByAdminClient. El <b>write path</b>
    /// NO pasa por acá: va por <c>SetValues</c> (escalares, EF-nativo) + <c>ChildCollectionSync.SyncBy</c>
    /// (colecciones por clave de negocio), y el merge Input→Dto por asignación explícita en el AppService
    /// (ADR-0001). Las libs de mapping no entienden el ChangeTracker, por eso no se usan para persistir.
    ///
    /// <para><see cref="RequiredMappingStrategy.Target"/>: se exige completar cada miembro del DTO destino;
    /// los miembros del source no usados (navegaciones) no generan diagnósticos. Los targets que se pueblan
    /// fuera del mapper (archivos subidos, StyleSheetFile, Usuario del alta) y los campos Office que esta API
    /// podada ya no expone en la entidad se marcan con <see cref="MapperIgnoreTargetAttribute"/>.</para>
    /// </summary>
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class TenantMapper
    {
        // ---- Detalle del ABM (GetByIdAsync y retorno de los saves) ----------------------------------
        // Los *File / StyleSheetFile / Usuario no vienen de la entidad: los puebla el AppService (uploads
        // y alta de usuario admin). TenantAplicaciones/TenantLicenses se aplanan vía los sub-mappers.
        [MapperIgnoreTarget(nameof(TenantDto.LogoLoginLightFile))]
        [MapperIgnoreTarget(nameof(TenantDto.LogoLoginDarkFile))]
        [MapperIgnoreTarget(nameof(TenantDto.LogoHeaderLightFile))]
        [MapperIgnoreTarget(nameof(TenantDto.LogoHeaderDarkFile))]
        [MapperIgnoreTarget(nameof(TenantDto.FaviconFile))]
        [MapperIgnoreTarget(nameof(TenantDto.ImagenFondoLoginLightFile))]
        [MapperIgnoreTarget(nameof(TenantDto.ImagenFondoLoginDarkFile))]
        [MapperIgnoreTarget(nameof(TenantDto.StyleSheetFile))]
        [MapperIgnoreTarget(nameof(TenantDto.Usuario))]
        public partial TenantDto ToDto(Tenant entity);

        // ---- Estilo (header-style) ------------------------------------------------------------------
        public partial TenantHeaderStyleDto ToHeaderStyle(Tenant entity);

        // ---- DTO completo → header-style (retorno de EditByAdminClient) -----------------------------
        public partial TenantHeaderStyleDto ToHeaderStyle(TenantDto dto);

        // ---- Listado (Search → proyección SQL → header) ---------------------------------------------
        public partial TenantHeaderDto ToHeaderDto(TenantHeaderProjection projection);

        // ---- Listado para root (entidad → header) ---------------------------------------------------
        public partial TenantHeaderDto ToHeaderDto(Tenant entity);

        public partial List<TenantHeaderDto> ToHeaderDtos(IReadOnlyList<Tenant> entities);

        // ---- Estilo público (login anónimo). Los campos Office no existen en esta API → quedan default.
        [MapperIgnoreTarget(nameof(GetTenantPublicStyleOutput.ColorHeaderSheet))]
        [MapperIgnoreTarget(nameof(GetTenantPublicStyleOutput.ColorFontHeaderSheet))]
        [MapperIgnoreTarget(nameof(GetTenantPublicStyleOutput.LogoHeaderSheetUrl))]
        [MapperIgnoreTarget(nameof(GetTenantPublicStyleOutput.ColorHeaderTable))]
        [MapperIgnoreTarget(nameof(GetTenantPublicStyleOutput.ColorHeaderFontTable))]
        public partial GetTenantPublicStyleOutput ToPublicStyle(Tenant entity);

        // ---- Sub-mappers de colección ---------------------------------------------------------------
        // AplicacionNombre se aplana desde la navegación (null-safe si no vino incluida).
        [MapProperty(new[] { nameof(TenantAplicacion.Aplicacion), nameof(Aplicacion.Nombre) }, new[] { nameof(TenantAplicacionDto.AplicacionNombre) })]
        private partial TenantAplicacionDto ToDto(TenantAplicacion entity);

        // La entidad usa "...Datetime" y el DTO "...DateTime" (casing distinto): Mapperly matchea
        // case-sensitive por defecto, así que se mapean explícito. Cantidad int→long es widening implícito.
        [MapProperty(nameof(TenantLicencia.ModifiedDatetime), nameof(TenantLicenciaDto.ModifiedDateTime))]
        [MapProperty(nameof(TenantLicencia.StartDatetime), nameof(TenantLicenciaDto.StartDateTime))]
        [MapProperty(nameof(TenantLicencia.ExpireDatetime), nameof(TenantLicenciaDto.ExpireDateTime))]
        private partial TenantLicenciaDto ToDto(TenantLicencia entity);
    }
}
