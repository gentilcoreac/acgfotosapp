using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcgFotos.Base.SqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gen_Aplicaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Icono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IconoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_Aplicaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gen_AuthzVersion",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_AuthzVersion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gen_EndPoints",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ControllerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Namespace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Route = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_EndPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gen_Grupos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_Grupos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gen_LogInfos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_LogInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gen_RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RevokeReason = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedByUserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gen_Roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EsDefaultParaNuevoTenant = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gen_Tenants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TituloWeb = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    ColorPrimarioDark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColorPrimarioLight = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DarkModeByDefault = table.Column<bool>(type: "bit", nullable: false),
                    LogoLoginLightUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoLoginDarkUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoHeaderLightUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoHeaderDarkUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaviconUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImagenFondoLoginLightUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImagenFondoLoginDarkUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StyleSheetCssUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoLayoutLogin = table.Column<int>(type: "int", nullable: false),
                    HostName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasError = table.Column<bool>(type: "bit", nullable: true),
                    ErrorDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gen_TipoLicencia",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CodigoTipoLicencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EsDefaultParaNuevoTenant = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_TipoLicencia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gen_UsuariosActivosMensuales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Periodo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_UsuariosActivosMensuales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gen_UsuariosHistorial",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FechaLastLogin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_UsuariosHistorial", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_Permisos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CodigoPermiso = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    PermisoPadreId = table.Column<long>(type: "bigint", nullable: true),
                    AplicacionId = table.Column<long>(type: "bigint", nullable: true),
                    EsRestringido = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_Permisos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_Permisos_gen_Aplicaciones_AplicacionId",
                        column: x => x.AplicacionId,
                        principalTable: "gen_Aplicaciones",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_gen_Permisos_gen_Permisos_PermisoPadreId",
                        column: x => x.PermisoPadreId,
                        principalTable: "gen_Permisos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "gen_GrupoRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GrupoId = table.Column<long>(type: "bigint", nullable: false),
                    RolId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_GrupoRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_GrupoRoles_gen_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "gen_Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gen_GrupoRoles_gen_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "gen_Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_TenantAplicaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AplicacionId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_TenantAplicaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_TenantAplicaciones_gen_Aplicaciones_AplicacionId",
                        column: x => x.AplicacionId,
                        principalTable: "gen_Aplicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gen_TenantAplicaciones_gen_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "gen_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_TenantFiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_TenantFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_TenantFiles_gen_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "gen_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_Usuarios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<long>(type: "bigint", nullable: true),
                    ProfilePicture = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Administrador = table.Column<bool>(type: "bit", nullable: false),
                    EmailConfirmationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCambioClave = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_Usuarios_gen_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "gen_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gen_TenantLicencias",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    ModifiedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpireDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoLicenciaId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_TenantLicencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_TenantLicencias_gen_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "gen_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gen_TenantLicencias_gen_TipoLicencia_TipoLicenciaId",
                        column: x => x.TipoLicenciaId,
                        principalTable: "gen_TipoLicencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_TipoLicenciaRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RolId = table.Column<long>(type: "bigint", nullable: false),
                    TipoLicenciaId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_TipoLicenciaRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_TipoLicenciaRoles_gen_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "gen_Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gen_TipoLicenciaRoles_gen_TipoLicencia_TipoLicenciaId",
                        column: x => x.TipoLicenciaId,
                        principalTable: "gen_TipoLicencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_Menus",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    ImagenWeb = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    MenuPadreId = table.Column<long>(type: "bigint", nullable: true),
                    PermisoId = table.Column<long>(type: "bigint", nullable: true),
                    AplicacionId = table.Column<long>(type: "bigint", nullable: true),
                    VisibleSideMenu = table.Column<bool>(type: "bit", nullable: false),
                    VisibleDash = table.Column<bool>(type: "bit", nullable: false),
                    RoutePath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_Menus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_Menus_gen_Aplicaciones_AplicacionId",
                        column: x => x.AplicacionId,
                        principalTable: "gen_Aplicaciones",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_gen_Menus_gen_Menus_MenuPadreId",
                        column: x => x.MenuPadreId,
                        principalTable: "gen_Menus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_gen_Menus_gen_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "gen_Permisos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "gen_Parametros",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AplicacionId = table.Column<long>(type: "bigint", nullable: false),
                    TipoDato = table.Column<int>(type: "int", nullable: false),
                    PermisoId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_Parametros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_Parametros_gen_Aplicaciones_AplicacionId",
                        column: x => x.AplicacionId,
                        principalTable: "gen_Aplicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gen_Parametros_gen_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "gen_Permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gen_PermisoEndpoints",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermisoId = table.Column<long>(type: "bigint", nullable: false),
                    EndpointId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_PermisoEndpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_PermisoEndpoints_gen_EndPoints_EndpointId",
                        column: x => x.EndpointId,
                        principalTable: "gen_EndPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gen_PermisoEndpoints_gen_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "gen_Permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_RolPermisos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RolId = table.Column<long>(type: "bigint", nullable: false),
                    PermisoId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_RolPermisos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_RolPermisos_gen_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "gen_Permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gen_RolPermisos_gen_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "gen_Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_gen_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "gen_Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_gen_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "gen_Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_gen_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "gen_Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_gen_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "gen_Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duracion = table.Column<double>(type: "float", nullable: false),
                    Servicio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Metodo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Parametros = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    ImpersonatedBy = table.Column<long>(type: "bigint", nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RequestAbsolutePath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ClientIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClientUserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResultStatusCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ResultContent = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_AuditLogs_gen_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "gen_Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "gen_UsuarioAplicaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    AplicacionId = table.Column<long>(type: "bigint", nullable: false),
                    Default = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_UsuarioAplicaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_UsuarioAplicaciones_gen_Aplicaciones_AplicacionId",
                        column: x => x.AplicacionId,
                        principalTable: "gen_Aplicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gen_UsuarioAplicaciones_gen_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "gen_Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_UsuarioGrupos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    GrupoId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_UsuarioGrupos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_UsuarioGrupos_gen_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "gen_Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gen_UsuarioGrupos_gen_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "gen_Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_UsuarioRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    RolId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_UsuarioRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_UsuarioRoles_gen_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "gen_Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gen_UsuarioRoles_gen_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "gen_Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_UsuarioTipoLicencia",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    TipoLicenciaId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_UsuarioTipoLicencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_UsuarioTipoLicencia_gen_TipoLicencia_TipoLicenciaId",
                        column: x => x.TipoLicenciaId,
                        principalTable: "gen_TipoLicencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gen_UsuarioTipoLicencia_gen_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "gen_Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_ParametrosValoresTenants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ParametroId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_ParametrosValoresTenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gen_ParametrosValoresTenants_gen_Parametros_ParametroId",
                        column: x => x.ParametroId,
                        principalTable: "gen_Parametros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "gen_AuthzVersion",
                columns: new[] { "Id", "Version" },
                values: new object[] { 1L, 0L });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "ix_auditlog_fechahora",
                table: "gen_AuditLogs",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_gen_AuditLogs_UsuarioId",
                table: "gen_AuditLogs",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "uk_endpoint_route_method",
                table: "gen_EndPoints",
                columns: new[] { "Route", "HttpMethod" },
                unique: true,
                filter: "[Route] IS NOT NULL AND [HttpMethod] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_gen_GrupoRoles_GrupoId",
                table: "gen_GrupoRoles",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_GrupoRoles_RolId",
                table: "gen_GrupoRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "uk_tenant_grupo_rol",
                table: "gen_GrupoRoles",
                columns: new[] { "TenantId", "GrupoId", "RolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_grupo_tenant",
                table: "gen_Grupos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_Menus_AplicacionId",
                table: "gen_Menus",
                column: "AplicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_Menus_MenuPadreId",
                table: "gen_Menus",
                column: "MenuPadreId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_Menus_PermisoId",
                table: "gen_Menus",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "uk_menu_codigo",
                table: "gen_Menus",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gen_Parametros_AplicacionId",
                table: "gen_Parametros",
                column: "AplicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_Parametros_PermisoId",
                table: "gen_Parametros",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_ParametrosValoresTenants_ParametroId",
                table: "gen_ParametrosValoresTenants",
                column: "ParametroId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_ParametrosValoresTenants_TenantId_ParametroId",
                table: "gen_ParametrosValoresTenants",
                columns: new[] { "TenantId", "ParametroId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gen_PermisoEndpoints_EndpointId",
                table: "gen_PermisoEndpoints",
                column: "EndpointId");

            migrationBuilder.CreateIndex(
                name: "uk_permisos_endpoints",
                table: "gen_PermisoEndpoints",
                columns: new[] { "PermisoId", "EndpointId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gen_Permisos_AplicacionId",
                table: "gen_Permisos",
                column: "AplicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_Permisos_PermisoPadreId",
                table: "gen_Permisos",
                column: "PermisoPadreId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_RefreshTokens_TokenHash",
                table: "gen_RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gen_RefreshTokens_UserId_RevokedAt",
                table: "gen_RefreshTokens",
                columns: new[] { "UserId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_gen_RolPermisos_PermisoId",
                table: "gen_RolPermisos",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_RolPermisos_RolId",
                table: "gen_RolPermisos",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_TenantAplicaciones_AplicacionId",
                table: "gen_TenantAplicaciones",
                column: "AplicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_TenantAplicaciones_TenantId",
                table: "gen_TenantAplicaciones",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_TenantFiles_TenantId",
                table: "gen_TenantFiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_TenantLicencias_TenantId",
                table: "gen_TenantLicencias",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_TenantLicencias_TipoLicenciaId",
                table: "gen_TenantLicencias",
                column: "TipoLicenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_Tenants_Codigo",
                table: "gen_Tenants",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gen_TipoLicenciaRoles_RolId",
                table: "gen_TipoLicenciaRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_TipoLicenciaRoles_TipoLicenciaId",
                table: "gen_TipoLicenciaRoles",
                column: "TipoLicenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_UsuarioAplicaciones_AplicacionId",
                table: "gen_UsuarioAplicaciones",
                column: "AplicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_UsuarioAplicaciones_UsuarioId",
                table: "gen_UsuarioAplicaciones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_UsuarioGrupos_GrupoId",
                table: "gen_UsuarioGrupos",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "ix_usuariogrupo_usuario",
                table: "gen_UsuarioGrupos",
                column: "UsuarioId")
                .Annotation("SqlServer:Include", new[] { "GrupoId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "uk_tenant_grupo_usuario",
                table: "gen_UsuarioGrupos",
                columns: new[] { "TenantId", "GrupoId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gen_UsuarioRoles_RolId",
                table: "gen_UsuarioRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "uk_usuario_rol",
                table: "gen_UsuarioRoles",
                columns: new[] { "UsuarioId", "RolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "gen_Usuarios",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_gen_Usuarios_TenantId",
                table: "gen_Usuarios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_Usuarios_UserName",
                table: "gen_Usuarios",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "gen_Usuarios",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_gen_UsuariosHistorial_TenantId_Periodo",
                table: "gen_UsuariosHistorial",
                columns: new[] { "TenantId", "Periodo" });

            migrationBuilder.CreateIndex(
                name: "IX_gen_UsuarioTipoLicencia_TipoLicenciaId",
                table: "gen_UsuarioTipoLicencia",
                column: "TipoLicenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_gen_UsuarioTipoLicencia_UsuarioId",
                table: "gen_UsuarioTipoLicencia",
                column: "UsuarioId");

            // La vista vw_UsuarioRolesEfectivos la consume la entidad mapeada con ToView (menú + authz).
            // Es SQL crudo: no sale del snapshot del modelo al regenerar migraciones, hay que mantenerla a
            // mano acá (portada de la migración AddVwUsuarioRolesEfectivos del blueprint).
            // Fuente única de los roles efectivos (directos ∪ de grupos) + flag PermitidoPorLicencia
            // (EXISTS contra la licencia ACTIVA, tope duro para no-root).
            migrationBuilder.Sql(@"
CREATE VIEW dbo.vw_UsuarioRolesEfectivos
AS
    WITH efectivos AS (
        SELECT ur.UsuarioId, ur.TenantId, ur.RolId,
               CAST('Directo' AS varchar(10)) AS Origen, CAST(NULL AS bigint) AS GrupoId
        FROM dbo.gen_UsuarioRoles ur
        UNION ALL
        SELECT ug.UsuarioId, ug.TenantId, gr.RolId,
               CAST('Grupo' AS varchar(10)) AS Origen, ug.GrupoId
        FROM dbo.gen_UsuarioGrupos ug
        INNER JOIN dbo.gen_GrupoRoles gr
            ON gr.TenantId = ug.TenantId AND gr.GrupoId = ug.GrupoId
    )
    SELECT
        e.UsuarioId, e.TenantId, e.RolId, e.Origen, e.GrupoId,
        CAST(CASE WHEN EXISTS (
            SELECT 1
            FROM dbo.gen_UsuarioTipoLicencia ul
            INNER JOIN dbo.gen_TipoLicenciaRoles tlr ON tlr.TipoLicenciaId = ul.TipoLicenciaId
            WHERE ul.UsuarioId = e.UsuarioId AND ul.IsActive = 1 AND tlr.RolId = e.RolId
        ) THEN 1 ELSE 0 END AS bit) AS PermitidoPorLicencia
    FROM efectivos e;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_UsuarioRolesEfectivos;");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "gen_AuditLogs");

            migrationBuilder.DropTable(
                name: "gen_AuthzVersion");

            migrationBuilder.DropTable(
                name: "gen_GrupoRoles");

            migrationBuilder.DropTable(
                name: "gen_LogInfos");

            migrationBuilder.DropTable(
                name: "gen_Menus");

            migrationBuilder.DropTable(
                name: "gen_ParametrosValoresTenants");

            migrationBuilder.DropTable(
                name: "gen_PermisoEndpoints");

            migrationBuilder.DropTable(
                name: "gen_RefreshTokens");

            migrationBuilder.DropTable(
                name: "gen_RolPermisos");

            migrationBuilder.DropTable(
                name: "gen_TenantAplicaciones");

            migrationBuilder.DropTable(
                name: "gen_TenantFiles");

            migrationBuilder.DropTable(
                name: "gen_TenantLicencias");

            migrationBuilder.DropTable(
                name: "gen_TipoLicenciaRoles");

            migrationBuilder.DropTable(
                name: "gen_UsuarioAplicaciones");

            migrationBuilder.DropTable(
                name: "gen_UsuarioGrupos");

            migrationBuilder.DropTable(
                name: "gen_UsuarioRoles");

            migrationBuilder.DropTable(
                name: "gen_UsuariosActivosMensuales");

            migrationBuilder.DropTable(
                name: "gen_UsuariosHistorial");

            migrationBuilder.DropTable(
                name: "gen_UsuarioTipoLicencia");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "gen_Parametros");

            migrationBuilder.DropTable(
                name: "gen_EndPoints");

            migrationBuilder.DropTable(
                name: "gen_Grupos");

            migrationBuilder.DropTable(
                name: "gen_Roles");

            migrationBuilder.DropTable(
                name: "gen_TipoLicencia");

            migrationBuilder.DropTable(
                name: "gen_Usuarios");

            migrationBuilder.DropTable(
                name: "gen_Permisos");

            migrationBuilder.DropTable(
                name: "gen_Tenants");

            migrationBuilder.DropTable(
                name: "gen_Aplicaciones");
        }
    }
}
