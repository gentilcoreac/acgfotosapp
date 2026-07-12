using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcgFotos.Base.SqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class VerticalFotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fot_Eventos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Colegio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fot_Eventos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fot_Cursos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventoId = table.Column<long>(type: "bigint", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fot_Cursos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fot_Cursos_fot_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "fot_Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fot_TamanosPrecios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventoId = table.Column<long>(type: "bigint", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fot_TamanosPrecios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fot_TamanosPrecios_fot_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "fot_Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fot_Albumes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CursoId = table.Column<long>(type: "bigint", nullable: false),
                    NombreAlumno = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fot_Albumes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fot_Albumes_fot_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "fot_Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fot_CodigosAcceso",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlbumId = table.Column<long>(type: "bigint", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fot_CodigosAcceso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fot_CodigosAcceso_fot_Albumes_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "fot_Albumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fot_Fotos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventoId = table.Column<long>(type: "bigint", nullable: false),
                    CursoId = table.Column<long>(type: "bigint", nullable: false),
                    AlbumId = table.Column<long>(type: "bigint", nullable: true),
                    StorageKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreArchivoOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Ancho = table.Column<int>(type: "int", nullable: false),
                    Alto = table.Column<int>(type: "int", nullable: false),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    EstadoProcesamiento = table.Column<int>(type: "int", nullable: false),
                    ErrorProcesamiento = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fot_Fotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fot_Fotos_fot_Albumes_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "fot_Albumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fot_Fotos_fot_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "fot_Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fot_Fotos_fot_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "fot_Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fot_Pedidos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlbumId = table.Column<long>(type: "bigint", nullable: false),
                    NombreContacto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TelefonoContacto = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MedioPago = table.Column<int>(type: "int", nullable: true),
                    MercadoPagoPreferenciaId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PagadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fot_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fot_Pedidos_fot_Albumes_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "fot_Albumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fot_PedidoItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoId = table.Column<long>(type: "bigint", nullable: false),
                    FotoId = table.Column<long>(type: "bigint", nullable: false),
                    TamanoPrecioId = table.Column<long>(type: "bigint", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitarioSnapshot = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fot_PedidoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fot_PedidoItems_fot_Fotos_FotoId",
                        column: x => x.FotoId,
                        principalTable: "fot_Fotos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fot_PedidoItems_fot_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "fot_Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fot_PedidoItems_fot_TamanosPrecios_TamanoPrecioId",
                        column: x => x.TamanoPrecioId,
                        principalTable: "fot_TamanosPrecios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fot_Albumes_CursoId",
                table: "fot_Albumes",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_Albumes_TenantId",
                table: "fot_Albumes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_CodigosAcceso_AlbumId",
                table: "fot_CodigosAcceso",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_CodigosAcceso_TenantId_Codigo",
                table: "fot_CodigosAcceso",
                columns: new[] { "TenantId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fot_Cursos_EventoId",
                table: "fot_Cursos",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_Cursos_TenantId",
                table: "fot_Cursos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_Eventos_TenantId",
                table: "fot_Eventos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_Fotos_AlbumId",
                table: "fot_Fotos",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_Fotos_CursoId",
                table: "fot_Fotos",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_Fotos_EventoId",
                table: "fot_Fotos",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_Fotos_StorageKey",
                table: "fot_Fotos",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fot_Fotos_TenantId",
                table: "fot_Fotos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_PedidoItems_FotoId",
                table: "fot_PedidoItems",
                column: "FotoId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_PedidoItems_PedidoId",
                table: "fot_PedidoItems",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_PedidoItems_TamanoPrecioId",
                table: "fot_PedidoItems",
                column: "TamanoPrecioId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_PedidoItems_TenantId",
                table: "fot_PedidoItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_Pedidos_AlbumId",
                table: "fot_Pedidos",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_Pedidos_TenantId",
                table: "fot_Pedidos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_TamanosPrecios_EventoId",
                table: "fot_TamanosPrecios",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_fot_TamanosPrecios_TenantId",
                table: "fot_TamanosPrecios",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fot_CodigosAcceso");

            migrationBuilder.DropTable(
                name: "fot_PedidoItems");

            migrationBuilder.DropTable(
                name: "fot_Fotos");

            migrationBuilder.DropTable(
                name: "fot_Pedidos");

            migrationBuilder.DropTable(
                name: "fot_TamanosPrecios");

            migrationBuilder.DropTable(
                name: "fot_Albumes");

            migrationBuilder.DropTable(
                name: "fot_Cursos");

            migrationBuilder.DropTable(
                name: "fot_Eventos");
        }
    }
}
