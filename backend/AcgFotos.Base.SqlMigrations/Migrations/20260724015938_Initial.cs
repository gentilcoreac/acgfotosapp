using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "aspnetroles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalizedname = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrencystamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aspnetroles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fot_eventos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    lugarorganizacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fechaexpiracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fot_eventos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_aplicaciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    icono = table.Column<string>(type: "text", nullable: true),
                    iconourl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_aplicaciones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_authzversion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_authzversion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_endpoints",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    actionname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    controllername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    modulename = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    @namespace = table.Column<string>(name: "namespace", type: "text", nullable: true),
                    route = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    httpmethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_endpoints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_grupos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_grupos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_loginfos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message = table.Column<string>(type: "text", nullable: true),
                    messagetemplate = table.Column<string>(type: "text", nullable: true),
                    level = table.Column<string>(type: "text", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    exception = table.Column<string>(type: "text", nullable: true),
                    properties = table.Column<string>(type: "text", nullable: true),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_loginfos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_refreshtokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    tokenhash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiresat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revokedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replacedbytokenhash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    revokereason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    createdbyip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    createdbyuseragent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_refreshtokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    esdefaultparanuevotenant = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_tenants",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tituloweb = table.Column<string>(type: "text", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    colorprimariodark = table.Column<string>(type: "text", nullable: true),
                    colorprimariolight = table.Column<string>(type: "text", nullable: true),
                    darkmodebydefault = table.Column<bool>(type: "boolean", nullable: false),
                    logologinlighturl = table.Column<string>(type: "text", nullable: true),
                    logologindarkurl = table.Column<string>(type: "text", nullable: true),
                    logoheaderlighturl = table.Column<string>(type: "text", nullable: true),
                    logoheaderdarkurl = table.Column<string>(type: "text", nullable: true),
                    faviconurl = table.Column<string>(type: "text", nullable: true),
                    imagenfondologinlighturl = table.Column<string>(type: "text", nullable: true),
                    imagenfondologindarkurl = table.Column<string>(type: "text", nullable: true),
                    stylesheetcssurl = table.Column<string>(type: "text", nullable: true),
                    tipolayoutlogin = table.Column<int>(type: "integer", nullable: false),
                    hostname = table.Column<string>(type: "text", nullable: true),
                    haserror = table.Column<bool>(type: "boolean", nullable: true),
                    errordescription = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_tipolicencia",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    codigotipolicencia = table.Column<string>(type: "text", nullable: true),
                    esdefaultparanuevotenant = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_tipolicencia", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_usuariosactivosmensuales",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    periodo = table.Column<string>(type: "text", nullable: true),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_usuariosactivosmensuales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_usuarioshistorial",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuarioid = table.Column<long>(type: "bigint", nullable: false),
                    periodo = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    fechalastlogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_usuarioshistorial", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "aspnetroleclaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    roleid = table.Column<long>(type: "bigint", nullable: false),
                    claimtype = table.Column<string>(type: "text", nullable: true),
                    claimvalue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aspnetroleclaims", x => x.id);
                    table.ForeignKey(
                        name: "fk_aspnetroleclaims_aspnetroles_roleid",
                        column: x => x.roleid,
                        principalTable: "aspnetroles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fot_grupos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    eventoid = table.Column<long>(type: "bigint", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fot_grupos", x => x.id);
                    table.ForeignKey(
                        name: "fk_fot_grupos_fot_eventos_eventoid",
                        column: x => x.eventoid,
                        principalTable: "fot_eventos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fot_tamanosprecios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    eventoid = table.Column<long>(type: "bigint", nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    preciounitario = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fot_tamanosprecios", x => x.id);
                    table.ForeignKey(
                        name: "fk_fot_tamanosprecios_fot_eventos_eventoid",
                        column: x => x.eventoid,
                        principalTable: "fot_eventos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_permisos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    codigopermiso = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    permisopadreid = table.Column<long>(type: "bigint", nullable: true),
                    aplicacionid = table.Column<long>(type: "bigint", nullable: true),
                    esrestringido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_permisos", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_permisos_gen_aplicaciones_aplicacionid",
                        column: x => x.aplicacionid,
                        principalTable: "gen_aplicaciones",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_gen_permisos_gen_permisos_permisopadreid",
                        column: x => x.permisopadreid,
                        principalTable: "gen_permisos",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "gen_gruporoles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grupoid = table.Column<long>(type: "bigint", nullable: false),
                    rolid = table.Column<long>(type: "bigint", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_gruporoles", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_gruporoles_gen_grupos_grupoid",
                        column: x => x.grupoid,
                        principalTable: "gen_grupos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_gen_gruporoles_gen_roles_rolid",
                        column: x => x.rolid,
                        principalTable: "gen_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_tenantaplicaciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    aplicacionid = table.Column<long>(type: "bigint", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_tenantaplicaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_tenantaplicaciones_gen_aplicaciones_aplicacionid",
                        column: x => x.aplicacionid,
                        principalTable: "gen_aplicaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_gen_tenantaplicaciones_gen_tenants_tenantid",
                        column: x => x.tenantid,
                        principalTable: "gen_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_tenantfiles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    filename = table.Column<string>(type: "text", nullable: false),
                    contenttype = table.Column<string>(type: "text", nullable: true),
                    length = table.Column<long>(type: "bigint", nullable: false),
                    storagekey = table.Column<string>(type: "text", nullable: false),
                    visibility = table.Column<int>(type: "integer", nullable: false),
                    createdbyuserid = table.Column<long>(type: "bigint", nullable: true),
                    createdatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_tenantfiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_tenantfiles_gen_tenants_tenantid",
                        column: x => x.tenantid,
                        principalTable: "gen_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_usuarios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenantid = table.Column<long>(type: "bigint", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    apellido = table.Column<string>(type: "text", nullable: true),
                    telefono = table.Column<long>(type: "bigint", nullable: true),
                    profilepicture = table.Column<byte[]>(type: "bytea", nullable: true),
                    administrador = table.Column<bool>(type: "boolean", nullable: false),
                    emailconfirmationtoken = table.Column<string>(type: "text", nullable: true),
                    tokenexpirationdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    datecreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fechacambioclave = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    normalizedusername = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalizedemail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    emailconfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    passwordhash = table.Column<string>(type: "text", nullable: true),
                    securitystamp = table.Column<string>(type: "text", nullable: true),
                    concurrencystamp = table.Column<string>(type: "text", nullable: true),
                    phonenumber = table.Column<string>(type: "text", nullable: true),
                    phonenumberconfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    twofactorenabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockoutend = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockoutenabled = table.Column<bool>(type: "boolean", nullable: false),
                    accessfailedcount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_usuarios_gen_tenants_tenantid",
                        column: x => x.tenantid,
                        principalTable: "gen_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gen_tenantlicencias",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    modifieddatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    startdatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiredatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipolicenciaid = table.Column<long>(type: "bigint", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_tenantlicencias", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_tenantlicencias_gen_tenants_tenantid",
                        column: x => x.tenantid,
                        principalTable: "gen_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_gen_tenantlicencias_gen_tipolicencia_tipolicenciaid",
                        column: x => x.tipolicenciaid,
                        principalTable: "gen_tipolicencia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_tipolicenciaroles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rolid = table.Column<long>(type: "bigint", nullable: false),
                    tipolicenciaid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_tipolicenciaroles", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_tipolicenciaroles_gen_roles_rolid",
                        column: x => x.rolid,
                        principalTable: "gen_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_gen_tipolicenciaroles_gen_tipolicencia_tipolicenciaid",
                        column: x => x.tipolicenciaid,
                        principalTable: "gen_tipolicencia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fot_participantes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grupoid = table.Column<long>(type: "bigint", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fot_participantes", x => x.id);
                    table.ForeignKey(
                        name: "fk_fot_participantes_fot_grupos_grupoid",
                        column: x => x.grupoid,
                        principalTable: "fot_grupos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_menus",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    estado = table.Column<bool>(type: "boolean", nullable: false),
                    imagenweb = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    menupadreid = table.Column<long>(type: "bigint", nullable: true),
                    permisoid = table.Column<long>(type: "bigint", nullable: true),
                    aplicacionid = table.Column<long>(type: "bigint", nullable: true),
                    visiblesidemenu = table.Column<bool>(type: "boolean", nullable: false),
                    visibledash = table.Column<bool>(type: "boolean", nullable: false),
                    routepath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_menus", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_menus_gen_aplicaciones_aplicacionid",
                        column: x => x.aplicacionid,
                        principalTable: "gen_aplicaciones",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_gen_menus_gen_menus_menupadreid",
                        column: x => x.menupadreid,
                        principalTable: "gen_menus",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_gen_menus_gen_permisos_permisoid",
                        column: x => x.permisoid,
                        principalTable: "gen_permisos",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "gen_parametros",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    valor = table.Column<string>(type: "text", nullable: true),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    aplicacionid = table.Column<long>(type: "bigint", nullable: false),
                    tipodato = table.Column<int>(type: "integer", nullable: false),
                    permisoid = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_parametros", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_parametros_gen_aplicaciones_aplicacionid",
                        column: x => x.aplicacionid,
                        principalTable: "gen_aplicaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gen_parametros_gen_permisos_permisoid",
                        column: x => x.permisoid,
                        principalTable: "gen_permisos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gen_permisoendpoints",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    permisoid = table.Column<long>(type: "bigint", nullable: false),
                    endpointid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_permisoendpoints", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_permisoendpoints_gen_endpoints_endpointid",
                        column: x => x.endpointid,
                        principalTable: "gen_endpoints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_gen_permisoendpoints_gen_permisos_permisoid",
                        column: x => x.permisoid,
                        principalTable: "gen_permisos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_rolpermisos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rolid = table.Column<long>(type: "bigint", nullable: false),
                    permisoid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_rolpermisos", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_rolpermisos_gen_permisos_permisoid",
                        column: x => x.permisoid,
                        principalTable: "gen_permisos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_gen_rolpermisos_gen_roles_rolid",
                        column: x => x.rolid,
                        principalTable: "gen_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aspnetuserclaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    claimtype = table.Column<string>(type: "text", nullable: true),
                    claimvalue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aspnetuserclaims", x => x.id);
                    table.ForeignKey(
                        name: "fk_aspnetuserclaims_gen_usuarios_userid",
                        column: x => x.userid,
                        principalTable: "gen_usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aspnetuserlogins",
                columns: table => new
                {
                    loginprovider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    providerkey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    providerdisplayname = table.Column<string>(type: "text", nullable: true),
                    userid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aspnetuserlogins", x => new { x.loginprovider, x.providerkey });
                    table.ForeignKey(
                        name: "fk_aspnetuserlogins_gen_usuarios_userid",
                        column: x => x.userid,
                        principalTable: "gen_usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aspnetuserroles",
                columns: table => new
                {
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    roleid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aspnetuserroles", x => new { x.userid, x.roleid });
                    table.ForeignKey(
                        name: "fk_aspnetuserroles_aspnetroles_roleid",
                        column: x => x.roleid,
                        principalTable: "aspnetroles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_aspnetuserroles_gen_usuarios_userid",
                        column: x => x.userid,
                        principalTable: "gen_usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aspnetusertokens",
                columns: table => new
                {
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    loginprovider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aspnetusertokens", x => new { x.userid, x.loginprovider, x.name });
                    table.ForeignKey(
                        name: "fk_aspnetusertokens_gen_usuarios_userid",
                        column: x => x.userid,
                        principalTable: "gen_usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_auditlogs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fechahora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duracion = table.Column<double>(type: "double precision", nullable: false),
                    servicio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metodo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    parametros = table.Column<string>(type: "text", nullable: true),
                    usuarioid = table.Column<long>(type: "bigint", nullable: true),
                    impersonatedby = table.Column<long>(type: "bigint", nullable: true),
                    httpmethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    requestabsolutepath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    clientip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    clientuseragent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    resultstatuscode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    resultcontent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_auditlogs", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_auditlogs_gen_usuarios_usuarioid",
                        column: x => x.usuarioid,
                        principalTable: "gen_usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "gen_usuarioaplicaciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuarioid = table.Column<long>(type: "bigint", nullable: false),
                    aplicacionid = table.Column<long>(type: "bigint", nullable: false),
                    @default = table.Column<bool>(name: "default", type: "boolean", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_usuarioaplicaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_usuarioaplicaciones_gen_aplicaciones_aplicacionid",
                        column: x => x.aplicacionid,
                        principalTable: "gen_aplicaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_gen_usuarioaplicaciones_gen_usuarios_usuarioid",
                        column: x => x.usuarioid,
                        principalTable: "gen_usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_usuariogrupos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuarioid = table.Column<long>(type: "bigint", nullable: false),
                    grupoid = table.Column<long>(type: "bigint", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_usuariogrupos", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_usuariogrupos_gen_grupos_grupoid",
                        column: x => x.grupoid,
                        principalTable: "gen_grupos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_gen_usuariogrupos_gen_usuarios_usuarioid",
                        column: x => x.usuarioid,
                        principalTable: "gen_usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_usuarioroles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuarioid = table.Column<long>(type: "bigint", nullable: false),
                    rolid = table.Column<long>(type: "bigint", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_usuarioroles", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_usuarioroles_gen_roles_rolid",
                        column: x => x.rolid,
                        principalTable: "gen_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_gen_usuarioroles_gen_usuarios_usuarioid",
                        column: x => x.usuarioid,
                        principalTable: "gen_usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gen_usuariotipolicencia",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    isactive = table.Column<bool>(type: "boolean", nullable: false),
                    usuarioid = table.Column<long>(type: "bigint", nullable: false),
                    tipolicenciaid = table.Column<long>(type: "bigint", nullable: false),
                    createddatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_usuariotipolicencia", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_usuariotipolicencia_gen_tipolicencia_tipolicenciaid",
                        column: x => x.tipolicenciaid,
                        principalTable: "gen_tipolicencia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_gen_usuariotipolicencia_gen_usuarios_usuarioid",
                        column: x => x.usuarioid,
                        principalTable: "gen_usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fot_codigosacceso",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    participanteid = table.Column<long>(type: "bigint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creadoen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fot_codigosacceso", x => x.id);
                    table.ForeignKey(
                        name: "fk_fot_codigosacceso_fot_participantes_participanteid",
                        column: x => x.participanteid,
                        principalTable: "fot_participantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fot_fotos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    eventoid = table.Column<long>(type: "bigint", nullable: false),
                    grupoid = table.Column<long>(type: "bigint", nullable: false),
                    participanteid = table.Column<long>(type: "bigint", nullable: true),
                    storagekey = table.Column<Guid>(type: "uuid", nullable: false),
                    nombrearchivooriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ancho = table.Column<int>(type: "integer", nullable: false),
                    alto = table.Column<int>(type: "integer", nullable: false),
                    tamanobytes = table.Column<long>(type: "bigint", nullable: false),
                    estadoprocesamiento = table.Column<int>(type: "integer", nullable: false),
                    errorprocesamiento = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    creadoen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fot_fotos", x => x.id);
                    table.ForeignKey(
                        name: "fk_fot_fotos_fot_eventos_eventoid",
                        column: x => x.eventoid,
                        principalTable: "fot_eventos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fot_fotos_fot_grupos_grupoid",
                        column: x => x.grupoid,
                        principalTable: "fot_grupos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fot_fotos_fot_participantes_participanteid",
                        column: x => x.participanteid,
                        principalTable: "fot_participantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fot_pedidos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    participanteid = table.Column<long>(type: "bigint", nullable: false),
                    nombrecontacto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    telefonocontacto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    creadoen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    mediopago = table.Column<int>(type: "integer", nullable: true),
                    mercadopagopreferenciaid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pagadoen = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fot_pedidos", x => x.id);
                    table.ForeignKey(
                        name: "fk_fot_pedidos_fot_participantes_participanteid",
                        column: x => x.participanteid,
                        principalTable: "fot_participantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gen_parametrosvalorestenants",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    valor = table.Column<string>(type: "text", nullable: true),
                    tenantid = table.Column<long>(type: "bigint", nullable: false),
                    parametroid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gen_parametrosvalorestenants", x => x.id);
                    table.ForeignKey(
                        name: "fk_gen_parametrosvalorestenants_gen_parametros_parametroid",
                        column: x => x.parametroid,
                        principalTable: "gen_parametros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fot_pedidoitems",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pedidoid = table.Column<long>(type: "bigint", nullable: false),
                    fotoid = table.Column<long>(type: "bigint", nullable: false),
                    tamanoprecioid = table.Column<long>(type: "bigint", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    preciounitariosnapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fot_pedidoitems", x => x.id);
                    table.ForeignKey(
                        name: "fk_fot_pedidoitems_fot_fotos_fotoid",
                        column: x => x.fotoid,
                        principalTable: "fot_fotos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fot_pedidoitems_fot_pedidos_pedidoid",
                        column: x => x.pedidoid,
                        principalTable: "fot_pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fot_pedidoitems_fot_tamanosprecios_tamanoprecioid",
                        column: x => x.tamanoprecioid,
                        principalTable: "fot_tamanosprecios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "gen_authzversion",
                columns: new[] { "id", "version" },
                values: new object[] { 1L, 0L });

            migrationBuilder.CreateIndex(
                name: "ix_aspnetroleclaims_roleid",
                table: "aspnetroleclaims",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "aspnetroles",
                column: "normalizedname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_aspnetuserclaims_userid",
                table: "aspnetuserclaims",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_aspnetuserlogins_userid",
                table: "aspnetuserlogins",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_aspnetuserroles_roleid",
                table: "aspnetuserroles",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_codigosacceso_participanteid",
                table: "fot_codigosacceso",
                column: "participanteid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_codigosacceso_tenantid_codigo",
                table: "fot_codigosacceso",
                columns: new[] { "tenantid", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fot_eventos_tenantid",
                table: "fot_eventos",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_fotos_eventoid",
                table: "fot_fotos",
                column: "eventoid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_fotos_grupoid",
                table: "fot_fotos",
                column: "grupoid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_fotos_participanteid",
                table: "fot_fotos",
                column: "participanteid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_fotos_storagekey",
                table: "fot_fotos",
                column: "storagekey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fot_fotos_tenantid",
                table: "fot_fotos",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_grupos_eventoid",
                table: "fot_grupos",
                column: "eventoid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_grupos_tenantid",
                table: "fot_grupos",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_participantes_grupoid",
                table: "fot_participantes",
                column: "grupoid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_participantes_tenantid",
                table: "fot_participantes",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_pedidoitems_fotoid",
                table: "fot_pedidoitems",
                column: "fotoid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_pedidoitems_pedidoid",
                table: "fot_pedidoitems",
                column: "pedidoid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_pedidoitems_tamanoprecioid",
                table: "fot_pedidoitems",
                column: "tamanoprecioid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_pedidoitems_tenantid",
                table: "fot_pedidoitems",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_pedidos_participanteid",
                table: "fot_pedidos",
                column: "participanteid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_pedidos_tenantid",
                table: "fot_pedidos",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_tamanosprecios_eventoid",
                table: "fot_tamanosprecios",
                column: "eventoid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_tamanosprecios_tenantid",
                table: "fot_tamanosprecios",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_auditlog_fechahora",
                table: "gen_auditlogs",
                column: "fechahora");

            migrationBuilder.CreateIndex(
                name: "ix_gen_auditlogs_usuarioid",
                table: "gen_auditlogs",
                column: "usuarioid");

            migrationBuilder.CreateIndex(
                name: "uk_endpoint_route_method",
                table: "gen_endpoints",
                columns: new[] { "route", "httpmethod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gen_gruporoles_grupoid",
                table: "gen_gruporoles",
                column: "grupoid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_gruporoles_rolid",
                table: "gen_gruporoles",
                column: "rolid");

            migrationBuilder.CreateIndex(
                name: "uk_tenant_grupo_rol",
                table: "gen_gruporoles",
                columns: new[] { "tenantid", "grupoid", "rolid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_grupo_tenant",
                table: "gen_grupos",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_menus_aplicacionid",
                table: "gen_menus",
                column: "aplicacionid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_menus_menupadreid",
                table: "gen_menus",
                column: "menupadreid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_menus_permisoid",
                table: "gen_menus",
                column: "permisoid");

            migrationBuilder.CreateIndex(
                name: "uk_menu_codigo",
                table: "gen_menus",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gen_parametros_aplicacionid",
                table: "gen_parametros",
                column: "aplicacionid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_parametros_permisoid",
                table: "gen_parametros",
                column: "permisoid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_parametrosvalorestenants_parametroid",
                table: "gen_parametrosvalorestenants",
                column: "parametroid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_parametrosvalorestenants_tenantid_parametroid",
                table: "gen_parametrosvalorestenants",
                columns: new[] { "tenantid", "parametroid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gen_permisoendpoints_endpointid",
                table: "gen_permisoendpoints",
                column: "endpointid");

            migrationBuilder.CreateIndex(
                name: "uk_permisos_endpoints",
                table: "gen_permisoendpoints",
                columns: new[] { "permisoid", "endpointid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gen_permisos_aplicacionid",
                table: "gen_permisos",
                column: "aplicacionid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_permisos_permisopadreid",
                table: "gen_permisos",
                column: "permisopadreid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_refreshtokens_tokenhash",
                table: "gen_refreshtokens",
                column: "tokenhash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gen_refreshtokens_userid_revokedat",
                table: "gen_refreshtokens",
                columns: new[] { "userid", "revokedat" });

            migrationBuilder.CreateIndex(
                name: "ix_gen_rolpermisos_permisoid",
                table: "gen_rolpermisos",
                column: "permisoid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_rolpermisos_rolid",
                table: "gen_rolpermisos",
                column: "rolid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_tenantaplicaciones_aplicacionid",
                table: "gen_tenantaplicaciones",
                column: "aplicacionid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_tenantaplicaciones_tenantid",
                table: "gen_tenantaplicaciones",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_tenantfiles_tenantid",
                table: "gen_tenantfiles",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_tenantlicencias_tenantid",
                table: "gen_tenantlicencias",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_tenantlicencias_tipolicenciaid",
                table: "gen_tenantlicencias",
                column: "tipolicenciaid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_tenants_codigo",
                table: "gen_tenants",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gen_tipolicenciaroles_rolid",
                table: "gen_tipolicenciaroles",
                column: "rolid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_tipolicenciaroles_tipolicenciaid",
                table: "gen_tipolicenciaroles",
                column: "tipolicenciaid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_usuarioaplicaciones_aplicacionid",
                table: "gen_usuarioaplicaciones",
                column: "aplicacionid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_usuarioaplicaciones_usuarioid",
                table: "gen_usuarioaplicaciones",
                column: "usuarioid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_usuariogrupos_grupoid",
                table: "gen_usuariogrupos",
                column: "grupoid");

            migrationBuilder.CreateIndex(
                name: "ix_usuariogrupo_usuario",
                table: "gen_usuariogrupos",
                column: "usuarioid")
                .Annotation("Npgsql:IndexInclude", new[] { "grupoid", "tenantid" });

            migrationBuilder.CreateIndex(
                name: "uk_tenant_grupo_usuario",
                table: "gen_usuariogrupos",
                columns: new[] { "tenantid", "grupoid", "usuarioid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gen_usuarioroles_rolid",
                table: "gen_usuarioroles",
                column: "rolid");

            migrationBuilder.CreateIndex(
                name: "uk_usuario_rol",
                table: "gen_usuarioroles",
                columns: new[] { "usuarioid", "rolid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "gen_usuarios",
                column: "normalizedemail");

            migrationBuilder.CreateIndex(
                name: "ix_gen_usuarios_tenantid",
                table: "gen_usuarios",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_usuarios_username",
                table: "gen_usuarios",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "gen_usuarios",
                column: "normalizedusername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gen_UsuariosHistorial_TenantId_Periodo",
                table: "gen_usuarioshistorial",
                columns: new[] { "tenantid", "periodo" });

            migrationBuilder.CreateIndex(
                name: "ix_gen_usuariotipolicencia_tipolicenciaid",
                table: "gen_usuariotipolicencia",
                column: "tipolicenciaid");

            migrationBuilder.CreateIndex(
                name: "ix_gen_usuariotipolicencia_usuarioid",
                table: "gen_usuariotipolicencia",
                column: "usuarioid");

            // La vista vw_usuariorolesefectivos la consume la entidad mapeada con ToView (menú + authz).
            // Es SQL crudo: no sale del snapshot del modelo al regenerar migraciones, hay que mantenerla a
            // mano acá (portada a Postgres desde la migración original de SQL Server, ADR-09). Todo en
            // minúscula sin comillas: coincide con la convención UseLowerCaseNamingConvention aplicada en
            // DatabaseFactory (ver AcgFotosDbContext.OnModelCreating).
            // Fuente única de los roles efectivos (directos ∪ de grupos) + flag permitidoporlicencia
            // (EXISTS contra la licencia ACTIVA, tope duro para no-root).
            migrationBuilder.Sql(@"
CREATE VIEW vw_usuariorolesefectivos
AS
    WITH efectivos AS (
        SELECT ur.usuarioid, ur.tenantid, ur.rolid,
               CAST('Directo' AS varchar(10)) AS origen, CAST(NULL AS bigint) AS grupoid
        FROM gen_usuarioroles ur
        UNION ALL
        SELECT ug.usuarioid, ug.tenantid, gr.rolid,
               CAST('Grupo' AS varchar(10)) AS origen, ug.grupoid
        FROM gen_usuariogrupos ug
        INNER JOIN gen_gruporoles gr
            ON gr.tenantid = ug.tenantid AND gr.grupoid = ug.grupoid
    )
    SELECT
        e.usuarioid, e.tenantid, e.rolid, e.origen, e.grupoid,
        EXISTS (
            SELECT 1
            FROM gen_usuariotipolicencia ul
            INNER JOIN gen_tipolicenciaroles tlr ON tlr.tipolicenciaid = ul.tipolicenciaid
            WHERE ul.usuarioid = e.usuarioid AND ul.isactive = true AND tlr.rolid = e.rolid
        ) AS permitidoporlicencia
    FROM efectivos e;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS vw_usuariorolesefectivos;");

            migrationBuilder.DropTable(
                name: "aspnetroleclaims");

            migrationBuilder.DropTable(
                name: "aspnetuserclaims");

            migrationBuilder.DropTable(
                name: "aspnetuserlogins");

            migrationBuilder.DropTable(
                name: "aspnetuserroles");

            migrationBuilder.DropTable(
                name: "aspnetusertokens");

            migrationBuilder.DropTable(
                name: "fot_codigosacceso");

            migrationBuilder.DropTable(
                name: "fot_pedidoitems");

            migrationBuilder.DropTable(
                name: "gen_auditlogs");

            migrationBuilder.DropTable(
                name: "gen_authzversion");

            migrationBuilder.DropTable(
                name: "gen_gruporoles");

            migrationBuilder.DropTable(
                name: "gen_loginfos");

            migrationBuilder.DropTable(
                name: "gen_menus");

            migrationBuilder.DropTable(
                name: "gen_parametrosvalorestenants");

            migrationBuilder.DropTable(
                name: "gen_permisoendpoints");

            migrationBuilder.DropTable(
                name: "gen_refreshtokens");

            migrationBuilder.DropTable(
                name: "gen_rolpermisos");

            migrationBuilder.DropTable(
                name: "gen_tenantaplicaciones");

            migrationBuilder.DropTable(
                name: "gen_tenantfiles");

            migrationBuilder.DropTable(
                name: "gen_tenantlicencias");

            migrationBuilder.DropTable(
                name: "gen_tipolicenciaroles");

            migrationBuilder.DropTable(
                name: "gen_usuarioaplicaciones");

            migrationBuilder.DropTable(
                name: "gen_usuariogrupos");

            migrationBuilder.DropTable(
                name: "gen_usuarioroles");

            migrationBuilder.DropTable(
                name: "gen_usuariosactivosmensuales");

            migrationBuilder.DropTable(
                name: "gen_usuarioshistorial");

            migrationBuilder.DropTable(
                name: "gen_usuariotipolicencia");

            migrationBuilder.DropTable(
                name: "aspnetroles");

            migrationBuilder.DropTable(
                name: "fot_fotos");

            migrationBuilder.DropTable(
                name: "fot_pedidos");

            migrationBuilder.DropTable(
                name: "fot_tamanosprecios");

            migrationBuilder.DropTable(
                name: "gen_parametros");

            migrationBuilder.DropTable(
                name: "gen_endpoints");

            migrationBuilder.DropTable(
                name: "gen_grupos");

            migrationBuilder.DropTable(
                name: "gen_roles");

            migrationBuilder.DropTable(
                name: "gen_tipolicencia");

            migrationBuilder.DropTable(
                name: "gen_usuarios");

            migrationBuilder.DropTable(
                name: "fot_participantes");

            migrationBuilder.DropTable(
                name: "gen_permisos");

            migrationBuilder.DropTable(
                name: "gen_tenants");

            migrationBuilder.DropTable(
                name: "fot_grupos");

            migrationBuilder.DropTable(
                name: "gen_aplicaciones");

            migrationBuilder.DropTable(
                name: "fot_eventos");
        }
    }
}
